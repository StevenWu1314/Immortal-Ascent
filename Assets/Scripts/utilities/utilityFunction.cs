using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class utilityFunction
{
    public static TextMesh createWorldText(string text, Transform parent = null, Vector3 localposition = default(Vector3), int fontSize = 20, Color color = default(Color), TextAnchor textAnchor = TextAnchor.MiddleCenter, TextAlignment textAlignment = TextAlignment.Center, int sortingOrder = 0)
    {
        GameObject gameObject = new GameObject("World_Text", typeof(TextMesh));
        gameObject.tag = "worldtexts";
        Transform transform = gameObject.transform;
        transform.SetParent(parent, false);
        transform.localPosition = localposition;
        TextMesh textMesh = gameObject.GetComponent<TextMesh>();
        textMesh.anchor = textAnchor;
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = fontSize;
        textMesh.alignment = textAlignment;
        textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
        return textMesh;
    }

    /// <summary>
    /// Flashes a sprite through <paramref name="colors"/>, then restores white. Shared by the
    /// player and enemy hit indicators, which each carried their own copy of this.
    /// </summary>
    public static IEnumerator FlashSprite(SpriteRenderer renderer, Color[] colors, float interval)
    {
        foreach (Color color in colors)
        {
            if (renderer != null)
            {
                renderer.color = color;
            }
            yield return new WaitForSeconds(interval);
        }

        if (renderer != null)
        {
            renderer.color = Color.white;
        }
    }

    /// <summary>
    /// True when nothing is painted on <paramref name="blockers"/> along the cell line between
    /// the two points. Moved here verbatim from RangeAttackTilemap and RangeEnemyBehavior, which
    /// each carried their own identical copy.
    /// </summary>
    public static bool ClearLine(Tilemap blockers, Vector2Int start, Vector2Int target)
    {
        if (Mathf.Abs(target.y - start.y) < Mathf.Abs(target.x - start.x))
        {
            if (start.x > target.x)
            {
                return ClearLineLow(blockers, target, start);
            }
            else
            {
                return ClearLineLow(blockers, start, target);
            }
        }
        else
        {
            if (start.y > target.y)
            {
                return ClearLineHigh(blockers, target, start);
            }
            else
            {
                return ClearLineHigh(blockers, start, target);
            }
        }
    }

    private static bool ClearLineLow(Tilemap blockers, Vector2Int start, Vector2Int target)
    {
        Vector2Int current = start;
        if (blockers.GetTile((Vector3Int) current) != null)
        {
            return false;
        }
        int dy = target.y - start.y;
        int dx = target.x - start.x;
        int yi = 1;
        if (dy < 0)
        {
            yi = -1;
            dy = -dy;
        }
        int d = 2 * dy - dx;
        while (current.x != target.x)
        {
            if (d > 0)
            {
                current.y += yi;
                d += 2 * (dy - dx);
            }
            else
            {
                d += 2 * dy;
            }
            current.x++;
            if (blockers.GetTile((Vector3Int) current) != null)
            {
                return false;
            }
        }
        return true;
    }

    private static bool ClearLineHigh(Tilemap blockers, Vector2Int start, Vector2Int target)
    {
        Vector2Int current = start;
        if (blockers.GetTile((Vector3Int) current) != null)
        {
            return false;
        }
        int dy = target.y - start.y;
        int dx = target.x - start.x;
        int xi = 1;
        if (dx < 0)
        {
            xi = -1;
            dx = -dx;
        }
        int d = 2 * dx - dy;

        while (current.y != target.y)
        {
            if (d > 0)
            {
                current.x += xi;
                d += 2 * (dx - dy);
            }
            else
            {
                d += 2 * dx;
            }
            current.y++;
            if (blockers.GetTile((Vector3Int) current) != null)
            {
                return false;
            }
        }
        return true;
    }
}
