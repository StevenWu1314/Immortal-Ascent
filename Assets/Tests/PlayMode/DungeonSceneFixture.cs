using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared setup for the PlayMode suites that need the real dungeon scene generated. Kept in one place
/// so the scene name and the timeout cannot drift between suites.
/// </summary>
internal static class DungeonSceneFixture
{
    public const string SceneName = "Branching dungeon";
    private const float GenerationTimeoutSeconds = 20f;

    /// <summary>Load the dungeon scene and wait until its generator has built a floor.</summary>
    public static IEnumerator LoadAndWaitForGeneration()
    {
        SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
        yield return null;

        float deadline = Time.realtimeSinceStartup + GenerationTimeoutSeconds;
        while ((Grid.Current == null || !IslandManager.IsInitialized) && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.IsNotNull(Grid.Current,
            $"Grid.Current is null after {GenerationTimeoutSeconds}s - generation did not run in '{SceneName}'");
        Assert.IsTrue(IslandManager.IsInitialized,
            "IslandManager was never rebuilt; generation did not complete");
    }
}
