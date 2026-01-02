using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MazeCell
{
    public int x, z;
    public bool isVisited = false;
    public bool[] walls = new bool[4]; // North, East, South, West
    public bool hasEnemy = false;
    public bool hasTrap = false;
    public bool hasExit = false;

    public MazeCell(int x, int z)
    {
        this.x = x;
        this.z = z;
        // Initially all walls are present
        for (int i = 0; i < 4; i++)
            walls[i] = true;
    }
}

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Configuration")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 3f;
    
    [Header("Maze Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject exitPrefab;
    public GameObject enemyPrefab;
    public GameObject trapPrefab;
    
    [Header("Maze Settings")]
    public float enemySpawnChance = 0.1f;
    public float trapSpawnChance = 0.15f;
    
    private MazeCell[,] mazeGrid;
    private List<Vector3> mazePositions = new List<Vector3>();
    
    [HideInInspector] public Transform mazeRoot;

    public void GenerateMaze()
    {
        if (mazeRoot != null)
        {
            DestroyImmediate(mazeRoot.gameObject);
        }
        
        mazeRoot = new GameObject("MazeRoot").transform;
        
        // Initialize grid
        mazeGrid = new MazeCell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                mazeGrid[x, z] = new MazeCell(x, z);
            }
        }
        
        // Generate maze using recursive backtracking
        GenerateMazeRecursive(0, 0);
        
        // Place enemies and traps
        PlaceMazeElements();
        
        // Build the visual maze
        BuildMazeVisuals();
    }

    private void GenerateMazeRecursive(int x, int z)
    {
        mazeGrid[x, z].isVisited = true;
        
        // Define directions: North, East, South, West
        int[] directions = { 0, 1, 2, 3 };
        ShuffleArray(directions);
        
        foreach (int direction in directions)
        {
            int newX = x, newZ = z;
            
            switch (direction)
            {
                case 0: newZ++; break; // North
                case 1: newX++; break; // East
                case 2: newZ--; break; // South
                case 3: newX--; break; // West
            }
            
            if (newX >= 0 && newX < width && newZ >= 0 && newZ < height && !mazeGrid[newX, newZ].isVisited)
            {
                // Remove walls between current and new cell
                mazeGrid[x, z].walls[direction] = false;
                int oppositeDirection = (direction + 2) % 4;
                mazeGrid[newX, newZ].walls[oppositeDirection] = false;
                
                GenerateMazeRecursive(newX, newZ);
            }
        }
    }

    private void PlaceMazeElements()
    {
        // Place exit at far corner
        mazeGrid[width - 1, height - 1].hasExit = true;
        
        // Place enemies and traps in unvisited cells
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (x == 0 && z == 0) continue; // Skip start position
                
                if (Random.value < enemySpawnChance)
                {
                    mazeGrid[x, z].hasEnemy = true;
                }
                
                if (Random.value < trapSpawnChance)
                {
                    mazeGrid[x, z].hasTrap = true;
                }
            }
        }
    }

    private void BuildMazeVisuals()
    {
        // Create floor
        if (floorPrefab != null)
        {
            Vector3 floorSize = new Vector3(width * cellSize, 0.1f, height * cellSize);
            GameObject floor = Instantiate(floorPrefab, transform.position, Quaternion.identity, mazeRoot);
            floor.transform.localScale = new Vector3(width, 1, height);
            floor.name = "MazeFloor";
        }
        
        // Build walls and place elements
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 cellPosition = new Vector3(x * cellSize, 0, z * cellSize) + transform.position;
                
                // Place walls where needed
                for (int i = 0; i < 4; i++)
                {
                    if (mazeGrid[x, z].walls[i] && wallPrefab != null)
                    {
                        Vector3 wallPosition = cellPosition;
                        Quaternion wallRotation = Quaternion.identity;
                        
                        switch (i)
                        {
                            case 0: // North
                                wallPosition += new Vector3(0, 0, cellSize * 0.5f);
                                break;
                            case 1: // East
                                wallPosition += new Vector3(cellSize * 0.5f, 0, 0);
                                wallRotation = Quaternion.Euler(0, 90, 0);
                                break;
                            case 2: // South
                                wallPosition += new Vector3(0, 0, -cellSize * 0.5f);
                                break;
                            case 3: // West
                                wallPosition += new Vector3(-cellSize * 0.5f, 0, 0);
                                wallRotation = Quaternion.Euler(0, 90, 0);
                                break;
                        }
                        
                        GameObject wall = Instantiate(wallPrefab, wallPosition, wallRotation, mazeRoot);
                        wall.name = $"Wall_{x}_{z}_{i}";
                    }
                }
                
                // Place exit
                if (mazeGrid[x, z].hasExit && exitPrefab != null)
                {
                    GameObject exit = Instantiate(exitPrefab, cellPosition + Vector3.up, Quaternion.identity, mazeRoot);
                    exit.name = "MazeExit";
                }
                
                // Place enemy
                if (mazeGrid[x, z].hasEnemy && enemyPrefab != null)
                {
                    GameObject enemy = Instantiate(enemyPrefab, cellPosition + Vector3.up, Quaternion.identity, mazeRoot);
                    enemy.name = $"Enemy_{x}_{z}";
                }
                
                // Place trap
                if (mazeGrid[x, z].hasTrap && trapPrefab != null)
                {
                    GameObject trap = Instantiate(trapPrefab, cellPosition + Vector3.up * 0.1f, Quaternion.identity, mazeRoot);
                    trap.name = $"Trap_{x}_{z}";
                }
            }
        }
    }

    private void ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    public Vector3 GetStartPosition()
    {
        return new Vector3(0, 1, 0) + transform.position;
    }

    public Vector3 GetExitPosition()
    {
        return new Vector3((width - 1) * cellSize, 1, (height - 1) * cellSize) + transform.position;
    }
}