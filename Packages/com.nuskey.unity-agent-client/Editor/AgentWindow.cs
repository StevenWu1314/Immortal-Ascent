using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using AgentClientProtocol;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System;
using System.Text;

namespace UnityAgentClient
{
    public sealed class AgentWindow : EditorWindow, IAcpClient
    {
        enum ConnectionStatus
        {
            Pending,
            Success,
            Failed,
        }

        static GUIStyle wordWrapTextAreaStyle;

        Vector2 conversationScroll;
        Vector2 inputScroll;
        string inputText = "";
        readonly List<SessionUpdate> messages = new();
        readonly Dictionary<object, bool> foldoutStates = new();
        readonly List<UnityEngine.Object> attachedAssets = new();


        ConnectionStatus connectionStatus;
        bool isRunning;
        string sessionId;
        ClientSideConnection conn;
        Process agentProcess;

        // Model management
        ModelInfo[] availableModels = Array.Empty<ModelInfo>();
        int selectedModelIndex;

        // Mode management
        string[] availableModes = Array.Empty<string>();
        int selectedModeIndex;

        // Permission management
        RequestPermissionRequest pendingPermissionRequest;
        TaskCompletionSource<RequestPermissionResponse> pendingPermissionTcs;

        // Auth management
        AuthMethod[] pendingAuthMethods;
        TaskCompletionSource<AuthMethod> pendingAuthTcs;

