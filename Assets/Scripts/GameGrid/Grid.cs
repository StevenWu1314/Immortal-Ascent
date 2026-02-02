using Unity.Mathematics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Grid
{
    private int width;
    private int height;
    private Vector3 originPosition;
    private float cellSize;
    private int[,] gridArray;
    public delegate void genLoot();
    public static event genLoot chestOpen;

    
    public Grid(int width, int height, float cellSize, Vector3 originPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        gridArray = new int[width, height];


        // for (int x = 0; x < gridArray.GetLength(0); x++)
        // {
        //     Debug.DrawLine(GetWorldPosition(x, 0), GetWorldPosition(x, height), Color.white, 100f);
        // }
        // for (int y = 0 ; y < gridArray.GetLength(1); y++)
        // {
        //     Debug.DrawLine(GetWorldPosition(0, y), GetWorldPosition(width, y), Color.white, 100f);
                
        // }
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y) * cellSize + originPosition;
    }
    
    public int GetValueAtLocation(int x, int y)
    {
        return gridArray[x, y];
    }

    public int GetValueAtWorldLocation(Vector3 worldPos)
    {
        int x, y;
        getXY(worldPos, out x, out y);
        return gridArray[x, y];
    }
    public void SetValueAtLocation(int x, int y, int value)
    {
        gridArray[x, y] = value;
        //Debug.Log("x: " + x + " y: " + y);
        //utilityFunction.createWorldText(gridArray[x, y].ToString(), null, GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 1);
    }

    public void SetValueAtWorldLocation(Vector3 location, int value)
    {
        int worldx, worldy;
        worldx = (int)location.x;
        worldy = (int)location.y;
        int x, y;
        getXY(new Vector3(worldx, worldy), out x, out y);
        gridArray[x, y] = value;
        utilityFunction.createWorldText(gridArray[x, y].ToString(), null, new Vector3(worldx, worldy) + new Vector3(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 1);
    }
    public int GetSize(int dimension)
    {
        return gridArray.GetLength(dimension);
    }

    private void getXY(Vector3 Pos, out int x, out int y)
    {
        if(Pos.x < 0)
        {
            Pos = new Vector3(math.floor(Pos.x), Pos.y);
        }
        if(Pos.y < 0)
        {
            Pos = new Vector3(Pos.x, math.floor(Pos.y));
        }
        x = (int)math.floor(Pos.x - originPosition.x);
        y = (int)math.floor(Pos.y - originPosition.y);

    }

    public bool Move(Vector3 position, Vector2Int direction, Transform entity)
    {
        float worldx, worldy;
        worldx = position.x;
        worldy = position.y;
        int x, y;
        getXY(new Vector3(worldx, worldy), out x, out y);
        int value = gridArray[x, y];
        Vector2Int targetDirection = new Vector2Int(x, y) + direction;
        int targetValue = gridArray[targetDirection.x, targetDirection.y];
        if (targetValue == 0)
        {
            return true;
        }
        else 
        {
            return false;
        }
    }

    public void printGrid() {
        GameObject[] worldtexts = GameObject.FindGameObjectsWithTag("worldtexts");
        for(int i = 0; i < worldtexts.GetLength(0); i++)
        {
            spawnPointBehavior.Destroy(worldtexts[i]);
        }
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < gridArray.GetLength(1); y++)
            {
                if(gridArray[x, y] != 0)
                {
                    utilityFunction.createWorldText(gridArray[x, y].ToString(), null, GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 1);
                }
                
            }
        }
    }


    public Enemy detectEnemiesInALine(Vector3 position, Vector2Int direction, int range)
    {
        float worldx, worldy;
        worldx = position.x;
        worldy = position.y;
        int x, y;
        getXY(new Vector3(worldx, worldy), out x, out y);
        for (int i = 0; i < range; i++)
        {
            x += direction.x;
            y += direction.y;
            int value = gridArray[x, y];
            if (value == 3) {
                Collider2D[] targets = Physics2D.OverlapCircleAll(position + new Vector3(i * direction.x, i * direction.y), 1);
                Enemy target = null;
                foreach (Collider2D collider in targets)
                {
                    Debug.Log(collider);
                    if (collider.gameObject.GetComponent<Enemy>() != null) {
                    
                        target = collider.gameObject.GetComponent<Enemy>();
                    }
                }
                return target;
            }     
        }
        return null;
        
    }
}
