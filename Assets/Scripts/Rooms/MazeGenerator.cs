using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MazeGenerator : MonoBehaviour {
    [Header("Grid Settings")]
    [SerializeField] private int width = 50;      // number of cells in X
    [SerializeField] private int height = 50;     // number of cells in Z
    [SerializeField] private float cellSize = 2f; // 100x100 plane -> 50x50 grid -> 2 units per cell

    [Header("Entrance / Exit (World Space)")]
    [SerializeField] private Transform pointA;    // entrance transform
    [SerializeField] private Transform pointB;    // exit transform

    [Header("Visuals")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private Transform mazeRoot;  // parent of all walls

    [Header("Controls")]
    [SerializeField] private KeyCode generateKey = KeyCode.F;

    private MazeCell[,] cells;
    private readonly List<GameObject> spawnedWalls = new List<GameObject>();
    private System.Random rng = new System.Random();

    private LineRenderer pathRenderer;
    private Vector2Int startCell;
    private Vector2Int endCell;

    [Serializable]
    private class MazeCell {
        public bool wallN = true;
        public bool wallS = true;
        public bool wallE = true;
        public bool wallW = true;
    }

    private void Awake() {
        pathRenderer = GetComponent<LineRenderer>();
        pathRenderer.positionCount = 0;
        pathRenderer.widthMultiplier = 0.15f;
        pathRenderer.material = new Material(Shader.Find("Sprites/Default"));
        pathRenderer.startColor = Color.green;
        pathRenderer.endColor = Color.red;
    }

    private void Start() {
        GenerateMaze();
    }

    private void Update() {
        if (Input.GetKeyDown(generateKey))
            GenerateMaze();
    }

    private void GenerateMaze() {
        ClearMaze();
        pathRenderer.positionCount = 0;

        cells = new MazeCell[width, height];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                cells[x, z] = new MazeCell();

        // Map world A/B to grid cells
        startCell = ClampCell(WorldToCell(pointA.position));
        endCell = ClampCell(WorldToCell(pointB.position));

        rng = new System.Random(rng.Next());

        // 1) Create an initial backbone path from A to B
        CreateBackbone(startCell, endCell);

        // 2) Fill the rest using Prim to get a perfect maze
        PrimsFill(startCell);

        // 3) Ensure entrance/exit on outer border for A/B
        OpenEntranceExit(startCell, endCell);

        // 4) Build 3D walls with mazeRoot as the center of the maze
        BuildWalls();

        // 5) Debug path from A to B with BFS + LineRenderer
        DrawPathFromAToB();
    }

    // Convert world position (plane space) to grid cell indices
    private Vector2Int WorldToCell(Vector3 worldPos) {
        // Assume mazeRoot is the center of the maze.
        // Offset world position to maze local space.
        Vector3 local = worldPos - mazeRoot.position;

        // Convert local x/z to cell indices
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

    private void CreateBackbone(Vector2Int start, Vector2Int end) {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<Vector2Int> path = new List<Vector2Int>();

        Vector2Int current = start;
        visited.Add(current);
        path.Add(current);

        int safety = width * height * 4;

        while (current != end && safety-- > 0) {
            List<Vector2Int> neighbors = GetNeighbors(current);
            List<Vector2Int> unvisited = new List<Vector2Int>();

            foreach (var n in neighbors)
                if (!visited.Contains(n))
                    unvisited.Add(n);

            if (unvisited.Count == 0)
                break;

            // Sort neighbors by distance to end (greedy towards exit)
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

    private void PrimsFill(Vector2Int start) {
        HashSet<Vector2Int> inMaze = new HashSet<Vector2Int>();
        HashSet<Vector2Int> frontier = new HashSet<Vector2Int>();

        // Flood fill existing open cells (backbone tree)
        Queue<Vector2Int> q = new Queue<Vector2Int>();
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

        // Initialize frontier neighbors
        foreach (var c in inMaze)
            foreach (var n in GetNeighbors(c))
                if (!inMaze.Contains(n))
                    frontier.Add(n);

        while (frontier.Count > 0) {
            int idx = rng.Next(frontier.Count);
            Vector2Int active = default;
            int i = 0;
            foreach (var f in frontier) {
                if (i == idx) { active = f; break; }
                i++;
            }
            frontier.Remove(active);

            List<Vector2Int> neighbors = GetNeighbors(active);
            List<Vector2Int> inMazeNeighbors = new List<Vector2Int>();
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
        List<Vector2Int> list = new List<Vector2Int>();

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

    private void OpenEntranceExit(Vector2Int start, Vector2Int end) {
        // Open start cell towards closest border
        if (start.y == 0)
            cells[start.x, start.y].wallS = false;
        else if (start.y == height - 1)
            cells[start.x, start.y].wallN = false;
        else if (start.x == 0)
            cells[start.x, start.y].wallW = false;
        else if (start.x == width - 1)
            cells[start.x, start.y].wallE = false;

        // Open end cell towards closest border
        if (end.y == 0)
            cells[end.x, end.y].wallS = false;
        else if (end.y == height - 1)
            cells[end.x, end.y].wallN = false;
        else if (end.x == 0)
            cells[end.x, end.y].wallW = false;
        else if (end.x == width - 1)
            cells[end.x, end.y].wallE = false;
    }

    private void BuildWalls() {
        if (!wallPrefab || !mazeRoot) {
            Debug.LogError("WallPrefab or MazeRoot is not assigned!");
            return;
        }

        float wallHeight = wallPrefab.transform.localScale.y;

        // Origin so that mazeRoot is in the middle of the maze
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

    private void ClearMaze() {
        foreach (var w in spawnedWalls)
            if (w) Destroy(w);
        spawnedWalls.Clear();
    }

    // ---------- DEBUG PATH FROM A TO B ----------

    private void DrawPathFromAToB() {
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

        pathRenderer.positionCount = points.Length;
        pathRenderer.SetPositions(points);
    }

    private List<Vector2Int> BfsPath(Vector2Int start, Vector2Int goal) {
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

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

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int cur = goal;
        path.Add(cur);
        while (cur != start) {
            cur = parent[cur];
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }
}
