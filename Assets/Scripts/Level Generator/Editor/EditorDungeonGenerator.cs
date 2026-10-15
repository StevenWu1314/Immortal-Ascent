using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomFirstDungeonGenerator), true)]
public class EditorDungeonGenerator : Editor
{
    private RoomFirstDungeonGenerator generator;

    // OnEnable, not Awake: Awake is not guaranteed on an Editor instance, and a null generator
    // made the button below throw.
    private void OnEnable()
    {
        generator = (RoomFirstDungeonGenerator)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Create Branching Dungeon"))
        {
            generator.generateBranchingDungeon();
        }
    }
}
