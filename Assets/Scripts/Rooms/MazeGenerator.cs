using System;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour {
    [Header("Grid Settings")]
    [SerializeField] private int width = 50;      // Number of cells in X
    [SerializeField] private int height = 50;     // Number of cells in Z
    [SerializeField] private float cellSize = 2f; // 100x100 plane -> 50x50 grid -> 2 units per cell

    [Header("Entrance / Exit (World Space)")]
    [SerializeField] private Transform pointA;    // Entrance transform
    [SerializeField] private Transform pointB;    // Exit transform

    [Header("Visuals")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private Transform mazeRoot;  // Center of the maze and parent of all walls

    [Header("Controls")]
    [SerializeField] private KeyCode generateKey = KeyCode.F;

    [Header("Props")]
    [SerializeField] private GameObject[] props;
    [SerializeField, Range(0f, 1f)] private float propSpawnChance = 0.05f;
    [SerializeField] private int propSpawnSeedOffset = 1000;

    [Header("Enemies & Traps")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject[] trapPrefabs;
    [SerializeField, Range(0f, 1f)] public float enemyDensityOnPath = 0.04f; // 4% of path cells
    [SerializeField, Range(0f, 1f)] public float trapDensityOnPath = 0.05f;  // 5% of path cells
    [SerializeField] private float minCellSpacingBetweenHazards = 3f;          // World distance

    private class MazeCell {
        public bool wallN = true;
        public bool wallS = true;
        public bool wallE = true;
        public bool wallW = true;
    }

    private MazeCell[,] cells;
    private readonly List<GameObject> spawnedWalls = new();
    private readonly List<GameObject> spawnedProps = new();
    private readonly List<GameObject> spawnedEnemies = new();
    private readonly List<GameObject> spawnedTraps = new();

    private System.Random rng = new System.Random();

    // Main path A -> B
    private Vector2Int startCell;
    private Vector2Int endCell;

    // Debug renderers
    private LineRenderer mainPathRenderer;   // A -> B path

    private void Awake() {
        // First LineRenderer: main A->B path
        mainPathRenderer = GetComponent<LineRenderer>();
        mainPathRenderer.positionCount = 0;
        mainPathRenderer.widthMultiplier = 0.15f;
        mainPathRenderer.material = new Material(Shader.Find("Sprites/Default"));
        mainPathRenderer.startColor = Color.green;
        mainPathRenderer.endColor = Color.red;
    }

    private void Start() {
        GenerateMaze();
    }

    private void Update() {
        if (Input.GetKeyDown(generateKey))
            GenerateMaze();

    }

    /// <summary>
    /// Main entry to generate a new maze.
    /// </summary>
    public void GenerateMaze() {
        ClearMaze();
        mainPathRenderer.positionCount = 0;

        // Create cell grid
        cells = new MazeCell[width, height];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                cells[x, z] = new MazeCell();

        // Map world-space A/B to nearest grid cells
        startCell = ClampCell(WorldToCell(pointA.position));
        endCell = ClampCell(WorldToCell(pointB.position));

        rng = new System.Random(rng.Next());

        // 1) Create an initial backbone path from A to B
        CreateBackbone(startCell, endCell);

        // 2) Fill the rest using Prim's algorithm to get a perfect maze
        PrimsFill(startCell);

        // 3) Ensure entrance/exit cuts on the outer border for A/B
        OpenEntranceExit(startCell, endCell);

        // 4) Build 3D walls
        BuildWalls();

        // 5) Place decorative props
        SpawnProps();

        // 6) Compute and draw main path A->B, and spawn enemies/traps on it
        DrawMainPathAndHazards();
    }

    /// <summary>
    /// Convert world position to grid cell index, assuming mazeRoot is the maze center.
    /// </summary>
    private Vector2Int WorldToCell(Vector3 worldPos) {
        Vector3 local = worldPos - mazeRoot.position;

        float originOffsetX = -(width * cellSize) * 0.5f;
        float originOffsetZ = -(height * cellSize) * 0.5f;

        float fx = (local.x - originOffsetX) / cellSize;
        float fz = (local.z - originOffsetZ) / cellSize;

        int x = Mathf.FloorToInt(fx);
        int z = Mathf.FloorToInt(fz);
        return new Vector2Int(x, z);
    }

    private Vector2Int ClampCell(Vector2Int c) {
        c.x = Mathf.Clamp(c.x, 0, width - 1);
        c.y = Mathf.Clamp(c.y, 0, height - 1);
        return c;
    }

    /// <summary>
    /// Greedy random walk from start to end to create a skeleton path.
    /// </summary>
    private void CreateBackbone(Vector2Int start, Vector2Int end) {
        HashSet<Vector2Int> visited = new();
        List<Vector2Int> path = new();

        Vector2Int current = start;
        visited.Add(current);
        path.Add(current);

        int safety = width * height * 4;

        while (current != end && safety-- > 0) {
            List<Vector2Int> neighbors = GetNeighbors(current);
            List<Vector2Int> unvisited = new();
            foreach (var n in neighbors)
                if (!visited.Contains(n))
                    unvisited.Add(n);

            if (unvisited.Count == 0)
                break;

            // Move mainly towards end, but keep randomness
            unvisited.Sort((a, b) =>
                Vector2Int.Distance(a, end).CompareTo(Vector2Int.Distance(b, end)));

            int pickIndex = rng.Next(Mathf.Min(3, unvisited.Count));
            Vector2Int next = unvisited[pickIndex];

            RemoveWallBetween(current, next);

            current = next;
            visited.Add(current);
            path.Add(current);
        }
    }

    /// <summary>
    /// Prim's algorithm to fill remaining cells and form a spanning tree.
    /// </summary> 
    private void PrimsFill(Vector2Int start) {
        HashSet<Vector2Int> inMaze = new();
        HashSet<Vector2Int> frontier = new();

        // Flood fill from start across already opened edges (backbone)
        Queue<Vector2Int> q = new();
        q.Enqueue(start);
        inMaze.Add(start);

        while (q.Count > 0) {
            var c = q.Dequeue();
            foreach (var n in GetNeighbors(c)) {
                if (!inMaze.Contains(n) && !HasWallBetween(c, n)) {
                    inMaze.Add(n);
                    q.Enqueue(n);
                }
            }
        }

        // Initialize frontier: neighbors of inMaze
        foreach (var c in inMaze)
            foreach (var n in GetNeighbors(c))
                if (!inMaze.Contains(n))
                    frontier.Add(n);

        // Prim's loop
        while (frontier.Count > 0) {
            int idx = rng.Next(frontier.Count);
            Vector2Int active = default;
            int i = 0;
            foreach (var f in frontier) {
                if (i == idx) { active = f; break; }
                i++;
            }
            frontier.Remove(active);

            // Find neighbor already in the maze
            List<Vector2Int> neighbors = GetNeighbors(active);
            List<Vector2Int> inMazeNeighbors = new();
            foreach (var n in neighbors)
                if (inMaze.Contains(n))
                    inMazeNeighbors.Add(n);

            if (inMazeNeighbors.Count == 0)
                continue;

            Vector2Int connectTo = inMazeNeighbors[rng.Next(inMazeNeighbors.Count)];
            RemoveWallBetween(active, connectTo);
            inMaze.Add(active);

            foreach (var n in GetNeighbors(active))
                if (!inMaze.Contains(n))
                    frontier.Add(n);
        }
    }

    private List<Vector2Int> GetNeighbors(Vector2Int c) {
        List<Vector2Int> list = new();

        if (c.x > 0) list.Add(new Vector2Int(c.x - 1, c.y));
        if (c.x < width - 1) list.Add(new Vector2Int(c.x + 1, c.y));
        if (c.y > 0) list.Add(new Vector2Int(c.x, c.y - 1));
        if (c.y < height - 1) list.Add(new Vector2Int(c.x, c.y + 1));

        return list;
    }

    private void RemoveWallBetween(Vector2Int a, Vector2Int b) {
        if (a.x == b.x) {
            if (a.y < b.y) {
                cells[a.x, a.y].wallN = false;
                cells[b.x, b.y].wallS = false;
            } else {
                cells[a.x, a.y].wallS = false;
                cells[b.x, b.y].wallN = false;
            }
        } else if (a.y == b.y) {
            if (a.x < b.x) {
                cells[a.x, a.y].wallE = false;
                cells[b.x, b.y].wallW = false;
            } else {
                cells[a.x, a.y].wallW = false;
                cells[b.x, b.y].wallE = false;
            }
        }
    }

    private bool HasWallBetween(Vector2Int a, Vector2Int b) {
        if (a.x == b.x) {
            if (a.y < b.y) return cells[a.x, a.y].wallN || cells[b.x, b.y].wallS;
            else return cells[a.x, a.y].wallS || cells[b.x, b.y].wallN;
        } else if (a.y == b.y) {
            if (a.x < b.x) return cells[a.x, a.y].wallE || cells[b.x, b.y].wallW;
            else return cells[a.x, a.y].wallW || cells[b.x, b.y].wallE;
        }
        return true;
    }

    /// <summary>
    /// Cut entrance and exit walls on the outer border closest to A/B cells.
    /// </summary>
    private void OpenEntranceExit(Vector2Int start, Vector2Int end) {
        // Start cell
        if (start.y == 0) cells[start.x, start.y].wallS = false;
        else if (start.y == height - 1) cells[start.x, start.y].wallN = false;
        else if (start.x == 0) cells[start.x, start.y].wallW = false;
        else if (start.x == width - 1) cells[start.x, start.y].wallE = false;

        // End cell
        if (end.y == 0) cells[end.x, end.y].wallS = false;
        else if (end.y == height - 1) cells[end.x, end.y].wallN = false;
        else if (end.x == 0) cells[end.x, end.y].wallW = false;
        else if (end.x == width - 1) cells[end.x, end.y].wallE = false;
    }

    /// <summary>
    /// Instantiate walls around each cell so that mazeRoot is at the center of the maze.
    /// </summary>
    private void BuildWalls() {
        if (!wallPrefab || !mazeRoot) {
            Debug.LogError("WallPrefab or MazeRoot is not assigned!");
            return;
        }

        float wallHeight = wallPrefab.transform.localScale.y;
        Vector3 origin = mazeRoot.position - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f);

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < height; z++) {
                Vector3 cellPos = origin + new Vector3(x * cellSize, 0f, z * cellSize);
                MazeCell cell = cells[x, z];

                if (cell.wallN)
                    SpawnWall(cellPos + new Vector3(0f, 0f, cellSize * 0.5f),
                              new Vector3(cellSize, wallHeight, 0.1f));
                if (cell.wallS)
                    SpawnWall(cellPos + new Vector3(0f, 0f, -cellSize * 0.5f),
                              new Vector3(cellSize, wallHeight, 0.1f));
                if (cell.wallE)
                    SpawnWall(cellPos + new Vector3(cellSize * 0.5f, 0f, 0f),
                              new Vector3(0.1f, wallHeight, cellSize));
                if (cell.wallW)
                    SpawnWall(cellPos + new Vector3(-cellSize * 0.5f, 0f, 0f),
                              new Vector3(0.1f, wallHeight, cellSize));
            }
        }
    }

    private void SpawnWall(Vector3 pos, Vector3 scale) {
        GameObject w = Instantiate(wallPrefab, pos, Quaternion.identity, mazeRoot);
        w.transform.localScale = scale;
        spawnedWalls.Add(w);
    }

    /// <summary>
    /// Place decorative props randomly inside walkable cells.
    /// </summary>
    private void SpawnProps() {
        if (props == null || props.Length == 0)
            return;

        System.Random propRng = new(rng.Next() + propSpawnSeedOffset);
        Vector3 origin = mazeRoot.position - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f);

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < height; z++) {
                // Optionally skip border cells
                if (x == 0 || z == 0 || x == width - 1 || z == height - 1)
                    continue;

                if (propRng.NextDouble() > propSpawnChance)
                    continue;

                // Skip fully closed cells (isolated)
                if (cells[x, z].wallN && cells[x, z].wallS && cells[x, z].wallE && cells[x, z].wallW)
                    continue;

                GameObject prefab = props[propRng.Next(props.Length)];
                if (prefab == null) continue;

                Vector3 cellCenter = origin + new Vector3(x * cellSize, 0f, z * cellSize);
                float offsetX = (float)(propRng.NextDouble() - 0.5) * (cellSize * 0.4f);
                float offsetZ = (float)(propRng.NextDouble() - 0.5) * (cellSize * 0.4f);
                Vector3 spawnPos = cellCenter + new Vector3(offsetX, 0f, offsetZ);

                GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity, mazeRoot);
                spawnedProps.Add(instance);
            }
        }
    }

    /// <summary>
    /// Compute the main path from A to B, draw it, and spawn enemies/traps on it.
    /// </summary>
    private void DrawMainPathAndHazards() {
        List<Vector2Int> path = BfsPath(startCell, endCell);
        if (path == null || path.Count == 0) {
            Debug.LogWarning("No path found between A and B (should not happen in a perfect maze).");
            return;
        }

        Vector3 origin = mazeRoot.position - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f);
        Vector3[] points = new Vector3[path.Count];

        for (int i = 0; i < path.Count; i++) {
            Vector2Int c = path[i];
            Vector3 cellCenter = origin + new Vector3(c.x * cellSize, 0.1f, c.y * cellSize);
            points[i] = cellCenter;
        }

        mainPathRenderer.positionCount = points.Length;
        mainPathRenderer.SetPositions(points);

        SpawnEnemiesAndTrapsOnMainPath(path);
    }

    /// <summary>
    /// Spawn enemies and traps sparsely along the main path.
    /// </summary>
    private void SpawnEnemiesAndTrapsOnMainPath(List<Vector2Int> mainPath) {
        if ((enemyPrefabs == null || enemyPrefabs.Length == 0) &&
            (trapPrefabs == null || trapPrefabs.Length == 0))
            return;

        int pathLength = mainPath.Count;
        if (pathLength < 5)
            return;

        int enemyCount = Mathf.Clamp(Mathf.RoundToInt(pathLength * enemyDensityOnPath), 1, 10);
        int trapCount = Mathf.Clamp(Mathf.RoundToInt(pathLength * trapDensityOnPath), 1, 15);

        System.Random hazardRng = new(rng.Next() + 2000);
        Vector3 origin = mazeRoot.position - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f);

        List<Vector3> usedPositions = new();

        bool IsFarEnough(Vector3 pos) {
            foreach (var p in usedPositions)
                if (Vector3.Distance(p, pos) < minCellSpacingBetweenHazards)
                    return false;
            return true;
        }

        // Enemies
        if (enemyPrefabs != null && enemyPrefabs.Length > 0) {
            for (int i = 0; i < enemyCount; i++) {
                int idx = hazardRng.Next(1, pathLength - 1); // avoid very start/end
                Vector2Int c = mainPath[idx];
                Vector3 cellCenter = origin + new Vector3(c.x * cellSize, 0f, c.y * cellSize);

                if (!IsFarEnough(cellCenter)) {
                    i--;
                    continue;
                }

                GameObject prefab = enemyPrefabs[hazardRng.Next(enemyPrefabs.Length)];
                if (prefab == null) continue;

                GameObject instance = Instantiate(prefab, cellCenter, Quaternion.identity, mazeRoot);
                spawnedEnemies.Add(instance);
                usedPositions.Add(cellCenter);
            }
        }

        // Traps
        if (trapPrefabs != null && trapPrefabs.Length > 0) {
            for (int i = 0; i < trapCount; i++) {
                int idx = hazardRng.Next(1, pathLength - 1);
                Vector2Int c = mainPath[idx];
                Vector3 cellCenter = origin + new Vector3(c.x * cellSize, 0f, c.y * cellSize);

                if (!IsFarEnough(cellCenter)) {
                    i--;
                    continue;
                }

                GameObject prefab = trapPrefabs[hazardRng.Next(trapPrefabs.Length)];
                if (prefab == null) continue;

                GameObject instance = Instantiate(prefab, cellCenter, Quaternion.identity, mazeRoot);
                spawnedTraps.Add(instance);
                usedPositions.Add(cellCenter);
            }
        }
    }

    /// <summary>
    /// BFS pathfinding on the maze graph.
    /// </summary>
    private List<Vector2Int> BfsPath(Vector2Int start, Vector2Int goal) {
        Queue<Vector2Int> q = new();
        Dictionary<Vector2Int, Vector2Int> parent = new();
        HashSet<Vector2Int> visited = new();

        q.Enqueue(start);
        visited.Add(start);

        while (q.Count > 0) {
            var c = q.Dequeue();
            if (c == goal)
                break;

            foreach (var n in GetNeighbors(c)) {
                if (visited.Contains(n))
                    continue;
                if (HasWallBetween(c, n))
                    continue;

                visited.Add(n);
                parent[n] = c;
                q.Enqueue(n);
            }
        }

        if (!visited.Contains(goal))
            return null;

        List<Vector2Int> path = new();
        Vector2Int cur = goal;
        path.Add(cur);
        while (cur != start) {
            cur = parent[cur];
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }


    /// <summary>
    /// Destroy all spawned objects from previous maze.
    /// </summary>
    private void ClearMaze() {
        foreach (var w in spawnedWalls)
            if (w) Destroy(w);
        spawnedWalls.Clear();

        foreach (var p in spawnedProps)
            if (p) Destroy(p);
        spawnedProps.Clear();

        foreach (var e in spawnedEnemies)
            if (e) Destroy(e);
        spawnedEnemies.Clear();

        foreach (var t in spawnedTraps)
            if (t) Destroy(t);
        spawnedTraps.Clear();
    }
}
