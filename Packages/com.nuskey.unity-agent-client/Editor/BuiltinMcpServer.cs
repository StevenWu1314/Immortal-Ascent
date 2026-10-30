using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityAgentClient
{
    [InitializeOnLoad]
    public static class BuiltinMcpServer
    {
        static HttpListener listener;
        static Thread listenerThread;
        const int Port = 57123;
        static readonly string Url = $"http://localhost:{Port}/";

        static readonly List<LogEntry> collectedLogs = new();
        static readonly object logLock = new();

        static BuiltinMcpServer()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.update += Initialize;
        }

        static int startAttempts;

        static void Initialize()
        {
            // Keep retrying on the next tick instead of giving up: during a script reload the
            // previous domain's listener can still hold the port for a moment, and without the
            // retry the server would stay dead for the rest of the session.
            if (!StartServer()) return;

            EditorApplication.update -= Initialize;
            EditorApplication.quitting += StopServer;
        }

        static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            lock (logLock)
            {
                collectedLogs.Add(new LogEntry
                {
                    Condition = condition,
                    StackTrace = stackTrace,
                    Type = type.ToString()
                });
            }
        }

        static bool StartServer()
        {
            if (listener != null && listener.IsListening) return true;

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(Url);
                listener.Start();

                listenerThread = new Thread(HandleRequests)
                {
                    IsBackground = true
                };
                listenerThread.Start();

                Logger.LogVerbose($"Built-in MCP Server Started on port {Port}");
                return true;
            }
            catch (Exception e)
            {
                // Clear the half-built listener so the next attempt is not blocked by the
                // "already listening" guard, and only report the first failure.
                listener = null;
                if (startAttempts++ == 0)
                {
                    Logger.LogError($"Failed to start MCP Server: {e.Message} (retrying)");
                }
                return false;
            }
        }

        static void StopServer()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            if (listener != null)
            {
                listener.Stop();
                listener.Close();
                listener = null;
                Logger.LogVerbose("Built-in MCP server Stopped");
            }
        }

        static void HandleRequests()
        {
            while (listener != null && listener.IsListening)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.LogError($"Request handling error: {e.Message}");
                }
            }
        }

        static void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                string responseString;


                if (request.Url.AbsolutePath == "/tools" && request.HttpMethod == "GET")
                {
                    responseString = GetToolsList();
                }
                else if (request.Url.AbsolutePath == "/editor_state" && request.HttpMethod == "GET")
                {
                    responseString = HandleEditorState();
                }
                else if (request.Url.AbsolutePath == "/execute_method" && request.HttpMethod == "POST")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = reader.ReadToEnd();
                    responseString = HandleExecuteMethod(body);
                }
                else if (request.Url.AbsolutePath == "/read_unity_console" && request.HttpMethod == "POST")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = reader.ReadToEnd();
                    responseString = HandleGetLogs(body);
                }
                else
                {
                    response.StatusCode = 404;
                    responseString = "{\"error\":\"Not Found\"}";
                }

                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception e)
            {
                Logger.LogError($"Processing error: {e.Message}");
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { }
            }
        }

        static string GetToolsList()
        {
            var tools = new object[]
            {
                new
                {
                    name = "read_unity_console",
                    description = "Retrieve Unity Editor console logs.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            maxCount = new
                            {
                                type = "number",
                                description = "Maximum number of logs to retrieve (default: 100).",
                                @default = 100
                            }
                        }
                    }
                },
                new
                {
                    name = "editor_state",
                    description = "Whether the editor is playing, compiling, or holding an unsaved scene.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new
                {
                    name = "execute_method",
                    description = "Invoke a static C# method (for example an [MenuItem] diagnostic) on the " +
                                  "editor's main thread. The method's own Debug.Log output goes to the console " +
                                  "and can be read back with read_unity_console.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            type = new
                            {
                                type = "string",
                                description = "Type name holding the method, e.g. MapgenDiagnostics."
                            },
                            method = new
                            {
                                type = "string",
                                description = "Static method to run, e.g. ChestWalkability."
                            },
                            timeoutMs = new
                            {
                                type = "number",
                                description = "Give up after this long (default 30000). The method is not started if the editor never becomes idle.",
                                @default = 30000
                            }
                        },
                        required = new[] { "type", "method" }
                    }
                }
            };

            return JsonSerializer.Serialize(new { tools });
        }

        static string HandleEditorState()
        {
            string state = RunOnMainThread(() =>
            {
                var scene = SceneManager.GetActiveScene();
                return JsonSerializer.Serialize(new
                {
                    busy = false,
                    isPlaying = EditorApplication.isPlaying,
                    isPaused = EditorApplication.isPaused,
                    isCompiling = EditorApplication.isCompiling,
                    isUpdating = EditorApplication.isUpdating,
                    activeScene = scene.path,
                    activeSceneName = scene.name,
                    activeSceneDirty = scene.isDirty,
                    unityVersion = Application.unityVersion
                });
            }, 2000);

            // Anything that is not already a JSON object means the editor never ran the block, which
            // is itself the answer: it is mid-reload, so report that instead of invalid JSON.
            return state.Length > 0 && state[0] == '{'
                ? state
                : JsonSerializer.Serialize(new { busy = true, detail = state });
        }

        static string HandleExecuteMethod(string requestBody)
        {
            ExecuteMethodArguments args;
            try
            {
                args = string.IsNullOrEmpty(requestBody)
                    ? null
                    : JsonSerializer.Deserialize<ExecuteMethodArguments>(requestBody);
            }
            catch (Exception e)
            {
                return Error("malformed request body: " + e.Message);
            }

            if (args == null || string.IsNullOrEmpty(args.Type) || string.IsNullOrEmpty(args.Method))
            {
                return Error("both 'type' and 'method' are required, for example " +
                             "{\"type\":\"MapgenDiagnostics\",\"method\":\"ChestWalkability\"}");
            }

            if (args.TimeoutMs <= 0) args.TimeoutMs = 30000;

            string outcome = RunOnMainThread(() => InvokeStatic(args.Type, args.Method), args.TimeoutMs);
            return $"{{\"result\":\"{EscapeJson(outcome)}\"}}";
        }

        /// <summary>
        /// Runs <paramref name="action"/> on the editor's main thread. The HTTP listener serves
        /// requests from thread-pool threads, and anything that touches the scene, the AssetDatabase
        /// or a GameObject throws when called from there.
        /// </summary>
        static string RunOnMainThread(Func<string> action, int timeoutMs)
        {
            var completion = new TaskCompletionSource<string>();
            EditorApplication.delayCall += () =>
            {
                try
                {
                    completion.TrySetResult(action());
                }
                catch (Exception e)
                {
                    completion.TrySetResult("EXCEPTION: " + Describe(e));
                }
            };

            if (!completion.Task.Wait(timeoutMs))
            {
                return $"TIMEOUT after {timeoutMs}ms: the editor never became idle (compiling, reloading, " +
                       "or showing a modal dialog). Nothing was executed.";
            }
            return completion.Task.Result;
        }

        static string InvokeStatic(string typeName, string methodName)
        {
            Type type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null) break;
            }
            if (type == null)
            {
                return $"ERROR: type '{typeName}' was not found in any loaded assembly.";
            }

            var method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                return $"ERROR: no static method '{methodName}' on '{type.FullName}'.";
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            object returned = method.Invoke(null, null);
            watch.Stop();

            string suffix = returned != null ? $" -> {returned}" : "";
            return $"{typeName}.{methodName} ran in {watch.ElapsedMilliseconds}ms{suffix}";
        }

        /// <summary>Unwraps the reflection wrapper so the real exception is what gets reported.</summary>
        static string Describe(Exception e)
        {
            while (e is TargetInvocationException && e.InnerException != null)
            {
                e = e.InnerException;
            }
            return $"{e.GetType().Name}: {e.Message}\n{e.StackTrace}";
        }

        static string Error(string message)
        {
            return $"{{\"error\":\"{EscapeJson(message)}\"}}";
        }

        static string HandleGetLogs(string requestBody)
        {
            try
            {
                var args = string.IsNullOrEmpty(requestBody)
                    ? new GetLogsArguments()
                    : JsonSerializer.Deserialize<GetLogsArguments>(requestBody);

                if (args.MaxCount <= 0) args.MaxCount = 100;

                List<LogEntry> logs;
                lock (logLock)
                {
                    var startIndex = Math.Max(0, collectedLogs.Count - args.MaxCount);
                    var count = Math.Min(args.MaxCount, collectedLogs.Count);
                    logs = collectedLogs.GetRange(startIndex, count);
                }

                var result = new StringBuilder();
                result.AppendLine($"number of logs: {logs.Count}");
                result.AppendLine();

                foreach (var log in logs)
                {
                    result.AppendLine($"[{log.Type.ToUpper()}]");
                    result.AppendLine(log.Condition);
                    result.AppendLine();
                }

                return $"{{\"result\":\"{EscapeJson(result.ToString())}\"}}";
            }
            catch (Exception e)
            {
                return Error(e.Message);
            }
        }

        static string EscapeJson(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        class LogEntry
        {
            public string Condition { get; set; }
            public string StackTrace { get; set; }
            public string Type { get; set; }
        }

        [Serializable]
        class GetLogsArguments
        {
            // A property, not a field: System.Text.Json ignores public fields by default, so the
            // original field silently ignored whatever maxCount the caller sent.
            [JsonPropertyName("maxCount")]
            public int MaxCount { get; set; } = 100;
        }

        [Serializable]
        class ExecuteMethodArguments
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("method")]
            public string Method { get; set; }

            [JsonPropertyName("timeoutMs")]
            public int TimeoutMs { get; set; } = 30000;
        }
    }
}
