using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class MazeGenerator : MonoBehaviour {
    [Header("Grid Settings")]
    [SerializeField] private int width = 50;
    [SerializeField] private int height = 50;
    [SerializeField] private float cellSize = 2f;

    [Header("Entrance / Exit (World Space)")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Visuals")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private Transform mazeRoot;


    [Header("Wall Size")]
    [SerializeField] private float wallHeight = 4f;
    [SerializeField] private float wallThickness = 0.4f;

    [Header("Controls")]
    [SerializeField] private KeyCode generateKey = KeyCode.F;

    [Header("Props")]
    [SerializeField] private GameObject[] props;
    [SerializeField, Range(0f, 1f)] private float propSpawnChance = 0.05f;
    [SerializeField] private int propSpawnSeedOffset = 1000;

    [Header("Enemies & Traps")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject[] trapPrefabs;
    [SerializeField, Range(0f, 1f)] public float enemyDensityOnPath = 0.04f;
    [SerializeField, Range(0f, 1f)] public float trapDensityOnPath = 0.05f;
    [SerializeField] private float minCellSpacingBetweenHazards = 3f;

    [Header("Navigation / Vision")]
    [SerializeField] private NavMeshSurface navSurface;
    [SerializeField] private LayerMask wallMask = ~0; // Walls/obstacles for line-of-sight checks


    [Header("AI Debug / Patrol")]
    public List<Vector3> mainPathWorldPoints = new();

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

    private Vector2Int startCell;
    private Vector2Int endCell;

    private LineRenderer mainPathRenderer;

    [Header("Debug Path")]
    [SerializeField] private bool mainPathVisible = true;
    [SerializeField] private KeyCode togglePathKey = KeyCode.F1;

    private void Awake() {
        mainPathRenderer = GetComponent<LineRenderer>();
        mainPathRenderer.positionCount = 0;
        mainPathRenderer.widthMultiplier = 0.15f;
        mainPathRenderer.material = new Material(Shader.Find("Sprites/Default"));
        mainPathRenderer.startColor = Color.green;
        mainPathRenderer.endColor = Color.red;

        UpdateMainPathVisibility();
    }

    private void Start() {
        GenerateMaze();
    }

    private void Update() {
        if (Input.GetKeyDown(generateKey))
            GenerateMaze();

        if (Input.GetKeyDown(togglePathKey)) {
            mainPathVisible = !mainPathVisible;
            UpdateMainPathVisibility();
        }
    }

    private void UpdateMainPathVisibility() {
        if (mainPathRenderer != null) {
            mainPathRenderer.enabled = mainPathVisible;
        }
    }

    public void GenerateMaze() {
        ClearMaze();
        mainPathRenderer.positionCount = 0;
        mainPathWorldPoints.Clear();

        cells = new MazeCell[width, height];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                cells[x, z] = new MazeCell();

        startCell = ClampCell(WorldToCell(pointA.position));
        endCell = ClampCell(WorldToCell(pointB.position));

        rng = new System.Random(rng.Next());

        CreateBackbone(startCell, endCell);
        PrimsFill(startCell);
        OpenEntranceExit(startCell, endCell);
        BuildWalls();
        SpawnProps();
        DrawMainPathAndHazards();

        if (navSurface != null) {
            navSurface.BuildNavMesh();
        } else {
            Debug.LogWarning("MazeGenerator: NavSurface is not assigned, enemies will not have a NavMesh.");
        }
    }

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
        HashSet<Vector2Int> inMaze = new();
        HashSet<Vector2Int> frontier = new();

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

    private void OpenEntranceExit(Vector2Int start, Vector2Int end) {
        if (start.y == 0) cells[start.x, start.y].wallS = false;
        else if (start.y == height - 1) cells[start.x, start.y].wallN = false;
        else if (start.x == 0) cells[start.x, start.y].wallW = false;
        else if (start.x == width - 1) cells[start.x, start.y].wallE = false;

        if (end.y == 0) cells[end.x, end.y].wallS = false;
        else if (end.y == height - 1) cells[end.x, end.y].wallN = false;
        else if (end.x == 0) cells[end.x, end.y].wallW = false;
        else if (end.x == width - 1) cells[end.x, end.y].wallE = false;
    }

    private void BuildWalls() {
        if (!wallPrefab || !mazeRoot) {
            Debug.LogError("WallPrefab or MazeRoot is not assigned!");
            return;
        }

        Vector3 origin = mazeRoot.position - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f);

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < height; z++) {
                Vector3 cellPos = origin + new Vector3(x * cellSize, 0f, z * cellSize);
                MazeCell cell = cells[x, z];

                if (cell.wallN)
                    SpawnWall(
                        cellPos + new Vector3(0f, wallHeight * 0.5f, cellSize * 0.5f),
                        new Vector3(cellSize, wallHeight, wallThickness)
                    );

                if (cell.wallS)
                    SpawnWall(
                        cellPos + new Vector3(0f, wallHeight * 0.5f, -cellSize * 0.5f),
                        new Vector3(cellSize, wallHeight, wallThickness)
                    );

                if (cell.wallE)
                    SpawnWall(
                        cellPos + new Vector3(cellSize * 0.5f, wallHeight * 0.5f, 0f),
                        new Vector3(wallThickness, wallHeight, cellSize)
                    );

                if (cell.wallW)
                    SpawnWall(
                        cellPos + new Vector3(-cellSize * 0.5f, wallHeight * 0.5f, 0f),
                        new Vector3(wallThickness, wallHeight, cellSize)
                    );
            }
        }
    }


    private void SpawnWall(Vector3 pos, Vector3 scale) {
        GameObject w = Instantiate(wallPrefab, pos, Quaternion.identity, mazeRoot);
        w.transform.localScale = scale;
        spawnedWalls.Add(w);
    }

    private void SpawnProps() {
        if (props == null || props.Length == 0)
            return;

        System.Random propRng = new(rng.Next() + propSpawnSeedOffset);
        Vector3 origin = mazeRoot.position - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f);

        for (int x = 0; x < width; x++) {
            for (int z = 0; z < height; z++) {
                if (x == 0 || z == 0 || x == width - 1 || z == height - 1)
                    continue;

                if (propRng.NextDouble() > propSpawnChance)
                    continue;

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

    private void DrawMainPathAndHazards() {
        List<Vector2Int> path = BfsPath(startCell, endCell);
        if (path == null || path.Count == 0) {
            Debug.LogWarning("No path found between A and B.");
            return;
        }

        Vector3 origin = mazeRoot.position - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f);
        Vector3[] points = new Vector3[path.Count];
        mainPathWorldPoints.Clear();

        for (int i = 0; i < path.Count; i++) {
            Vector2Int c = path[i];
            Vector3 cellCenter = origin + new Vector3(c.x * cellSize, 0.1f, c.y * cellSize);
            points[i] = cellCenter;
            mainPathWorldPoints.Add(cellCenter);
        }

        mainPathRenderer.positionCount = points.Length;
        mainPathRenderer.SetPositions(points);
        UpdateMainPathVisibility();

        SpawnEnemiesAndTrapsOnMainPath(path);
    }



    /// <summary>
    /// Snap a world position to the center of the nearest maze cell.
    /// </summary>
    public Vector3 SnapToCellCenter(Vector3 worldPos) {
        Vector3 local = worldPos - mazeRoot.position;

        float originOffsetX = -(width * cellSize) * 0.5f;
        float originOffsetZ = -(height * cellSize) * 0.5f;

        float fx = (local.x - originOffsetX) / cellSize;
        float fz = (local.z - originOffsetZ) / cellSize;

        int cx = Mathf.RoundToInt(fx);
        int cz = Mathf.RoundToInt(fz);

        cx = Mathf.Clamp(cx, 0, width - 1);
        cz = Mathf.Clamp(cz, 0, height - 1);

        Vector3 center = mazeRoot.position
                         - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f)
                         + new Vector3(cx * cellSize, 0f, cz * cellSize);

        return new Vector3(center.x, worldPos.y, center.z);
    }

    /// <summary>
    /// Returns centers of neighbor cells that are connected (no wall between)
    /// AND have clear line of sight (no collider between them).
    /// </summary>
    public List<Vector3> GetVisibleNeighborCellCenters(Vector3 worldPos) {
        List<Vector3> result = new();

        Vector2Int cell = ClampCell(WorldToCell(worldPos));
        var neighbors = GetNeighbors(cell);

        Vector3 origin = mazeRoot.position - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f);
        Vector3 currentCenter = origin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);

        foreach (var n in neighbors) {
            // Skip if there is a logical wall between cells in the maze graph
            if (HasWallBetween(cell, n))
                continue;

            Vector3 neighborCenter = origin + new Vector3(n.x * cellSize, 0f, n.y * cellSize);

            // Line-of-sight check: there must be no wall collider between centers
            Vector3 dir = (neighborCenter - currentCenter).normalized;
            float dist = Vector3.Distance(currentCenter, neighborCenter);

            if (Physics.Raycast(currentCenter + Vector3.up * 0.5f,
                                dir,
                                dist - 0.1f,
                                wallMask,
                                QueryTriggerInteraction.Ignore)) {
                // Something in wallMask is between the two cells (e.g. a wall)
                continue;
            }

            result.Add(neighborCenter);
        }

        return result;
    }



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

        if (enemyPrefabs != null && enemyPrefabs.Length > 0) {
            for (int i = 0; i < enemyCount; i++) {
                int idx = hazardRng.Next(1, pathLength - 1);
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
