using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Desktop build entry points. The project's active build target is recorded as WebGL, which does
/// not match the design (Windows + macOS), so switching is an explicit, deliberate step rather than
/// something that happens during a normal play session: switching targets forces a long reimport.
///
/// Usage through the UnityAgentClient bridge:
/// <code>POST /execute_method {"type":"BuildMenu","method":"ReportActiveTarget"}</code>
/// </summary>
public static class BuildMenu
{
    private const string MacOutput = "Builds/macOS/RogueAsianFantasy.app";
    private const string WindowsOutput = "Builds/Windows/RogueAsianFantasy.exe";

    [MenuItem("Tools/Build/Report Active Target")]
    public static void ReportActiveTarget()
    {
        Debug.Log($"[BUILD] active target = {EditorUserBuildSettings.activeBuildTarget}, " +
                  $"host platform = {Application.platform}, " +
                  $"enabled scenes = {string.Join(", ", EnabledScenes())}");
    }

    [MenuItem("Tools/Build/Switch Target To This Platform")]
    public static void SwitchToDesktop()
    {
        BuildTarget target = Application.platform == RuntimePlatform.OSXEditor
            ? BuildTarget.StandaloneOSX
            : BuildTarget.StandaloneWindows64;

        if (EditorUserBuildSettings.activeBuildTarget == target)
        {
            Debug.Log($"[BUILD] already on {target}; nothing to switch");
            return;
        }

        bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, target);
        Debug.Log(switched
            ? $"[BUILD] switched active target to {target} (this triggers a long reimport)"
            : $"[BUILD] switch to {target} FAILED - is that platform's build support module installed?");
    }

    [MenuItem("Tools/Build/Build macOS")]
    public static void BuildMac()
    {
        Build(BuildTarget.StandaloneOSX, MacOutput);
    }

    [MenuItem("Tools/Build/Build Windows")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, WindowsOutput);
    }

    private static string[] EnabledScenes()
    {
        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        return scenes.ToArray();
    }

    private static void Build(BuildTarget target, string outputPath)
    {
        string[] scenes = EnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[BUILD] no enabled scenes in Build Settings; refusing to build.");
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            Debug.LogWarning($"[BUILD] active target is {EditorUserBuildSettings.activeBuildTarget}, " +
                             $"not {target}. Unity will switch (long reimport) before building.");
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        string message = $"[BUILD] {target} -> {outputPath}: {summary.result}, " +
                         $"{summary.totalErrors} error(s), {summary.totalSize} bytes, {summary.totalTime}";
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log(message);
        }
        else
        {
            Debug.LogError(message);
        }
    }
}