        /// <summary>Folder holding this package, wherever UPM resolved it (embedded or git).</summary>
        static string PackageRoot
        {
            get
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AgentWindow).Assembly);
                return info != null && !string.IsNullOrEmpty(info.resolvedPath)
                    ? info.resolvedPath
                    : Path.Combine(Application.dataPath, "..", "Packages", "com.nuskey.unity-agent-client");
            }
        }

        [MenuItem("Window/Unity Agent Client/AI Agent")]
        static void Init()
        {
            var window = (AgentWindow)GetWindow(typeof(AgentWindow));
            window.titleContent = EditorGUIUtility.IconContent("account");
            window.titleContent.text = "AI Agent";

            window.connectionStatus = ConnectionStatus.Pending;
            window.isRunning = false;
            window.sessionId = null;
            window.messages.Clear();
            window.foldoutStates.Clear();
            window.attachedAssets.Clear();
            window.availableModels = Array.Empty<ModelInfo>();
            window.selectedModelIndex = 0;
            window.availableModes = Array.Empty<string>();
            window.selectedModeIndex = 0;
            window.pendingPermissionRequest = null;
            window.pendingPermissionTcs = null;
            window.pendingAuthMethods = null;
            window.pendingAuthTcs = null;

            window.Show();
        }

        void Disconnect()
        {
            if (agentProcess != null && !agentProcess.HasExited)
            {
                agentProcess.Kill();
                Logger.LogVerbose("Disconnected");
            }

            AssemblyReloadEvents.beforeAssemblyReload -= Disconnect;
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Disconnect;
        }

        void OnDisable()
        {
            Disconnect();
        }

        async Task ConnectAsync(AgentSettings config, CancellationToken cancellationToken = default)
        {
            Disconnect();

            var startInfo = new ProcessStartInfo
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

#if UNITY_EDITOR_OSX
            // Route through the login shell so PATH is populated, but pass the command as one
            // argv entry: interpolating it into Arguments breaks any path containing a space.
            startInfo.FileName = "/bin/zsh";
            startInfo.ArgumentList.Add("-cl");
            startInfo.ArgumentList.Add($"'{config.Command}' {config.Arguments}");
#else
            startInfo.FileName = config.Command;
            startInfo.Arguments = config.Arguments;
#endif

            foreach (var kv in config.EnvironmentVariables)
            {
                startInfo.EnvironmentVariables[kv.Key] = kv.Value;
            }

            agentProcess = Process.Start(startInfo);

            Task.Run(() =>
            {
                var line = agentProcess.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(line))
                {
                    UnityEngine.Debug.LogError(line);
                    connectionStatus = ConnectionStatus.Failed;
                }
            }, cancellationToken).Forget();

            conn = new ClientSideConnection(_ => this, agentProcess.StandardOutput, agentProcess.StandardInput);
            conn.Open();

            Logger.LogVerbose("Connecting...");

            var initRes = await conn.InitializeAsync(new()
            {
                ProtocolVersion = 1,
                ClientCapabilities = new()
                {
                    Fs = new()
                    {
                        ReadTextFile = true,
                        WriteTextFile = true,
                    }
                },
                ClientInfo = new()
                {
                    Name = "UnityClientAgent",
                    Version = "0.1.0",
                }
            }, cancellationToken);

            Logger.LogVerbose($"Connected to agent '{initRes.AgentInfo?.Name}'");

            if (initRes.AuthMethods != null && initRes.AuthMethods.Length > 1)
            {
                pendingAuthMethods = initRes.AuthMethods;
                pendingAuthTcs = new TaskCompletionSource<AuthMethod>(TaskCreationOptions.RunContinuationsAsynchronously);

                var selectedAuthMethod = await pendingAuthTcs.Task;

                var authRes = await conn.AuthenticateAsync(new()
                {
                    MethodId = selectedAuthMethod.Id,
                }, cancellationToken);

                Logger.LogVerbose($"Authenticated with method: {selectedAuthMethod.Id}");
            }

            var newSession = await conn.NewSessionAsync(new()
            {
                Cwd = Directory.GetParent(Application.dataPath).FullName,
                McpServers = new McpServer[]
                {
                    new StdioMcpServer
                    {
                        // Absolute command path: dsh rejects relative ones. `env` resolves node from the
                        // login-shell PATH the agent itself was launched with.
                        Command = "/usr/bin/env",
                        Args = new string[] { "node", Path.Combine(PackageRoot, "Editor", "server.js") },
                        Env = new EnvVariable[] { },
                        Name = "unity-agent-client-builtin-mcp"
                    }
                },
            }, cancellationToken);

            sessionId = newSession.SessionId;

            if (newSession.Models?.AvailableModels != null && newSession.Models.AvailableModels.Length > 0)
            {
                availableModels = newSession.Models.AvailableModels;
                selectedModelIndex = availableModels
                    .Select((model, index) => (model, index))
                    .Where(x => x.model.ModelId == newSession.Models.CurrentModelId)
                    .First()
                    .index;
            }

            // Cache available modes
            if (newSession.Modes?.AvailableModes != null && newSession.Modes.AvailableModes.Length > 0)
            {
                availableModes = newSession.Modes.AvailableModes.Select(m => m.Id).ToArray();
                selectedModeIndex = availableModes
                    .Select((mode, index) => (mode, index))
                    .Where(x => x.mode == newSession.Modes.CurrentModeId)
                    .FirstOrDefault()
                    .index;
            }

            Logger.LogVerbose($"Session created ({sessionId}).");

            connectionStatus = ConnectionStatus.Success;
        }

        void OnGUI()
        {
            wordWrapTextAreaStyle ??= new(EditorStyles.textArea)
            {
                wordWrap = true
            };

            HandleWindowDragAndDrop();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(Math.Min(800, EditorGUIUtility.currentViewWidth - 18))))
                {
                    if (conn == null)
                    {
                        var settings = AgentSettingsProvider.Load();
                        if (settings == null || string.IsNullOrWhiteSpace(settings.Command))
                        {
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("No agent has been configured.");
                            if (GUILayout.Button("Open Project Settings..."))
                            {
                                SettingsService.OpenProjectSettings("Project/Unity Agent Client");
                            }
                            return;
                        }

                        ConnectAsync(settings).Forget();
                    }

                    EditorGUILayout.Space();

                    switch (connectionStatus)
                    {
                        case ConnectionStatus.Pending:
                            if (pendingAuthMethods != null)
                            {
                                DrawAuthRequest();
                            }
                            else
                            {
                                EditorGUILayout.LabelField("Connecting...");
                            }
                            return;
                        case ConnectionStatus.Failed:
                            EditorGUILayout.LabelField("Connection Error");
                            if (GUILayout.Button("Retry"))
                            {
                                var settings = AgentSettingsProvider.Load();
                                ConnectAsync(settings).Forget();
                            }
                            return;

                    }

                    conversationScroll = EditorGUILayout.BeginScrollView(conversationScroll, GUILayout.ExpandHeight(true));

                    try
                    {
                        foreach (var message in messages)
                        {
                            DrawMessage(message);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // ignore 'Collection was modified' error
                    }

                    if (pendingPermissionRequest != null)
                    {
                        DrawPermissionRequest();
                    }

                    EditorGUILayout.EndScrollView();

                    if (isRunning)
                    {
                        conversationScroll.y = float.MaxValue;
                    }

                    EditorGUILayout.Space();

                    DrawAttachmentUI();
                    DrawInputField();

                    EditorGUILayout.BeginHorizontal();

                    using (new EditorGUI.DisabledScope(isRunning))
                    {
                        int newModeIndex = EditorGUILayout.Popup(
                            selectedModeIndex,
                            availableModes,
                            GUILayout.Width(80));
                        if (newModeIndex != selectedModeIndex)
                        {
                            selectedModeIndex = newModeIndex;
                            SetSessionModeAsync(availableModes[newModeIndex]).Forget();
                        }

                        int newModelIndex = EditorGUILayout.Popup(
                            selectedModelIndex,
                            availableModels.Select(x => x.Name).ToArray());
                        if (newModelIndex != selectedModelIndex)
                        {
                            selectedModelIndex = newModelIndex;
                            SetSessionModelAsync(availableModels[newModelIndex].ModelId).Forget();
                        }
                    }

                    if (isRunning)
                    {
                        if (GUILayout.Button("Stop"))
                        {
                            try
                            {
                                CancelSessionAsync().Forget();
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"Failed to cancel: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Send"))
                        {
                            SendRequestAsync().Forget();
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                }

                GUILayout.FlexibleSpace();
            }
        }

        void DrawInputField()
        {
            inputScroll = EditorGUILayout.BeginScrollView(inputScroll, GUILayout.Height(80));
            inputText = EditorGUILayout.TextArea(
                inputText,
                wordWrapTextAreaStyle,
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true));
            EditorGUILayout.EndScrollView();
        }

        void DrawMessage(SessionUpdate update)
        {
            switch (update)
            {
                case UserMessageChunkSessionUpdate userMessage:
                    DrawUserMessageChunk(userMessage);
                    break;
                case AgentMessageChunkSessionUpdate agentMessage:
                    DrawAgentMessageChunk(agentMessage);
                    break;
                case AgentThoughtChunkSessionUpdate agentThought:
                    DrawAgentThoughtChunk(agentThought);
                    break;
                case ToolCallSessionUpdate toolCall:
                    DrawToolCall(toolCall);
                    break;
                case ToolCallUpdateSessionUpdate toolCallUpdate:
                    DrawToolCallUpdate(toolCallUpdate);
                    break;
                case PlanSessionUpdate plan:
                    DrawPlan(plan);
                    break;
                case AvailableCommandsUpdateSessionUpdate availableCommands:
                    DrawAvailableCommands(availableCommands);
                    break;
                case CurrentModeUpdateSessionUpdate currentMode:
                    DrawCurrentMode(currentMode);
                    break;
                default:
                    EditorGUILayout.LabelField($"Unknown update type: {update.GetType().Name}");
                    break;
            }
        }

        void DrawUserMessageChunk(UserMessageChunkSessionUpdate update)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (update.Content is ResourceLinkContentBlock resourceLink)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📎", GUILayout.Width(14));

                if (resourceLink.Uri.StartsWith("file://"))
                {
                    var path = Path.GetRelativePath(Path.Combine(Application.dataPath, ".."), resourceLink.Uri[7..]);
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (asset == null)
                    {
                        EditorGUILayout.LabelField(resourceLink.Name ?? resourceLink.Uri, EditorStyles.wordWrappedLabel);
                    }
                    else
                    {
                        EditorGUILayout.ObjectField(asset, asset.GetType(), false);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(resourceLink.Name ?? resourceLink.Uri, EditorStyles.wordWrappedLabel);
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorMarkdownRenderer.Render(GetContentText(update.Content));
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        void DrawAgentMessageChunk(AgentMessageChunkSessionUpdate update)
        {
            EditorMarkdownRenderer.Render(GetContentText(update.Content));
            EditorGUILayout.Space();
        }

        void DrawAgentThoughtChunk(AgentThoughtChunkSessionUpdate update)
        {
            foldoutStates.TryAdd(update, false);

            var foldout = EditorGUILayout.Foldout(foldoutStates[update], "Thinking...", true);
            if (foldout)
            {
                EditorGUILayout.Space();
                EditorMarkdownRenderer.Render(GetContentText(update.Content));
            }
            EditorGUILayout.Space();

            foldoutStates[update] = foldout;
        }

        void DrawToolCall(ToolCallSessionUpdate update)
        {
            foldoutStates.TryAdd(update, false);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var foldout = EditorGUILayout.Foldout(foldoutStates[update], $"{update.Title}", true);
            if (foldout)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(update.RawInput.ToString(), EditorMarkdownRenderer.CodeBlockStyle);

                EditorGUILayout.Space();

                if (update.RawOutput != null)
                {
                    EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
                    EditorGUILayout.TextArea(update.RawOutput.ToString(), EditorMarkdownRenderer.CodeBlockStyle);
                }

                if (update.Content != null)
                {
                    EditorGUILayout.LabelField("Content", EditorStyles.boldLabel);
                    foreach (var content in update.Content)
                    {
                        EditorMarkdownRenderer.Render(content switch
                        {
                            ContentToolCallContent c => GetContentText(c.Content),
                            DiffToolCallContent diff => diff.Path,
                            _ => content.ToString(),
                        });
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            foldoutStates[update] = foldout;
        }

        void DrawToolCallUpdate(ToolCallUpdateSessionUpdate _)
        {
            // Do nothing...
        }

        void DrawPlan(PlanSessionUpdate update)
        {
            foldoutStates.TryAdd(update, false);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var foldout = EditorGUILayout.Foldout(foldoutStates[update], "Plan", true);
            if (foldout)
            {
                EditorGUILayout.Space();

                foreach (var entry in update.Entries)
                {
                    EditorGUILayout.LabelField($"- {entry}", EditorStyles.wordWrappedLabel);
                }
            }
            EditorGUILayout.Space();

            foldoutStates[update] = foldout;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        void DrawAvailableCommands(AvailableCommandsUpdateSessionUpdate update)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                foldoutStates.TryAdd(update, false);

                var foldout = EditorGUILayout.Foldout(foldoutStates[update], "Available Commands", true);
                if (foldout)
                {
                    EditorGUILayout.Space();

                    foreach (var cmd in update.AvailableCommands)
                    {
                        EditorGUILayout.LabelField($"{cmd.Name} - {cmd.Description}", EditorStyles.wordWrappedLabel);
                    }
                }

                foldoutStates[update] = foldout;
            }

            EditorGUILayout.Space();
        }

        void DrawPermissionRequest()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Permission Request", EditorStyles.boldLabel);

            var toolCall = JsonSerializer.Deserialize<ToolCallSessionUpdate>((JsonElement)pendingPermissionRequest.ToolCall, AcpJsonSerializerContext.Default.Options);
            EditorGUILayout.TextArea(toolCall.Title, EditorMarkdownRenderer.CodeBlockStyle);

            EditorGUILayout.BeginHorizontal();
            foreach (var option in pendingPermissionRequest.Options)
            {
                var buttonLabel = option.Kind switch
                {
                    PermissionOptionKind.AllowOnce => "Allow",
                    PermissionOptionKind.AllowAlways => "Allow Always",
                    PermissionOptionKind.RejectOnce => "Reject",
                    PermissionOptionKind.RejectAlways => "Reject Always",
                    _ => default,
                };

                if (GUILayout.Button(buttonLabel))
                {
                    HandlePermissionResponse(option.OptionId);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        void HandlePermissionResponse(string optionId)
        {
            pendingPermissionTcs.TrySetResult(new RequestPermissionResponse
            {
                Outcome = new SelectedRequestPermissionOutcome
                {
                    OptionId = optionId,
                }
            });

            pendingPermissionRequest = null;
            pendingPermissionTcs = null;

            Repaint();
        }

        void DrawAuthRequest()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Authentication Required", EditorStyles.boldLabel);

            foreach (var authMethod in pendingAuthMethods)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"{authMethod.Name}", EditorStyles.boldLabel);

                if (!string.IsNullOrEmpty(authMethod.Description))
                {
                    EditorGUILayout.LabelField($"{authMethod.Description}", EditorStyles.wordWrappedLabel);
                }

                if (GUILayout.Button("Select", GUILayout.Width(80)))
                {
                    HandleAuthResponse(authMethod);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        void HandleAuthResponse(AuthMethod authMethod)
        {
            pendingAuthTcs.TrySetResult(authMethod);

            pendingAuthMethods = null;
            pendingAuthTcs = null;

            Repaint();
        }

        void DrawCurrentMode(CurrentModeUpdateSessionUpdate update)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            EditorGUILayout.LabelField($"Current Mode: {update.CurrentModeId}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        void HandleWindowDragAndDrop()
        {
            var evt = Event.current;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj != null && !attachedAssets.Contains(obj))
                        {
                            attachedAssets.Add(obj);
                        }
                    }

                    Repaint();
                }

                evt.Use();
            }
        }

        void DrawAttachmentUI()
        {
            if (attachedAssets.Count == 0) return;

            EditorGUILayout.Space();

            for (int i = 0; i < attachedAssets.Count; i++)
            {
                var asset = attachedAssets[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    attachedAssets[i] = EditorGUILayout.ObjectField(asset, typeof(UnityEngine.Object), allowSceneObjects: true);

                    if (GUILayout.Button($"×", EditorStyles.miniButton, GUILayout.Width(30)))
                    {
                        attachedAssets.RemoveAt(i);
                    }
                }
            }
        }

        string GetContentText(ContentBlock content)
        {
            if (content == null) return "";

            var result = content switch
            {
                TextContentBlock text => text.Text,
                ResourceLinkContentBlock resourceLink => $"[{resourceLink.Name ?? "Resource"}]({resourceLink.Uri})",
                _ => content.ToString()
            };

            return result;
        }

        async Task SendRequestAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputText)) return;

            isRunning = true;
            try
            {
                // Add user message
                messages.Add(new UserMessageChunkSessionUpdate
                {
                    Content = new TextContentBlock
                    {
                        Text = inputText,
                    }
                });

                // Build prompt content blocks
                var promptBlocks = new List<ContentBlock>
                {
                    new TextContentBlock { Text = inputText }
                };

                // Add attached assets as resource links
                foreach (var asset in attachedAssets)
                {
                    if (asset == null) continue;

                    var assetPath = AssetDatabase.GetAssetPath(asset);
                    string uri;

                    if (string.IsNullOrEmpty(assetPath))
                    {
                        var gameObject = asset as GameObject;
                        if (gameObject != null && gameObject.scene.IsValid())
                        {
                            var scenePath = gameObject.scene.path;
                            var instanceId = gameObject.GetInstanceID();
                            uri = new Uri($"file://{Path.GetFullPath(scenePath)}?instanceID={instanceId}").AbsoluteUri;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        var fullPath = Path.GetFullPath(assetPath);
                        uri = new Uri(fullPath).AbsoluteUri;
                    }

                    var resourceLink = new ResourceLinkContentBlock
                    {
                        Name = asset.name,
                        Uri = uri
                    };

                    promptBlocks.Add(resourceLink);

                    // Add resource link to messages for UI display
                    messages.Add(new UserMessageChunkSessionUpdate
                    {
                        Content = resourceLink
                    });
                }

                var request = new PromptRequest
                {
                    SessionId = sessionId,
                    Prompt = promptBlocks.ToArray(),
                };

                inputText = "";
                attachedAssets.Clear(); // Clear attachments after sending

                Repaint();

                await conn.PromptAsync(request, cancellationToken);

                Repaint();
            }
            finally
            {
                isRunning = false;
                Repaint();
            }
        }

        async Task SetSessionModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            isRunning = true;
            try
            {
                await conn.SetSessionModelAsync(new()
                {
                    SessionId = sessionId,
                    ModelId = modelId,
                }, cancellationToken);

                Repaint();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to change model: {ex.Message}");
            }
            finally
            {
                isRunning = false;
                Repaint();
            }
        }

        async Task SetSessionModeAsync(string modeId, CancellationToken cancellationToken = default)
        {
            isRunning = true;
            try
            {
                await conn.SetSessionModeAsync(new()
                {
                    SessionId = sessionId,
                    ModeId = modeId,
                }, cancellationToken);

                Repaint();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to change mode: {ex.Message}");
            }
            finally
            {
                isRunning = false;
                Repaint();
            }
        }

        async Task CancelSessionAsync(CancellationToken cancellationToken = default)
        {
            if (!isRunning) return;
            try
            {
                await conn.CancelAsync(new()
                {
                    SessionId = sessionId,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to cancel session: {ex.Message}");
            }
            finally
            {
                isRunning = false;
            }
        }

        public async ValueTask<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest request, CancellationToken cancellationToken = default)
        {
            pendingPermissionRequest = request;
            pendingPermissionTcs = new TaskCompletionSource<RequestPermissionResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

            var response = await pendingPermissionTcs.Task;
            return response;
        }

        public ValueTask SessionNotificationAsync(SessionNotification notification, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var update = notification.Update;

            if (update is AgentMessageChunkSessionUpdate agentMessageChunk)
            {
                if (messages.Count > 0 && messages[^1] is AgentMessageChunkSessionUpdate lastAgentMessage)
                {
                    var lastText = GetContentText(lastAgentMessage.Content);
                    var currentText = GetContentText(agentMessageChunk.Content);
                    messages[^1] = new AgentMessageChunkSessionUpdate
                    {
                        Content = new TextContentBlock
                        {
                            Text = lastText + currentText,
                        }
                    };
                    return default;
                }
            }
            else if (update is AgentThoughtChunkSessionUpdate agentThoughtChunk)
            {
                if (messages.Count > 0 && messages[^1] is AgentThoughtChunkSessionUpdate lastAgentThought)
                {
                    var lastText = GetContentText(lastAgentThought.Content);
                    var currentText = GetContentText(agentThoughtChunk.Content);
                    messages[^1] = new AgentThoughtChunkSessionUpdate
                    {
                        Content = new TextContentBlock
                        {
                            Text = lastText + currentText,
                        }
                    };
                    return default;
                }
            }
            else if (update is ToolCallUpdateSessionUpdate toolCallUpdate)
            {
                if (messages.Count > 0 && messages[^1] is ToolCallSessionUpdate lastToolCall)
                {
                    var combinedContent = new List<ToolCallContent>(lastToolCall.Content);
                    combinedContent.AddRange(toolCallUpdate.Content);

                    messages[^1] = lastToolCall with
                    {
                        Status = toolCallUpdate.Status ?? lastToolCall.Status,
                        Title = toolCallUpdate.Title,
                        Content = combinedContent.ToArray(),
                        RawInput = toolCallUpdate.RawInput ?? lastToolCall.RawInput,
                        RawOutput = toolCallUpdate.RawOutput ?? lastToolCall.RawOutput,
                    };

                    return default;
                }
            }

            messages.Add(update);

            return default;
        }

        public async ValueTask<WriteTextFileResponse> WriteTextFileAsync(WriteTextFileRequest request, CancellationToken cancellationToken = default)
        {
            var directory = Path.GetDirectoryName(request.Path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(request.Path, request.Content);

            return new WriteTextFileResponse();
        }

        public async ValueTask<ReadTextFileResponse> ReadTextFileAsync(ReadTextFileRequest request, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(
                request.Path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );

            using var reader = new StreamReader(
                stream,
                encoding: System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096
            );

            string content;

            if (request.Line.HasValue || request.Limit.HasValue)
            {
                content = await ReadLinesAsync(reader, request.Line ?? 1, request.Limit, cancellationToken);
            }
            else
            {
                content = await reader.ReadToEndAsync();
            }

            return new ReadTextFileResponse
            {
                Content = content
            };
        }

        async Task<string> ReadLinesAsync(StreamReader reader, uint startLine, uint? limit, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            uint currentLine = 1;
            uint linesRead = 0;

            // Skip lines
            while (currentLine < startLine && !reader.EndOfStream)
            {
                await reader.ReadLineAsync();
                currentLine++;
                cancellationToken.ThrowIfCancellationRequested();
            }

            while (!reader.EndOfStream)
            {
                if (limit.HasValue && linesRead >= limit.Value)
                {
                    break;
                }

                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }
                sb.Append(line);

                linesRead++;
                cancellationToken.ThrowIfCancellationRequested();
            }

            return sb.ToString();
        }

        public ValueTask<CreateTerminalResponse> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<TerminalOutputRequest> TerminalOutputAsync(TerminalOutputRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<ReleaseTerminalResponse> ReleaseTerminalAsync(ReleaseTerminalRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<WaitForTerminalExitResponse> WaitForTerminalExitAsync(WaitForTerminalExitRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<KillTerminalCommandResponse> KillTerminalCommandAsync(KillTerminalCommandRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<JsonElement> ExtMethodAsync(string method, JsonElement request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask ExtNotificationAsync(string method, JsonElement notification, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}