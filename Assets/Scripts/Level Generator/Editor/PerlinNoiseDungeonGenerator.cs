using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PerlinNoiseMap), true)]
public class PerlinNoiseDungeonGenerator : Editor
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
        perlinNoiseMap = (PerlinNoiseMap)target;
    }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Create Perlin Noise Map"))
        {
            GameObject[] worldTexts = GameObject.FindGameObjectsWithTag("worldtexts");
            foreach (GameObject text in worldTexts)
                DestroyImmediate(text);
            perlinNoiseMap.GenerateMap();


        }
        if (GUILayout.Button("Clean up"))
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                DestroyImmediate(enemy);
            }
            GameObject[] worldTexts = GameObject.FindGameObjectsWithTag("worldtexts");
            foreach (GameObject text in worldTexts)
                DestroyImmediate(text);
            
                
        }
    }
    
}