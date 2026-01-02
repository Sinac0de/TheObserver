using UnityEngine;
using UnityEngine.AI;

public class HorrorRoomController : BaseRoomController {
    [Header("Horror Elements")]
    [SerializeField] private Transform[] dollSpawnPoints;
    [SerializeField] private GameObject dollPrefab;
    [SerializeField] private Transform[] girlPatrolPoints;
    [SerializeField] private NavMeshAgent girlAgent;
    [SerializeField] private float baseGirlSpeed = 2f;
    [SerializeField] private float maxGirlSpeed = 6f;
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float jumpscareDelay = 1.0f;
    [SerializeField] private GameObject exitDoor;
    [SerializeField] private Animator exitDoorAnimator;

    private GameObject spawnedDoll;
    private bool dollCollected;
    private int currentPatrolIndex;

    protected override void ApplyAIDifficulty() {
        if (aiModel == null) {
            Debug.LogWarning("HorrorRoomController: AIModel not assigned in Initialize.");
            return;
        }

        float c = aiModel.CurrentComplexity;

        float speed = Mathf.Lerp(baseGirlSpeed, maxGirlSpeed, c);
        if (girlAgent != null) {
            girlAgent.speed = speed;
        }

        detectionRadius = Mathf.Lerp(4f, 10f, c);

        if (dollSpawnPoints != null && dollSpawnPoints.Length > 0 && dollPrefab != null) {
            int index = Random.Range(0, dollSpawnPoints.Length);
            spawnedDoll = Instantiate(dollPrefab, dollSpawnPoints[index].position, dollSpawnPoints[index].rotation);
        }

        if (girlAgent != null && girlPatrolPoints != null && girlPatrolPoints.Length > 0) {
            girlAgent.enabled = true;
            currentPatrolIndex = 0;
            girlAgent.SetDestination(girlPatrolPoints[currentPatrolIndex].position);
        }
    }

    private void Update() {
        if (currentState != RoomState.Active) return;

        UpdateGirlPatrol();
        CheckGirlDetection();
    }

    private void UpdateGirlPatrol() {
        if (girlAgent == null || girlPatrolPoints == null || girlPatrolPoints.Length == 0) return;

        if (!girlAgent.pathPending && girlAgent.remainingDistance <= girlAgent.stoppingDistance) {
            currentPatrolIndex = (currentPatrolIndex + 1) % girlPatrolPoints.Length;
            girlAgent.SetDestination(girlPatrolPoints[currentPatrolIndex].position);
        }
    }

    private void CheckGirlDetection() {
        if (girlAgent == null) return;

        Collider[] hits = Physics.OverlapSphere(girlAgent.transform.position, detectionRadius, playerLayer);
        if (hits.Length > 0) {
            currentState = RoomState.Failing;
            RegisterDetection();
            Invoke(nameof(TriggerJumpscareFail), jumpscareDelay);
        }
    }

    private void TriggerJumpscareFail() {
        RoomManager.Instance.CompleteCurrentRoom(false);
    }

    public void OnDollCollected() {
        dollCollected = true;
        if (exitDoorAnimator != null) {
            exitDoorAnimator.SetTrigger("Open");
        }
    }

    public override bool CanExit() {
        return dollCollected;
    }
}