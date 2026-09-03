using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;

    private bool resolved;
    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector3 velocity)
    {
        body.linearVelocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        TargetController target = collision.collider.GetComponent<TargetController>();
        if (target != null)
        {
            target.Hit();
        }
        else if (TargetRushGame.Instance != null)
        {
            TargetRushGame.Instance.RegisterMiss();
        }

        Destroy(gameObject);
    }
}
