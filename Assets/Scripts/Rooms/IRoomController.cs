public interface IRoomController {
    public RoomState CurrentState { get; }
    void UpdateRoomState(RoomState newState);
    void OnRoomActive();
    void OnRoomInactive();
}
