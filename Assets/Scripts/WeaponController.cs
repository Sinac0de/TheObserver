using UnityEngine;

public class WeaponController : MonoBehaviour {
    public Camera fpsCamera;
    public float range = 50f;
    public int ammo = 10;

    public ParticleSystem muzzleFlash;

    private void Start() {
        if (GameInputManager.Instance != null)
            GameInputManager.Instance.OnShoot += GameInputManager_OnShoot;
    }

    private void GameInputManager_OnShoot() {
        Shoot();
    }

    void Shoot() {
        if (ammo <= 0) {
            Debug.Log("No Ammo!");
            return;
        }

        ammo--;

        if (muzzleFlash != null) muzzleFlash.Play();

        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);

        // Debug Line
        Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, range)) {
            Debug.Log("Hit: " + hit.collider.name);
        }
    }

    private void OnDestroy() {
        if (GameInputManager.Instance != null)
            GameInputManager.Instance.OnShoot -= GameInputManager_OnShoot;
    }
}
