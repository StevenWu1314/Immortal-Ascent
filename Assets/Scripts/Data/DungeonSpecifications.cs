using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="DungeonSpecifications",menuName = "PCG/DungeonSpecifications")]
public class DungeonSpecifications : ScriptableObject
{
    public BoundsInt Space;
    public int minWidth;
    public int minHeight;
    public int numberOfRooms;
}