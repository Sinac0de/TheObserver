using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A humanoid enemy that actively pursues the player once aware.
/// Can patrol areas until detecting the player, then switches to chase mode.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class StalkerEnemy : MonoBehaviour
{
    [Header("Stalker Settings")]
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float hearingRange = 10f;
    [SerializeField] private float sightRange = 15f;
    [SerializeField] private float detectionAngle = 90f; // In degrees
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private int damageOnAttack = 25;
    
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private bool isPatrolling = true;
    
    [Header("AI Model Integration")]
    [SerializeField] private float aiModelSpeedMultiplier = 1f;
    [SerializeField] private float aiModelDetectionMultiplier = 1f;
    [SerializeField] private float aiModelAggressionMultiplier = 1f;

    private NavMeshAgent agent;
    private Transform player;
    private int currentPatrolIndex;
    private bool isChasing;
    private bool isAttacking;
    private float lastSightTime;
    private float lastSoundTime;
    private bool playerInSight;
    private bool playerInHearingRange;
    private Animator animator; // If you have animations

    public bool IsChasing => isChasing;
    public bool IsAttacking => isAttacking;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentPatrolIndex = 0;
        lastSightTime = -1000f; // Initialize to a long time ago
        lastSoundTime = -1000f;
        
        if (player == null)
        {
            Debug.LogWarning("StalkerEnemy: No player found with tag 'Player'", this);
        }
        
        if (patrolPoints.Length > 0 && isPatrolling)
        {
            SetDestinationToPatrolPoint();
        }
        
        // Update initial agent settings based on AI model
        UpdateAIBasedOnModel();
    }

    private void Update()
    {
        if (player == null) return;
        
        UpdateAIBasedOnModel();
        UpdateSightAndSoundDetection();
        HandleChaseAndAttack();
    }

    private void UpdateAIBasedOnModel()
    {
        if (GameManager.Instance?.AIModel != null)
        {
            float complexity = GameManager.Instance.AIModel.CurrentComplexity;
            
            // Adjust speed based on AI model complexity
            float adjustedSpeed = normalSpeed * (1f + (complexity * aiModelSpeedMultiplier * 0.5f));
            float adjustedChaseSpeed = chaseSpeed * (1f + (complexity * aiModelSpeedMultiplier));
            
            agent.speed = isChasing ? adjustedChaseSpeed : adjustedSpeed;
            agent.angularSpeed = 120f * (1f + (complexity * aiModelSpeedMultiplier * 0.3f)); // Turn faster with complexity
            
            // Adjust detection range based on AI model
            float adjustedSightRange = sightRange * (1f + (complexity * aiModelDetectionMultiplier * 0.3f));
            float adjustedHearingRange = hearingRange * (1f + (complexity * aiModelDetectionMultiplier * 0.2f));
            
            // Use the adjusted ranges in detection
        }
    }

    private void UpdateSightAndSoundDetection()
    {
        // Check if player is in sight
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        playerInHearingRange = distanceToPlayer <= hearingRange;
        
        if (distanceToPlayer <= sightRange)
        {
            // Check if player is within the detection angle
            Vector3 forward = transform.forward;
            float angleToPlayer = Vector3.Angle(forward, directionToPlayer);
            
            if (angleToPlayer < detectionAngle / 2f)
            {
                // Perform raycast to check for obstacles
                if (CanSeePlayer())
                {
                    playerInSight = true;
                    lastSightTime = Time.time;
                    OnPlayerSighted();
                }
                else
                {
                    playerInSight = false;
                }
            }
            else
            {
                playerInSight = false;
            }
        }
        else
        {
            playerInSight = false;
        }

        // Check if player made noise (running, trap triggering, etc.)
        if (playerInHearingRange && IsPlayerMakingNoise())
        {
            lastSoundTime = Time.time;
            OnPlayerHeard();
        }
    }

    private bool CanSeePlayer()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, (player.position - transform.position).normalized, 
                           out hit, sightRange, -1, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.CompareTag("Player");
        }
        return false;
    }

    private bool IsPlayerMakingNoise()
    {
        // This would integrate with player controller to detect running, trap triggers, etc.
        // For now, we'll simulate it based on player movement speed
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Player is making noise if they're moving at significant speed
            CharacterController playerCC = player.GetComponent<CharacterController>();
            if (playerCC != null)
            {
                // Check if player is moving significantly
                Vector3 playerVelocity = (player.position - (player.position - playerCC.velocity)) / Time.deltaTime;
                return playerVelocity.magnitude > 2f; // If moving faster than walking speed
            }
        }
        return false;
    }

    private void OnPlayerSighted()
    {
        if (!isChasing)
        {
            StartChase();
        }
    }

    private void OnPlayerHeard()
    {
        if (!isChasing)
        {
            // Move to the location where the sound was heard
            agent.SetDestination(player.position);
        }
    }

    private void StartChase()
    {
        isChasing = true;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);
    }

    private void StopChase()
    {
        isChasing = false;
        agent.speed = normalSpeed;
        
        if (isPatrolling && patrolPoints.Length > 0)
        {
            SetDestinationToPatrolPoint();
        }
    }

    private void HandleChaseAndAttack()
    {
        if (isChasing)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= attackRange)
            {
                // Attack the player
                AttackPlayer();
            }
            else if (distanceToPlayer > sightRange * 2f && 
                     Time.time - lastSightTime > 8f && 
                     Time.time - lastSoundTime > 8f)
            {
                // Player is too far and hasn't been seen/heard for a while
                StopChase();
            }
            else
            {
                // Continue chasing
                agent.SetDestination(player.position);
            }
        }
        else if (isPatrolling && agent.remainingDistance < 0.5f && !agent.pathPending)
        {
            // Reached patrol point, wait and then move to next
            Invoke(nameof(SetDestinationToPatrolPoint), patrolWaitTime);
        }
    }

    private void SetDestinationToPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void AttackPlayer()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            
            // Damage player
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageOnAttack);
                
                // Trigger flashlight flicker when attacked
                FlashlightController flashlight = player.GetComponentInChildren<FlashlightController>();
                if (flashlight != null)
                {
                    flashlight.TriggerFlicker();
                }
            }
            
            // Brief pause before next attack
            Invoke(nameof(ResetAttack), 2f);
        }
    }

    private void ResetAttack()
    {
        isAttacking = false;
    }

    /// <summary>
    /// Called by AI model when difficulty changes to update behavior parameters.
    /// </summary>
    public void UpdateDifficulty(float complexity)
    {
        aiModelSpeedMultiplier = 1f + (complexity * 0.5f); // Increase speed with complexity
        aiModelDetectionMultiplier = 1f + (complexity * 0.3f); // Increase detection with complexity
        aiModelAggressionMultiplier = 1f + (complexity * 0.4f); // Increase aggression with complexity
    }
    
    /// <summary>
    /// Sets new patrol points for the enemy.
    /// </summary>
    public void SetPatrolPoints(Transform[] newPatrolPoints)
    {
        patrolPoints = newPatrolPoints;
        currentPatrolIndex = 0;
        
        if (isPatrolling && patrolPoints.Length > 0)
        {
            SetDestinationToPatrolPoint();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection range gizmos
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw field of view
        if (transform != null)
        {
            Vector3 forward = transform.forward;
            Vector3 left = Quaternion.AngleAxis(-detectionAngle / 2f, transform.up) * forward;
            Vector3 right = Quaternion.AngleAxis(detectionAngle / 2f, transform.up) * forward;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, left * sightRange);
            Gizmos.DrawRay(transform.position, right * sightRange);
        }
        
        // Draw patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Gizmos.color = Color.cyan;
                if (i > 0) Gizmos.DrawLine(patrolPoints[i-1].position, patrolPoints[i].position);
                
                Gizmos.DrawSphere(patrolPoints[i].position, 0.5f);
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i].position + Vector3.up * 2f);
            }
            
            // Draw line from last to first if looping
            if (patrolPoints.Length > 1)
            {
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length-1].position, patrolPoints[0].position);
            }
        }
    }
}