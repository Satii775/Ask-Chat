using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class AssemblyPart : MonoBehaviour
{
    public string partId;

    public bool IsHeld { get; private set; }
    public bool IsSnapped { get; private set; }

    private XRGrabInteractable grab;
    private Rigidbody rb;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Transform startParent;
    private bool startUseGravity;
    private bool startIsKinematic;
    private bool startDetectCollisions;
    private RigidbodyConstraints startConstraints;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        startParent = transform.parent;
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (rb != null)
        {
            startUseGravity = rb.useGravity;
            startIsKinematic = rb.isKinematic;
            startDetectCollisions = rb.detectCollisions;
            startConstraints = rb.constraints;
        }

        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        IsHeld = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        IsHeld = false;
    }

    public void SetInteractable(bool value)
    {
        if (grab != null)
            grab.enabled = value;
    }

    public void SnapTo(Transform target)
    {
        IsSnapped = true;
        IsHeld = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.detectCollisions = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        transform.SetPositionAndRotation(target.position, target.rotation);

        if (grab != null)
            grab.enabled = false;
    }

    public void ResetPart()
    {
        IsHeld = false;
        IsSnapped = false;

        if (grab != null)
            grab.enabled = true;

        transform.SetParent(startParent);
        transform.SetPositionAndRotation(startPosition, startRotation);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = startUseGravity;
            rb.isKinematic = startIsKinematic;
            rb.detectCollisions = startDetectCollisions;
            rb.constraints = startConstraints;
        }
    }
}