public enum RoomState {
    Inactive,     // Not loaded
    Initializing, // AI adaptation applied  
    Active,       // Player inside
    Completing,   // Victory sequence
    Failing       // Death/timeout
}
