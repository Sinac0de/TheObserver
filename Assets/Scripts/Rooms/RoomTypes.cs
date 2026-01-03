
public enum RoomType {
    Maze,
    Horror,
    Boss
}

[System.Serializable]
public struct RoomMetrics {
    public RoomType roomType;
    public float solveTimeSeconds;
    public int mistakes;      // traps, hits, etc.
    public int detections;    // for Maze (zero for now)
}