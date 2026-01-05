using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Ghost-like enemy for the maze:
/// - Wanders between visible neighboring cells (graph-based).
/// - Chases the player on sight with line-of-sight checks.
/// - Falls back to patrol when player is lost or path is invalid.
/// - Kills the player on contact.
/// - Periodically teleports along the main path to keep pressure high.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class MazeEnemy : MonoBehaviour {
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

    [Header("Teleporting")]
    [SerializeField] private bool enableTeleport = true;
    [SerializeField] private float minTeleportDistanceFromPlayer = 10f;
    [SerializeField] private float maxTeleportDistanceFromPlayer = 35f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private NavMeshAgent agent;
    private Transform player;
    private MazeGenerator maze;

    private bool isChasing;
    private float lastTimeSawPlayer;
    private float nextWanderTime;
    private float nextTeleportTime;

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start() {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        maze = FindObjectOfType<MazeGenerator>();

        // Configure NavMeshAgent for responsive movement
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.1f;
        agent.autoBraking = false;
        agent.autoRepath = true;
        agent.acceleration = 12f;
        agent.angularSpeed = 360f;
        agent.avoidancePriority = 50;

        agent.radius = 0.5f;
        agent.height = 1.0f;

        nextWanderTime = Time.time + Random.Range(0f, wanderInterval);

        if (enableTeleport) {
            ScheduleNextTeleport();
        }
    }

    private void Update() {
        if (player == null) return;

        UpdateChaseStateFromVision();

        if (isChasing) {
            HandleChase();
        } else {
            HandleWander();
        }

        if (enableTeleport && Time.time >= nextTeleportTime) {
            TryTeleportAlongMainPath();
            ScheduleNextTeleport();
        }
    }

    private void LateUpdate() {
        if (player == null) return;

        if (isChasing) {
            if (agent.pathPending || agent.remainingDistance <= 1.0f) {
                agent.SetDestination(player.position);
            }
        }

        EnsureAgentOnNavMesh();
        CheckProximityToPlayer();
    }

    #region Vision / Chase

    /// <summary>
    /// Switches to chase when player is inside FOV and line of sight.
    /// </summary>
    private void UpdateChaseStateFromVision() {
        if (CanSeePlayer()) {
            lastTimeSawPlayer = Time.time;

            if (!isChasing) {
                isChasing = true;
                agent.speed = chaseSpeed;
                agent.SetDestination(player.position);
            }
        }
    }

    private void HandleChase() {
        // Lost sight for too long: back to patrol
        if (Time.time - lastTimeSawPlayer > loseSightTime) {
            ResetToPatrol();
            return;
        }

        // Invalid or partial path: back to patrol
        if (!agent.pathPending &&
            (agent.pathStatus == NavMeshPathStatus.PathPartial ||
             agent.pathStatus == NavMeshPathStatus.PathInvalid)) {
            ResetToPatrol();
            return;
        }

        if (agent.pathPending) {
            return;
        }

        agent.SetDestination(player.position);

        // If seemingly stuck on a complete path, try recalculating
        if (agent.velocity.magnitude < 0.1f && agent.pathStatus == NavMeshPathStatus.PathComplete) {
            agent.ResetPath();
            agent.SetDestination(player.position);
        }
    }

    private bool CanSeePlayer() {
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > viewRadius)
            return false;

        Vector3 directionToPlayer = toPlayer.normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > viewAngle * 0.5f)
            return false;

        // Check occlusion with a raycast
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                            directionToPlayer,
                            out RaycastHit hit,
                            distance,
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
        agent.ResetPath();
        nextWanderTime = Time.time + Random.Range(0f, wanderInterval);
    }

    #endregion

    #region Wander

    private void HandleWander() {
        if (maze == null) return;

        bool reachedTarget = !agent.pathPending && agent.remainingDistance <= 0.2f;

        if (Time.time >= nextWanderTime && reachedTarget) {
            Vector3 target = GetRandomVisibleNeighborCellCenter();

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2.0f, NavMesh.AllAreas)) {
                agent.SetDestination(hit.position);
            } else {
                Vector3 adjustedTarget = FindValidNavMeshPosition(target, 5.0f);
                if (adjustedTarget != Vector3.zero) {
                    agent.SetDestination(adjustedTarget);
                }
            }

            nextWanderTime = Time.time + wanderInterval;
        }
    }

    /// <summary>
    /// Picks a random neighboring cell center that:
    /// - Is connected in the maze graph,
    /// - Has clear line of sight,
    /// - And is on the NavMesh.
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

    #region Teleport

    /// <summary>
    /// Schedules the next teleport based on AI enemy aggression factor.
    /// Higher aggression = more frequent teleports.
    /// </summary>
    private void ScheduleNextTeleport() {
        float aggression = GameManager.Instance?.AIModel?.EnemyAggressionFactor ?? 0.5f;

        // Map aggression (0-1) to a min/max interval range
        float minInterval = Mathf.Lerp(30f, 10f, aggression);
        float maxInterval = Mathf.Lerp(60f, 20f, aggression);

        nextTeleportTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    /// <summary>
    /// Teleports the enemy to a random point on the main path,
    /// at a reasonable distance from the player.
    /// </summary>
    private void TryTeleportAlongMainPath() {
        if (maze == null || maze.mainPathWorldPoints == null || maze.mainPathWorldPoints.Count == 0)
            return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        // Skip teleport if already very close
        if (distToPlayer < minTeleportDistanceFromPlayer * 0.5f)
            return;

        Vector3 bestPoint = transform.position;
        bool found = false;

        // Try some random points along main path and pick the first that fits distance constraints
        for (int i = 0; i < 10; i++) {
            int idx = Random.Range(0, maze.mainPathWorldPoints.Count);
            Vector3 candidate = maze.mainPathWorldPoints[idx];

            float d = Vector3.Distance(candidate, player.position);
            if (d >= minTeleportDistanceFromPlayer && d <= maxTeleportDistanceFromPlayer) {
                bestPoint = candidate;
                found = true;
                break;
            }
        }

        if (!found) return;

        if (NavMesh.SamplePosition(bestPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas)) {
            // Warp instead of MovePosition to avoid sliding through walls
            agent.Warp(hit.position);
            ResetToPatrol(); // After teleport, return to patrol or start chasing depending on design
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// Finds a valid NavMesh position near the target position.
    /// </summary>
    private Vector3 FindValidNavMeshPosition(Vector3 target, float searchRadius) {
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, searchRadius, NavMesh.AllAreas)) {
            return hit.position;
        }

        // Expand in rings around the target
        for (float r = 0.5f; r <= searchRadius; r += 0.5f) {
            for (int i = 0; i < 8; i++) {
                float angle = (i * 45f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 testPos = target + dir * r;

                if (NavMesh.SamplePosition(testPos, out hit, 0.5f, NavMesh.AllAreas)) {
                    return hit.position;
                }
            }
        }

        return Vector3.zero;
    }

    #endregion

    #region Combat

    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null) {
            // One-hit kill for now (can be tuned later)
            health.TakeDamage(int.MaxValue, ignoreImmunity: true);
        }
    }

    private void OnCollisionEnter(Collision other) {
        if (!other.gameObject.CompareTag("Player")) return;

        PlayerHealth health = other.gameObject.GetComponent<PlayerHealth>();
        if (health != null) {
            health.TakeDamage(int.MaxValue, ignoreImmunity: true);
        }
    }

    /// <summary>
    /// Keeps the agent snapped to the NavMesh in case geometry or baking is imperfect.
    /// </summary>
    private void EnsureAgentOnNavMesh() {
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas)) {
            Vector3 validPosition = FindValidNavMeshPosition(transform.position, 3.0f);
            if (validPosition != Vector3.zero) {
                agent.Warp(validPosition);
            }
        }
    }

    /// <summary>
    /// Backup proximity check to ensure the player gets hit even if triggers fail.
    /// </summary>
    private void CheckProximityToPlayer() {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float detectionRadius = agent.radius + 0.5f;

        if (distanceToPlayer <= detectionRadius) {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null) {
                health.TakeDamage(int.MaxValue, ignoreImmunity: true);
            }
        }
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected() {
        if (!showGizmos) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 leftDir = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * viewRadius);
    }

    #endregion
}
