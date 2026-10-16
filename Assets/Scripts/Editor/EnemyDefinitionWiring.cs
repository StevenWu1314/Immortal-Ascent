using UnityEditor;
using UnityEngine;

/// <summary>
/// Authors the three Plains enemy definitions (T09, GDD 13.1), assigns each to its prefab and puts them in the
/// realm's spawn pool. Values are the GDD's starting points - tune them in the assets afterwards, not here.
/// </summary>
public static class EnemyDefinitionWiring
{
    private const string Folder = "Assets/Resources/Enemies";

    private struct Spec
    {
        public string Prefab;
        public string Asset;
        public string Display;
        public int Hp;
        public int Damage;
        public int Xp;
        public float Detection;
        public int Chase;
        public int Range;
        public int Cooldown;
        public string DropTable;
    }

    [MenuItem("Tools/Content/Create Enemy Definitions")]
    public static void CreateEnemyDefinitions()
    {
        Spec[] specs =
        {
            new Spec { Prefab = "Assets/Prefabs/Enemies/Pig_yao.prefab", Asset = "PigYao", Display = "Pig Yao",
                       Hp = 50, Damage = 10, Xp = 10, Detection = 10f, Chase = 20, Range = 0, Cooldown = 2,
                       DropTable = "Pig_yao" },
            new Spec { Prefab = "Assets/Prefabs/Enemies/Spider_yao.prefab", Asset = "SpiderYao", Display = "Spider Yao",
                       Hp = 40, Damage = 8, Xp = 10, Detection = 10f, Chase = 20, Range = 0, Cooldown = 2,
                       DropTable = "Spider_yao" },
            new Spec { Prefab = "Assets/Prefabs/Enemies/jing_yao.prefab", Asset = "JingYao", Display = "Jing Yao",
                       Hp = 30, Damage = 10, Xp = 10, Detection = 10f, Chase = 10, Range = 5, Cooldown = 2,
                       DropTable = "Jing_yao" }
        };

        System.IO.Directory.CreateDirectory(Folder);
        GameObject[] pool = new GameObject[specs.Length];

        for (int i = 0; i < specs.Length; i++)
        {
            Spec spec = specs[i];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.Prefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[CONTENT] prefab '{spec.Prefab}' not found; skipping {spec.Asset}.");
                continue;
            }

            string path = $"{Folder}/{spec.Asset}.asset";
            EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            bool created = definition == null;
            if (created)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            }

            // Only fill an asset that does not exist yet, so re-running never overwrites tuned numbers.
            if (created)
            {
                definition.displayName = spec.Display;
                definition.maxHealth = spec.Hp;
                definition.damage = spec.Damage;
                definition.experience = spec.Xp;
                definition.detectionRadius = spec.Detection;
                definition.chaseRange = spec.Chase;
                definition.range = spec.Range;
                definition.rangedCooldown = spec.Cooldown;
                definition.dropTable = Resources.Load<MaterialDropTable>($"DropTables/{spec.DropTable}");
                AssetDatabase.CreateAsset(definition, path);
            }

            Assign(prefab, definition);
            pool[i] = prefab;
        }

        AssetDatabase.SaveAssets();

        RealmDefinition plains = AssetDatabase.LoadAssetAtPath<RealmDefinition>("Assets/Resources/Realms/Plains.asset");
        if (plains == null)
        {
            Debug.LogWarning("[CONTENT] definitions are authored, but Resources/Realms/Plains.asset does not exist, " +
                             "so the realm pool was not set. Run Tools/Content/Create Plains Realm Asset, then this " +
                             "menu item again.");
            return;
        }

        plains.enemies = pool;
        EditorUtility.SetDirty(plains);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CONTENT] enemy definitions authored, assigned to their prefabs, and {pool.Length} prefabs set " +
                  "as the Plains pool. Tune the numbers in the assets.");
    }

    private static void Assign(GameObject prefab, EnemyDefinition definition)
    {
        Enemy enemy = prefab.GetComponentInChildren<Enemy>();
        if (enemy == null)
        {
            Debug.LogWarning($"[CONTENT] '{prefab.name}' has no Enemy component; its definition was not assigned.");
            return;
        }

        SerializedObject serialized = new SerializedObject(enemy);
        SerializedProperty property = serialized.FindProperty("definition");
        if (property == null)
        {
            Debug.LogWarning($"[CONTENT] '{prefab.name}': Enemy has no 'definition' field to assign.");
            return;
        }
        property.objectReferenceValue = definition;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(prefab);
    }
}
