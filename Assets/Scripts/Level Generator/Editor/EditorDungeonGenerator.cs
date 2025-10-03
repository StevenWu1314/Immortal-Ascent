using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomFirstDungeonGenerator), true)]
public class EditorDungeonGenerator : Editor
{
    [SerializeField]
    public TilePlacer tilePlacer;
    public BoundsInt Space;
    public int minWidth;
    public int minHeight;
    public int numberOfRooms;
    RoomFirstDungeonGenerator generator;
    public PerlinNoiseMap perlinNoiseMap;

    void Awake()
    {
        generator = (RoomFirstDungeonGenerator)target;
        perlinNoiseMap = (PerlinNoiseMap)target;
    }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Create Dungeon"))
        {
            generator.generateDungeon();
        }
        if (GUILayout.Button("Create Branching Dungeon"))
        {
            generator.generateBranchingDungeon();
        }
        
        if (GUILayout.Button("Create Perlin Noise Map"))
        {
            perlinNoiseMap.GenerateMap();
        }
    }
    
}