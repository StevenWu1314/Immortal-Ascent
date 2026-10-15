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
                Debug.Log($"initiating combat between {occupant} and {entityGrid[newCell].name}");
                if (entity.GetComponent<PlayerStats>() != null)
                {
                    entityGrid.TryGetValue(newCell, out var enemy);
                    if (enemy.GetComponent<Enemy>() != null)
                        if(entity.GetComponent<Controls>().swordDrawn)
                            entity.GetComponent<PlayerStats>().attack("melee", enemy.GetComponent<Enemy>());
                        else
                        {
                            Debug.Log("You do not have a sword drawn");
                        }
                }
            }
            else
            {
                entityGrid.Remove(oldCell);
                entityGrid[newCell] = entity;
                // Assign the cell position outright instead of adding a delta. The transform must end
                // up exactly where the registry says it is: accumulating deltas preserved any residue
                // (a hit shake leaves up to 0.1), and since an integer position is a cell corner,
                // FloorToInt would then report the previous cell. The old += also doubled z every step
                // because the z term was the position's own z.
                entity.transform.position = new Vector3(newCell.x, newCell.y, entity.transform.position.z);
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

    /// <summary>
    /// Unregister an entity wherever it currently sits. Death teardown uses this instead of
    /// the cell-keyed overload so a stale or drifted position can never leave the entity
    /// registered forever, which used to make the cell permanently unregisterable.
    /// </summary>
    public void UnregisterEntity(GameObject entity)
    {
        if (entity == null)
        {
            return;
        }
        Vector2Int found = default;
        bool any = false;
        foreach (var pair in entityGrid)
        {
            if (pair.Value == entity)
            {
                found = pair.Key;
                any = true;
                break;
            }
        }
        if (any)
        {
            entityGrid.Remove(found);
        }
    }

    /// <summary> Get the entity at a given cell, or null if empty. </summary>
    public GameObject GetEntityAt(Vector2Int cell)
    {
        entityGrid.TryGetValue(cell, out var entity);
        return entity;
    }

    /// <summary>
    /// The cell an entity is currently registered in.
    ///
    /// Callers that are about to move an entity must use this instead of deriving the cell from
    /// transform.position. Visual effects (the damage shake) move the transform, and a rounded
    /// cell that disagrees with the registry makes MoveEntity's old-cell lookup fail -- which
    /// silently dropped every move.
    /// </summary>
    public bool TryGetCell(GameObject entity, out Vector2Int cell)
    {
        cell = default;
        if (entity == null)
        {
            return false;
        }
        foreach (var pair in entityGrid)
        {
            if (pair.Value == entity)
            {
                cell = pair.Key;
                return true;
            }
        }
        return false;
    }

    /// <summary>Read-only view of every registered entity, keyed by cell.</summary>
    public IReadOnlyDictionary<Vector2Int, GameObject> AllEntities => entityGrid;

    /// <summary> Check if a cell is occupied. </summary>
    public bool IsOccupied(Vector2Int cell)
    {
        return entityGrid.ContainsKey(cell);
    }

    public void Reset()
    {
        Instance.entityGrid = new();
    }
}
