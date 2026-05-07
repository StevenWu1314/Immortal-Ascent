
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class NewCustomRuleTile : RuleTile<NewCustomRuleTile.Neighbor> {
    public Tile[] other;
    public Tile water;
    public string[] additionalTilemapTags;
    private Tilemap[] _cachedAdditionalTilemaps;

    private Tilemap[] GetAdditionalTilemaps()
    {
        if (_cachedAdditionalTilemaps != null) return _cachedAdditionalTilemaps;

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
        return _cachedAdditionalTilemaps;
    }
    public class Neighbor : RuleTile.TilingRule.Neighbor {
        public const int otherOrThis = 3;
        public const int other = 4;
        public const int water = 5;
        
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case Neighbor.otherOrThis: return tile == this || other.Contains(tile);
            case Neighbor.other: return other.Contains(tile);
            case Neighbor.water: return tile == water;
        }
        return base.RuleMatch(neighbor, tile);
    }

    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
    {
        // For each neighbor position in the rule, check BOTH the native tilemap
        // AND any additional tilemaps
        for (int i = 0; i < rule.m_Neighbors.Count && i < rule.m_NeighborPositions.Count; i++)
        {
            int neighbor = rule.m_Neighbors[i];
            Vector3Int neighborPos = position + rule.m_NeighborPositions[i];

            // Get the tile from the primary tilemap
            TileBase primaryTile = tilemap.GetTile(neighborPos);

            // Check additional tilemaps if primary is empty
            TileBase resolvedTile = primaryTile;
            if (resolvedTile == null)
            {
                foreach (var additionalMap in GetAdditionalTilemaps())
                {
                    var t = additionalMap.GetTile(neighborPos);
                    if (t != null)
                    {
                        resolvedTile = t;
                        break;
                    }
                }
            }

            if (!RuleMatch(neighbor, resolvedTile))
                return false;
        }

        return true;
    }

}