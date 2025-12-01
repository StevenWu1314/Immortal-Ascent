using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class NewCustomRuleTile : RuleTile<NewCustomRuleTile.Neighbor> {
    public Tile[] other;
    public Tile water;

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
}