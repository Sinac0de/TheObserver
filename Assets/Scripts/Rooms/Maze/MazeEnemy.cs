using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class MazeEnemy : MonoBehaviour {
    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waypointTolerance = 0.2f;
    [SerializeField] private float patrolWaitTime = 1f;

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
    private int currentPatrolIndex;
    private float patrolWaitTimer;

    private bool isChasing;
    private float lastTimeSawPlayer;

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start() {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0f;

        if (patrolPoints != null && patrolPoints.Length > 0) {
            currentPatrolIndex = 0;
            GoToNextPatrolPoint();
        }
    }

    private void Update() {
        if (player == null) return;

        if (CanSeePlayer()) {
            lastTimeSawPlayer = Time.time;
            if (!isChasing) {
                isChasing = true;
                agent.speed = chaseSpeed;
            }
        }

        if (isChasing) {
            if (Time.time - lastTimeSawPlayer > loseSightTime) {
                ResetToPatrol();
                return;
            }

            agent.SetDestination(player.position);
        } else {
            HandlePatrol();
        }
    }

    private void HandlePatrol() {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance) {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime) {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                GoToNextPatrolPoint();
                patrolWaitTimer = 0f;
            }
        }
    }

    private void GoToNextPatrolPoint() {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        agent.speed = patrolSpeed;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private bool CanSeePlayer() {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.magnitude > viewRadius)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, toPlayer.normalized,
                            out RaycastHit hit, viewRadius, ~0, QueryTriggerInteraction.Ignore)) {
            if (!hit.collider.CompareTag("Player"))
                return false;
        }

        return true;
    }

    public void ResetToPatrol() {
        isChasing = false;
        agent.speed = patrolSpeed;

        if (patrolPoints != null && patrolPoints.Length > 0) {
            currentPatrolIndex = 0;
            GoToNextPatrolPoint();
        }
    }

    public void SetPatrolPoints(Transform[] points) {
        patrolPoints = points;
        if (patrolPoints != null && patrolPoints.Length > 0 && agent != null) {
            currentPatrolIndex = 0;
            GoToNextPatrolPoint();
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null) {
            health.TakeDamage(int.MaxValue, ignoreImmunity: true);
        }
    }

    private void OnDrawGizmosSelected() {
        if (!showGizmos) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 leftDir = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * viewRadius);

        if (patrolPoints != null && patrolPoints.Length > 1) {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++) {
                if (patrolPoints[i] == null) continue;
                Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);

                Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (next != null) {
                    Gizmos.DrawLine(patrolPoints[i].position, next.position);
                }
            }
        }
    }
}
