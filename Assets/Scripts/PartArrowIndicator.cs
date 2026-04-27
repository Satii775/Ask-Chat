using UnityEngine;

public class PartArrowIndicator : MonoBehaviour
{
    [Header("Bob Animation")]
    [Tooltip("Vertical bob amplitude in meters (0.02 = 2cm).")]
    public float bobHeight = 0.02f;


    [Tooltip("Bob speed in cycles per second.")]
    public float bobSpeed = 2f;

    [Header("Position")]
    [Tooltip("How far above the target the arrow hovers (meters).")]
    public float heightAboveTarget = 0.15f;

    private Transform target;
    private float bobTimeOffset;

    private void Awake()
    {
        // Random phase so multiple arrows don't all bob in lockstep
        bobTimeOffset = Random.Range(0f, Mathf.PI * 2);
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f + bobTimeOffset) * bobHeight;
        transform.position = target.position + Vector3.up * (heightAboveTarget + bob);
    }
}
