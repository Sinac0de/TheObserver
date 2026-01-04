using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour {
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float trapImmuneDuration = 2f;

    private int currentHealth;
    private bool isImmune;

    public MazeRoomController currentMazeRoom;

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
            currentMazeRoom.Fail();
        } else {
            Debug.LogWarning("[PlayerHealth] Died but no MazeRoomController assigned.");
        }
    }

    public void ResetHealth() {
        currentHealth = maxHealth;
        isImmune = false;
        StopAllCoroutines();
    }
}
