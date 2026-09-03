using UnityEngine;

public class GazeReticle : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private LineRenderer ring;
    [SerializeField] private float idleRadius = 0.035f;
    [SerializeField] private float targetRadius = 0.055f;

    private void Update()
    {
        if (viewCamera == null || ring == null)
        {
            return;
        }

        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        RaycastHit hit;
        bool foundTarget = Physics.Raycast(ray, out hit, 20f) &&
                           hit.collider.GetComponent<TargetController>() != null;

        transform.position = ray.origin + ray.direction * (foundTarget ? hit.distance : 8f);
        transform.rotation = Quaternion.LookRotation(viewCamera.transform.forward);
        ring.startColor = foundTarget ? Color.cyan : Color.white;
        ring.endColor = ring.startColor;

        float radius = foundTarget ? targetRadius : idleRadius;
        ring.startWidth = radius * 0.35f;
        ring.endWidth = radius * 0.35f;
    }
}
