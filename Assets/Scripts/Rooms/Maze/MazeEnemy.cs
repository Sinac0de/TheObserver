using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Ghost-like maze enemy:
/// - Wanders between neighboring maze cells (graph-based).
/// - Chases the player while in field of view and line of sight.
/// - Falls back to wander if path is partial/invalid or player is lost.
/// - Kills the player on contact.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class MazeEnemy: MonoBehaviour {
    [Header("Wander")]
    [SerializeField] private float wanderInterval = 1.5f;

    [Header("Chase")]
    [SerializeField] private float viewRadius = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float loseSightTime = 2f;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private NavMeshAgent agent;
    private Transform player;
    private MazeGenerator maze;

    private bool isChasing;
    private float lastTimeSawPlayer;
    private float nextWanderTime;

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start() {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        maze = FindObjectOfType<MazeGenerator>();

        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.1f; // allow contact with player
        agent.autoBraking = true;
        agent.autoRepath = true;

        nextWanderTime = Time.time + Random.Range(0f, wanderInterval);
    }

    private void Update() {
        if (player == null) return;

        UpdateChaseStateFromVision();

        if (isChasing) {
            HandleChase();
        } else {
            HandleWander();
        }
    }

    #region Vision / Chase

    private void UpdateChaseStateFromVision() {
        if (!CanSeePlayer()) return;

        lastTimeSawPlayer = Time.time;

        if (!isChasing) {
            isChasing = true;
            agent.speed = chaseSpeed;
        }
    }

    private void HandleChase() {
        // Lost sight long enough -> back to wander
        if (Time.time - lastTimeSawPlayer > loseSightTime) {
            ResetToPatrol();
            return;
        }

        // If NavMesh path is broken (fragmented maze edges), bail out gracefully
        if (!agent.pathPending &&
            (agent.pathStatus == NavMeshPathStatus.PathPartial ||
             agent.pathStatus == NavMeshPathStatus.PathInvalid)) {
            ResetToPatrol();
            return;
        }

        agent.SetDestination(player.position);
    }

    private bool CanSeePlayer() {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;
        if (distance > viewRadius)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > viewAngle * 0.5f)
            return false;

        // Line of sight check against obstacles
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                            toPlayer.normalized,
                            out RaycastHit hit,
                            viewRadius,
                            obstacleMask,
                            QueryTriggerInteraction.Ignore)) {
            if (!hit.collider.CompareTag("Player"))
                return false;
        }

        return true;
    }

    public void ResetToPatrol() {
        isChasing = false;
        agent.speed = patrolSpeed;
        nextWanderTime = Time.time + Random.Range(0f, wanderInterval);
    }

    #endregion

    #region Wander

    private void HandleWander() {
        if (maze == null) return;

        bool reachedTarget = !agent.pathPending && agent.remainingDistance <= 0.2f;

        if (Time.time >= nextWanderTime && reachedTarget) {
            Vector3 target = GetRandomVisibleNeighborCellCenter();
            agent.SetDestination(target);
            nextWanderTime = Time.time + wanderInterval;
        }
    }

    /// <summary>
    /// Picks a random neighbor cell center that:
    /// - Is connected in the maze graph (no logical wall).
    /// - Has clear physical line of sight between cell centers (no wall collider).
    /// - Is snapped to NavMesh to avoid ending up in geometry gaps.
    /// </summary>
    private Vector3 GetRandomVisibleNeighborCellCenter() {
        if (maze == null) return transform.position;

        var neighbors = maze.GetVisibleNeighborCellCenters(transform.position);
        if (neighbors.Count == 0)
            return transform.position;

        int idx = Random.Range(0, neighbors.Count);
        Vector3 target = neighbors[idx];

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 0.5f, NavMesh.AllAreas)) {
            return hit.position;
        }

        return target;
    }

    #endregion

    #region Combat

    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null) {
            // One-hit kill, ignoring trap immunity
            health.TakeDamage(int.MaxValue, ignoreImmunity: true);
        }
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected() {
        if (!showGizmos) return;

        // Vision radius
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // Vision cone
        Vector3 leftDir = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * viewRadius);
    }

    #endregion
}
