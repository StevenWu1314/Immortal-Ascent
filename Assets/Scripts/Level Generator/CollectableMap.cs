using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableMap : MonoBehaviour
{
    public static CollectableMap Instance { get; private set; }
    // Maps cell -> Collectible
    private Dictionary<Vector2Int, Collectables> collectibleGrid = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void registerCollectable(Collectables collectable, Vector2Int position)
    {
        if (collectibleGrid.ContainsKey(position))
        {
            Debug.LogWarning($"Cell {position} already occupied ");
            return;
        }
        collectibleGrid[position] = collectable;
    }

    public Collectables GetCollectableAt(Vector2Int cell)
    {
        collectibleGrid.TryGetValue(cell, out var entity);
        return entity;
    }

    public void unregisterCollectable(Vector2Int position)
    {
        if (collectibleGrid.ContainsKey(position))
        {
            collectibleGrid[position] = null;
            return;
        }
        
    }
}
