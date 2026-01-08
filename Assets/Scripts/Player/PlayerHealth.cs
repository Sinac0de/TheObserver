using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour {
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float trapImmuneDuration = 2f;

    private int currentHealth;
    private bool isImmune;

    public MazeRoomController currentMazeRoom;
    
    // Properties to expose health values
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    
    // Metrics for AI analysis
    private int enemyEncounters = 0;
    private float movementSpeedTracker = 0f;
    private float standingStillTime = 0f;
    private float lastPositionUpdate = 0f;
    private Vector3 lastPosition = Vector3.zero;
    
    public int EnemyEncounters => enemyEncounters;
    public float MovementSpeedTracker => movementSpeedTracker;
    public float StandingStillTime => standingStillTime;
    
    public void RegisterEnemyEncounter() {
        enemyEncounters++;
    }

    private void Start() {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount, bool ignoreImmunity = false) {
        if (isImmune && !ignoreImmunity)
            return;

        currentHealth -= amount;
        if (currentHealth <= 0) {
            currentHealth = 0;
            OnDeath();
        }
    }

    public void ApplyTrapDamageAndImmune() {
        if (!isImmune) {
            int trapDamage = Mathf.RoundToInt(maxHealth / 3f);
            TakeDamage(trapDamage, ignoreImmunity: false);
            if (currentHealth > 0) {
                StartCoroutine(ImmuneRoutine());
            }
        }
    }

    private IEnumerator ImmuneRoutine() {
        isImmune = true;
        yield return new WaitForSeconds(trapImmuneDuration);
        isImmune = false;
    }

    private void OnDeath() {
        if (currentMazeRoom != null) {
            currentMazeRoom.RegisterMistake();
            currentMazeRoom.RegisterPlayerDeath();
        } else {
            Debug.LogWarning("[PlayerHealth] Died but no MazeRoomController assigned.");
        }
    }

    public void ResetHealth() {
        currentHealth = maxHealth;
        isImmune = false;
        StopAllCoroutines();
    }
    
    /// <summary>
    /// Set health to a specific value
    /// </summary>
    public void SetHealth(int health) {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
    }
}
