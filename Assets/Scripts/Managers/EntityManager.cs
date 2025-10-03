using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks dynamic entities (player, enemies, NPCs, etc.)
/// in a grid-aligned map for fast collision checks.
/// Only one entity allowed per cell.
/// </summary>
public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { get; private set; }

    // Maps cell -> entity
    private Dictionary<Vector2Int, GameObject> entityGrid = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary> Register an entity at a given cell. </summary>
    public void RegisterEntity(GameObject entity, Vector2Int cell)
    {
        if (entityGrid.ContainsKey(cell))
        {
            Debug.LogWarning($"Cell {cell} already occupied by {entityGrid[cell].name}. Cannot register {entity.name}.");
            return;
        }
        entityGrid[cell] = entity;
    }

    /// <summary> Move entity from one cell to another. </summary>
    public void MoveEntity(GameObject entity, Vector2Int oldCell, Vector2Int newCell)
    {
        if (entityGrid.TryGetValue(oldCell, out var occupant) && occupant == entity)
        {
            if (entityGrid.ContainsKey(newCell))
            {
                Debug.LogWarning($"initiating combat between {occupant} and {entityGrid[newCell].name}");
                if (entity.GetComponent<PlayerStats>() != null)
                {
                    entityGrid.TryGetValue(newCell, out var enemy);
                    if (enemy.GetComponent<Enemy>() != null)
                        entity.GetComponent<PlayerStats>().attack("melee", enemy.GetComponent<Enemy>());

                }
            }
            else
            {
                entityGrid.Remove(oldCell);
                entityGrid[newCell] = entity;
                entity.transform.position += new Vector3(newCell.x - oldCell.x, newCell.y - oldCell.y, entity.transform.position.z);
            }
        }
        else
        {
            Debug.Log("Error, the entity you're trying to move does not exits in this cell");
        }
    }

    /// <summary> Unregister entity completely. </summary>
    public void UnregisterEntity(GameObject entity, Vector2Int cell)
    {
        if (entityGrid.TryGetValue(cell, out var occupant) && occupant == entity)
        {
            entityGrid.Remove(cell);
        }
    }

    /// <summary> Get the entity at a given cell, or null if empty. </summary>
    public GameObject GetEntityAt(Vector2Int cell)
    {
        entityGrid.TryGetValue(cell, out var entity);
        return entity;
    }

    /// <summary> Check if a cell is occupied. </summary>
    public bool IsOccupied(Vector2Int cell)
    {
        return entityGrid.ContainsKey(cell);
    }
}
