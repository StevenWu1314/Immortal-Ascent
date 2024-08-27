using System;
using System.Collections;
using System.Collections.Generic;
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

    void Awake() {
        generator = (RoomFirstDungeonGenerator)target;
    }
    

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Create Dungeon"))
        {
            generator.generateDungeon();
        }
        if(GUILayout.Button("Create Branching Dungeon"))
        {
            generator.generateBranchingDungeon();
        }
    }
    
}