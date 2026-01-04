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

        // Configure NavMeshAgent for better performance
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.1f; // allow contact with player
        agent.autoBraking = false; // Disable auto-braking for smoother movement
        agent.autoRepath = true;
        agent.acceleration = 12f; // Higher acceleration for more responsive movement
        agent.angularSpeed = 360f; // Faster turning
        agent.avoidancePriority = 50; // Medium avoidance priority
        
        // Ensure proper collision detection
        agent.radius = 0.5f; // Make sure the agent has a reasonable collision radius
        agent.height = 1.0f;  // Make sure the agent has a reasonable collision height

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

    private void LateUpdate() {
        if (player == null) return;
        
        // Additional check to ensure enemy is always aware of player position
        if (isChasing) {
            // Update destination more frequently for smoother chasing
            if (agent.pathPending || agent.remainingDistance <= 1.0f) {
                agent.SetDestination(player.position);
            }
        }
        
        // Ensure the agent stays on the NavMesh and doesn't pass through walls
        EnsureAgentOnNavMesh();
        
        // Additional collision check in case trigger/collision detection fails
        CheckProximityToPlayer();
    }

    #region Vision / Chase

    private void UpdateChaseStateFromVision() {
        if (CanSeePlayer()) {
            lastTimeSawPlayer = Time.time;
            
            if (!isChasing) {
                isChasing = true;
                agent.speed = chaseSpeed;
                // When starting chase, set a more aggressive destination
                agent.SetDestination(player.position);
            }
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

        // Use a more robust pathfinding approach
        if (agent.pathPending) {
            // Path is still being calculated, wait
            return;
        }
        
        // Update destination more frequently for smoother chasing
        agent.SetDestination(player.position);
        
        // If the agent is stuck, try to recalculate
        if (agent.velocity.magnitude < 0.1f && agent.pathStatus == NavMeshPathStatus.PathComplete) {
            // Agent seems stuck, try to recalculate path
            agent.ResetPath();
            agent.SetDestination(player.position);
        }
    }

    private bool CanSeePlayer() {
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        
        if (distance > viewRadius)
            return false;

        // Check if player is within the field of view cone
        Vector3 directionToPlayer = toPlayer.normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > viewAngle * 0.5f)
            return false;

        // Line of sight check against obstacles
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
        agent.ResetPath(); // Clear any existing path when returning to patrol
        nextWanderTime = Time.time + Random.Range(0f, wanderInterval);
    }

    #endregion

    #region Wander

    private void HandleWander() {
        if (maze == null) return;

        bool reachedTarget = !agent.pathPending && agent.remainingDistance <= 0.2f;

        if (Time.time >= nextWanderTime && reachedTarget) {
            Vector3 target = GetRandomVisibleNeighborCellCenter();
            
            // Ensure the target is on the NavMesh before setting destination
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2.0f, NavMesh.AllAreas)) {
                agent.SetDestination(hit.position);
            } else {
                // If no valid NavMesh position, try to move to a nearby valid position
                Vector3 adjustedTarget = FindValidNavMeshPosition(target, 5.0f);
                if (adjustedTarget != Vector3.zero) {
                    agent.SetDestination(adjustedTarget);
                }
            }
            
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

    #region Utility Methods

    /// <summary>
    /// Finds a valid NavMesh position near the target position
    /// </summary>
    private Vector3 FindValidNavMeshPosition(Vector3 target, float searchRadius) {
        // Try to find a valid NavMesh position around the target
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, searchRadius, NavMesh.AllAreas)) {
            return hit.position;
        }
        
        // If not found, try in expanding circles around the target
        for (float r = 0.5f; r <= searchRadius; r += 0.5f) {
            for (int i = 0; i < 8; i++) { // 8 directions around the target
                float angle = (i * 45f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 testPos = target + dir * r;
                
                if (NavMesh.SamplePosition(testPos, out hit, 0.5f, NavMesh.AllAreas)) {
                    return hit.position;
                }
            }
        }
        
        return Vector3.zero; // No valid position found
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

    private void OnCollisionEnter(Collision other) {
        if (!other.gameObject.CompareTag("Player")) return;

        PlayerHealth health = other.gameObject.GetComponent<PlayerHealth>();
        if (health != null) {
            // One-hit kill, ignoring trap immunity
            health.TakeDamage(int.MaxValue, ignoreImmunity: true);
        }
    }

    /// <summary>
    /// Ensures the agent stays on the NavMesh and doesn't pass through walls
    /// </summary>
    private void EnsureAgentOnNavMesh() {
        // Check if the agent is on a valid NavMesh position
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas)) {
            // Agent is off-navmesh, try to snap it back
            Vector3 validPosition = FindValidNavMeshPosition(transform.position, 3.0f);
            if (validPosition != Vector3.zero) {
                // Teleport the agent back to a valid position
                agent.Warp(validPosition);
            }
        }
    }

    /// <summary>
    /// Checks if the player is close enough to trigger damage, as a backup to collision detection
    /// </summary>
    private void CheckProximityToPlayer() {
        if (player != null) {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            // Use a slightly larger radius than the agent's radius to ensure detection
            float detectionRadius = agent.radius + 0.5f;
            
            if (distanceToPlayer <= detectionRadius) {
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null) {
                    health.TakeDamage(int.MaxValue, ignoreImmunity: true);
                }
            }
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
