
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class NewCustomRuleTile : RuleTile<NewCustomRuleTile.Neighbor>
{
    public Tile[] other;
    public Tile water;
    public string[] additionalTilemapTags;

    // NOT cached as a field — look up fresh each play session
    [System.NonSerialized]
    private Tilemap[] _cachedAdditionalTilemaps;
    [System.NonSerialized]
    private bool _cacheInitialized = false;

    private Tilemap[] GetAdditionalTilemaps()
    {
        // Validate existing cache — destroy surviving nulls after domain reload
        if (_cacheInitialized && _cachedAdditionalTilemaps != null)
        {
            bool allValid = System.Array.TrueForAll(
                _cachedAdditionalTilemaps,
                tm => tm != null
            );
            if (allValid) return _cachedAdditionalTilemaps;
        }

        // Only search for tilemaps when actually in play mode
        if (!Application.isPlaying)
        {
            return System.Array.Empty<Tilemap>();
        }

        var maps = new System.Collections.Generic.List<Tilemap>();
        foreach (var tag in additionalTilemapTags)
        {
            foreach (var go in GameObject.FindGameObjectsWithTag(tag))
            {
                var tm = go.GetComponent<Tilemap>();
                if (tm != null) maps.Add(tm);
            }
        }

        _cachedAdditionalTilemaps = maps.ToArray();
        _cacheInitialized = true;
        return _cachedAdditionalTilemaps;
    }


    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int otherOrThis = 3;
        public const int other = 4;
        public const int water = 5;
    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        switch (neighbor)
        {
            case Neighbor.otherOrThis: return tile == this || other.Contains(tile);
            case Neighbor.other:       return other.Contains(tile);
            case Neighbor.water:       return tile == water;
        }
        return base.RuleMatch(neighbor, tile);
    }

    public override bool RuleMatches(TilingRule rule, Vector3Int position,
        ITilemap tilemap, ref Matrix4x4 transform)
    {
        for (int i = 0; i < rule.m_Neighbors.Count && i < rule.m_NeighborPositions.Count; i++)
        {
            int neighbor = rule.m_Neighbors[i];
            Vector3Int neighborPos = position + rule.m_NeighborPositions[i];

            TileBase primaryTile = tilemap.GetTile(neighborPos);

            TileBase resolvedTile = primaryTile;
            if (resolvedTile == null)
            {
                foreach (var additionalMap in GetAdditionalTilemaps())
                {
                    var t = additionalMap.GetTile(neighborPos);
                    if (t != null) { resolvedTile = t; break; }
                }
            }

            if (!RuleMatch(neighbor, resolvedTile))
                return false;
        }
        return true;
    }
}