public interface IRoom {
    void Initialize(AIModel aiModel);
    void OnPlayerEnter();
    void OnPlayerExit();

    void RegisterMistake();
    void RegisterDetection();

    bool CanExit();
    float GetSolveTime();

    int Mistakes { get; }
    int Detections { get; }
}
