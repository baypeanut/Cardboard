using System.Collections;
using UnityEngine;

public class TargetController : MonoBehaviour
{
    [SerializeField] private int pointValue = 100;
    [SerializeField] private float respawnDelay = 0.65f;
    [SerializeField] private Renderer[] targetRenderers;

    private Collider targetCollider;
    private Vector3 startingPosition;
    private Coroutine respawnRoutine;

    public int PointValue => pointValue;

    private void Awake()
    {
        startingPosition = transform.position;
        targetCollider = GetComponent<Collider>();

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    public void Hit()
    {
        if (!targetCollider.enabled)
        {
            return;
        }

        TargetRushGame.Instance.RegisterHit(this);
    }

    public void ResetTarget(bool initial)
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
        }

        if (initial)
        {
            transform.position = startingPosition;
            SetVisible(true);
        }
        else
        {
            respawnRoutine = StartCoroutine(Respawn());
        }
    }

    private IEnumerator Respawn()
    {
        SetVisible(false);
        yield return new WaitForSeconds(respawnDelay);

        transform.position = new Vector3(
            Random.Range(-2.8f, 2.8f),
            Random.Range(1.1f, 3.7f),
            Random.Range(10.5f, 14.5f));
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        targetCollider.enabled = visible;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
            {
                targetRenderers[i].enabled = visible;
            }
        }
    }
}
