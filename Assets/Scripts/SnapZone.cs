using UnityEngine;

public class SnapZone : MonoBehaviour
{
    public string partId;
    public Transform snapPoint;
    public Renderer[] ghostRenderers;

    private EngineAssemblyManager manager;

    private void Awake()
    {
        if (snapPoint == null)
            snapPoint = transform;

        if (ghostRenderers == null || ghostRenderers.Length == 0)
            ghostRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public void Setup(EngineAssemblyManager assemblyManager)
    {
        manager = assemblyManager;
        SetGhostVisible(false);
    }

    public void SetGhostVisible(bool visible)
    {
        if (ghostRenderers == null) return;

        foreach (Renderer r in ghostRenderers)
        {
            if (r != null)
                r.enabled = visible;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (manager == null)
            return;

        AssemblyPart part = other.GetComponentInParent<AssemblyPart>();
        if (part == null)
            return;

        if (part.IsSnapped || part.IsHeld)
            return;

        if (!manager.CanSnapPartToZone(part, this))
            return;

        manager.TrySnapPart(part, this);
    }
}