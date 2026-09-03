using Google.XR.Cardboard;
using UnityEngine;

public class CardboardShooter : MonoBehaviour
{
    [SerializeField] private BallProjectile projectilePrefab;
    [SerializeField] private float launchDistance = 0.45f;
    [SerializeField] private float launchForce = 19f;
    [SerializeField] private float cooldown = 0.18f;
    [SerializeField] private float gazeFireDelay = 0.75f;

    private float nextAllowedShot;
    private TargetController gazeTarget;
    private float gazeTime;

    private void Update()
    {
        bool cardboardTrigger = Application.isMobilePlatform && Api.IsTriggerPressed;
        UpdateGazeFire();
        if ((cardboardTrigger || InputSystemBridge.FirePressed()) && Time.time >= nextAllowedShot)
        {
            Fire();
        }
    }

    private void UpdateGazeFire()
    {
        if (!Application.isMobilePlatform || TargetRushGame.Instance == null ||
            !TargetRushGame.Instance.RoundActive)
        {
            gazeTarget = null;
            gazeTime = 0f;
            return;
        }

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        TargetController target = Physics.Raycast(ray, out hit, 20f)
            ? hit.collider.GetComponent<TargetController>()
            : null;

        if (target == null)
        {
            gazeTarget = null;
            gazeTime = 0f;
            return;
        }

        if (target != gazeTarget)
        {
            gazeTarget = target;
            gazeTime = 0f;
        }

        gazeTime += Time.deltaTime;
        if (gazeTime >= gazeFireDelay && Time.time >= nextAllowedShot)
        {
            gazeTime = 0f;
            Fire();
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null || TargetRushGame.Instance == null || !TargetRushGame.Instance.RoundActive)
        {
            return;
        }

        nextAllowedShot = Time.time + cooldown;
        BallProjectile projectile = Instantiate(
            projectilePrefab,
            transform.position + transform.forward * launchDistance,
            Quaternion.identity);
        projectile.Launch(transform.forward * Random.Range(launchForce * 0.9f, launchForce * 1.1f));
    }
}
