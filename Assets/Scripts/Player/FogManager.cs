using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FogManager : MonoBehaviour
{
    public static FogManager Instance;

    [SerializeField] Tilemap fogLayer;
    [SerializeField] Tilemap dimLayer;
    [SerializeField] TileBase fogTile;
    [SerializeField] TileBase[] dimTiles;
    [SerializeField] int sightRadius = 3;

    public enum TileVisibility {
        Undiscovered,
        Discovered,
        Visible
    }
    TileVisibility[,] fogMap;

    int mapWidth;
    int mapHeight;
    Vector2Int mapOffset;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Initialize(int width, int height, Vector2Int offset) {
        Debug.Log("FogManager Initialize called");
        mapWidth = width;
        mapHeight = height;
        mapOffset = offset;
        fogMap = new TileVisibility[width, height];
        GenerateFog();
    }

    void GenerateFog() {
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                fogMap[x,y] = TileVisibility.Undiscovered;
                Vector3Int tile_position = new Vector3Int(x + mapOffset.x, y + mapOffset.y, 0);
                fogLayer.SetTile(tile_position, fogTile);
            }
        }
    }

    public void ClearFog() {
        fogLayer.ClearAllTiles();
        dimLayer.ClearAllTiles();
    }

    public void DiscoveredTiles(Vector2Int player_position) {
        SetVisibleToDiscovered(); // run FIRST to dim previously visible tiles
        for (int x = player_position.x - sightRadius; x <= player_position.x + sightRadius; x++) {
            for (int y = player_position.y - sightRadius; y <= player_position.y + sightRadius; y++) {
                float distance = Vector2.Distance(new Vector2(x,y), new Vector2(player_position.x, player_position.y));
                if (distance > sightRadius) continue;
                int fx = x - mapOffset.x;
                int fy = y - mapOffset.y;
                if (fx < 0 || fx >= mapWidth || fy < 0 || fy >= mapHeight) continue;
                fogMap[fx, fy] = TileVisibility.Visible;
                Vector3Int tile_position = new Vector3Int(x, y, 0);
                fogLayer.SetTile(tile_position, null);
                dimLayer.SetTile(tile_position, null);
            }
        }
    }

    void SetVisibleToDiscovered() {
        Vector2Int player_position = Vector2Int.FloorToInt(PlayerStats.Instance.transform.position);
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (fogMap[x, y] == TileVisibility.Visible)
                {
                    fogMap[x, y] = TileVisibility.Discovered;
                    Vector3Int tilePos = new Vector3Int(x + mapOffset.x, y + mapOffset.y, 0);
                    float dist = Vector2.Distance(new Vector2(x, y), player_position);
                    Debug.Log($"dist: {dist}, chosen: {(dist < 2 ? 0 : dist < 4 ? 1 : 2)}");
                    TileBase chosenTile = dist < 4 ? dimTiles[0] : dist < 6 ? dimTiles[1] : dimTiles[2];
                    dimLayer.SetTile(tilePos, chosenTile);
                }
            }
        }
    }
}
