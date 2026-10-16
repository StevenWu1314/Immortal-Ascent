using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// Entry points that run the Unity Test Runner from outside the editor, so an agent can verify a
/// change without the designer closing Unity.
///
/// Usage through the UnityAgentClient bridge:
/// <code>POST /execute_method {"type":"TestRunnerMenu","method":"RunEditMode"}</code>
/// The call returns as soon as the run starts; read the console for
/// <c>[TESTRUN] ... complete: passed=N failed=N skipped=N</c> and for one <c>[TESTRUN] FAILED</c>
/// line per failure.
/// </summary>
public static class TestRunnerMenu
{
    private static TestRunnerApi api;
    private static RunCallbacks callbacks;

    [MenuItem("Tools/Tests/Run EditMode")]
    public static void RunEditMode()
    {
        Run(TestMode.EditMode);
    }

    [MenuItem("Tools/Tests/Run PlayMode")]
    public static void RunPlayMode()
    {
        Run(TestMode.PlayMode);
    }

    [MenuItem("Tools/Tests/Run All")]
    public static void RunAll()
    {
        Run(TestMode.EditMode, TestMode.PlayMode);
    }

    private static void Run(params TestMode[] modes)
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogWarning("[TESTRUN] the editor is still compiling; wait for it to settle and try again.");
            return;
        }

        if (api == null)
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
        }

        if (callbacks != null)
        {
            api.UnregisterCallbacks(callbacks);
        }

        callbacks = new RunCallbacks(modes);
        api.RegisterCallbacks(callbacks);
        api.Execute(new ExecutionSettings(new Filter { testMode = modes[0] }));

        // RunCallbacks already queues modes[1..]; do not enqueue them a second time here or
        // RunAll would execute the second mode twice.

        Debug.Log($"[TESTRUN] {string.Join("+", modes)} started");
    }

    private static void Finish(RunCallbacks finished)
    {
        if (api != null)
        {
            api.UnregisterCallbacks(finished);
        }

        if (ReferenceEquals(callbacks, finished))
        {
            callbacks = null;
        }
    }

    /// <summary>
    /// Writes the run summary into the project's Temp folder so the result can be read from outside the
    /// editor. Entering or leaving Play mode performs a domain reload, and a domain reload clears the
    /// Unity console buffer - so a PlayMode run's start and finish lines are erased before an external
    /// caller can read them, making the console useless as a PlayMode result channel. This file survives
    /// the reload. Temp/ is gitignored, so the result never reaches version control.
    /// </summary>
    private static void WriteResultFile(string mode, int passed, int failed, int skipped, List<string> failures)
    {
        try
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Temp"));
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, "testrun-" + mode + ".json");

            var entries = new List<string>(failures.Count);
            foreach (string failure in failures)
            {
                entries.Add("\"" + failure
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", " ")
                    .Replace("\n", " ") + "\"");
            }

            string json =
                "{\n" +
                "  \"mode\": \"" + mode + "\",\n" +
                "  \"passed\": " + passed + ",\n" +
                "  \"failed\": " + failed + ",\n" +
                "  \"skipped\": " + skipped + ",\n" +
                "  \"finishedUtc\": \"" + System.DateTime.UtcNow.ToString("o") + "\",\n" +
                "  \"failures\": [" + string.Join(", ", entries) + "]\n" +
                "}\n";

            System.IO.File.WriteAllText(path, json);
            Debug.Log($"[TESTRUN] result written to {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TESTRUN] could not write the result file: {e.Message}");
        }
    }

    private class RunCallbacks : ICallbacks
    {
        public readonly Queue<TestMode> pendingModes = new Queue<TestMode>();

        private readonly TestMode mode;
        private int passed;
        private int failed;
        private int skipped;
        private readonly List<string> failures = new List<string>();

        public RunCallbacks(TestMode[] modes)
        {
            mode = modes[0];
            for (int i = 1; i < modes.Length; i++)
            {
                pendingModes.Enqueue(modes[i]);
            }
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            passed = 0;
            failed = 0;
            skipped = 0;
            failures.Clear();
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test != null && result.Test.IsSuite)
            {
                return;
            }

            switch (result.TestStatus)
            {
                case TestStatus.Passed:
                    passed++;
                    break;
                case TestStatus.Failed:
                    failed++;
                    failures.Add($"{result.Name} :: {result.Message}");
                    break;
                case TestStatus.Skipped:
                case TestStatus.Inconclusive:
                    skipped++;
                    break;
            }
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"[TESTRUN] {mode} complete: passed={passed} failed={failed} skipped={skipped}");
            TestRunnerMenu.WriteResultFile(mode.ToString(), passed, failed, skipped, failures);

            foreach (string failure in failures)
            {
                Debug.LogError($"[TESTRUN] FAILED {failure}");
            }

            TestRunnerMenu.Finish(this);

            if (pendingModes.Count > 0 && failed == 0)
            {
                Run(pendingModes.ToArray());
            }
        }
    }
}
