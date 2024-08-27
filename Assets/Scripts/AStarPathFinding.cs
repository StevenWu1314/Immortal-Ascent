using System;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class AStarPathFinding
{
    
    public static Vector2Int incrementPathFinding(Vector3 current, Vector3 target)
    {
        Vector3 direction = (target - current).normalized;
        Debug.Log(direction);
        if (math.abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return Vector2Int.RoundToInt(new Vector3(direction.x, 0).normalized);
        }
        else 
        {
            return Vector2Int.RoundToInt(new Vector3(0, direction.y).normalized);
        }
    }
}