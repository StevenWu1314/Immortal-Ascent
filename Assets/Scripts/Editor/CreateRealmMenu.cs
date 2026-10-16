using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Authors the realm assets the run reads from <c>Resources/Realms</c>. Run once per realm; the run
/// falls back to a built-in plains definition when the folder is empty, so this is a data-authoring
/// convenience rather than a prerequisite for playing.
/// </summary>
public static class CreateRealmMenu
{
    private const string Folder = "Assets/Resources/Realms";

    [MenuItem("Tools/Content/Create Plains Realm Asset")]
    public static void CreatePlainsRealm()
    {
        string path = Folder + "/Plains.asset";
        if (File.Exists(path))
        {
            Debug.Log($"[CONTENT] {path} already exists; nothing to do.");
            return;
        }

        Directory.CreateDirectory(Folder);

        RealmDefinition plains = ScriptableObject.CreateInstance<RealmDefinition>();
        plains.realmIndex = 0;
        plains.displayName = "Plains";
        plains.floorsInRealm = 2;   // GDD 3.2: 2-3 floors per realm; M1 ships two.

        AssetDatabase.CreateAsset(plains, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CONTENT] created {path}. Edit it in the Inspector to retune the realm.");
    }
}
