using System;
using System.Collections.Generic;
using UnityEngine;


public sealed class EngineAssemblySequence : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform engineRoot;              // Root containing the final assembled engine parts (children)
    [SerializeField] private Transform tableSpawnPoint;         // Where spawned grabbable parts appear (can be overridden per stage)

    [Header("Visuals")]
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float guideAlpha = 0.5f;
    [SerializeField, Range(0f, 1f)] private float completedAlpha = 1f;

    [Tooltip("Property name used by URP Lit for base color.")]
    [SerializeField] private string baseColorProperty = "_BaseColor";

    [Header("Stages")]
    [SerializeField] private List<AssemblyStage> stages = new();

    [Header("Runtime (read-only)")]
    [SerializeField] private int currentStageIndex = -1;

    private readonly Dictionary<Renderer, MaterialPropertyBlock> _mpbCache = new();
    private readonly List<Renderer> _allEngineRenderers = new();

    private GameObject _spawnedPartInstance;
    private GameObject _triggerInstance;
    private PlacementTrigger _trigger;

    [Serializable]
    public sealed class AssemblyStage
    {
        [Tooltip("The final part already in the engine hierarchy to reveal (set alpha 0.5 when active, alpha 1 when completed).")]
        public Transform enginePartInFinalAssembly;

        [Tooltip("Prefab the player grabs and moves (should have XRGrabInteractable + colliders; Rigidbody recommended).")]
        public GameObject grabbablePartPrefab;

        [Tooltip("Optional: override spawn location for this stage. If null, uses the global Table Spawn Point.")]
        public Transform spawnPointOverride;

        [Tooltip("Where you manually place the detection sphere for this stage (position/rotation). REQUIRED.")]
        public Transform detectionTransform;

        [Tooltip("Detection sphere radius.")]
        [Min(0.01f)] public float detectionRadius = 0.15f;

        [Tooltip("Optional: where the placed part should snap to. If null, snaps to detectionTransform pose.")]
        public Transform finalSnapPose;

        [Tooltip("If true, the enginePartInFinalAssembly will be guided/filled using all child renderers under that transform.")]
        public bool includeChildrenRenderers = true;
    }

    private void Awake()
    {
        if (engineRoot == null)
        {
            Debug.LogError($"{nameof(EngineAssemblySequence)}: Engine Root is not assigned.", this);
            enabled = false;
            return;
        }

        CacheAllEngineRenderers();
    }

    private void Start()
    {
        // Hide everything initially.
        SetAlpha(_allEngineRenderers, hiddenAlpha);

        // Start sequence.
        BeginStage(0);
    }

    private void OnDisable()
    {
        CleanupRuntimeObjects();
    }

    private void CacheAllEngineRenderers()
    {
        _allEngineRenderers.Clear();
        engineRoot.GetComponentsInChildren(includeInactive: true, result: _allEngineRenderers);
    }

    private void BeginStage(int index)
    {
        if (stages == null || stages.Count == 0)
        {
            Debug.LogError($"{nameof(EngineAssemblySequence)}: No stages configured.", this);
            enabled = false;
            return;
        }

        if (index < 0 || index >= stages.Count)
        {
            CompleteSequence();
            return;
        }

        CleanupRuntimeObjects();

        currentStageIndex = index;
        var stage = stages[currentStageIndex];

        if (stage.enginePartInFinalAssembly == null)
        {
            Debug.LogError($"{nameof(EngineAssemblySequence)} Stage {currentStageIndex}: enginePartInFinalAssembly is null.", this);
            enabled = false;
            return;
        }

        if (stage.grabbablePartPrefab == null)
        {
            Debug.LogError($"{nameof(EngineAssemblySequence)} Stage {currentStageIndex}: grabbablePartPrefab is null.", this);
            enabled = false;
            return;
        }

        if (stage.detectionTransform == null)
        {
            Debug.LogError($"{nameof(EngineAssemblySequence)} Stage {currentStageIndex}: detectionTransform is null (you must manually place it).", this);
            enabled = false;
            return;
        }

        // Hide everything, then restore completed parts (1.0) for all earlier stages, and set current target to guideAlpha.
        SetAlpha(_allEngineRenderers, hiddenAlpha);

        for (int i = 0; i < currentStageIndex; i++)
            SetStagePartAlpha(stages[i], completedAlpha);

        SetStagePartAlpha(stage, guideAlpha);

        SpawnGrabbable(stage);
        SpawnDetectionSphere(stage);
    }

    private void SpawnGrabbable(AssemblyStage stage)
    {
        Transform spawn = stage.spawnPointOverride != null ? stage.spawnPointOverride : tableSpawnPoint;
        if (spawn == null)
        {
            Debug.LogError($"{nameof(EngineAssemblySequence)}: No spawn point assigned (global or per-stage).", this);
            enabled = false;
            return;
        }

        _spawnedPartInstance = Instantiate(stage.grabbablePartPrefab, spawn.position, spawn.rotation);
        _spawnedPartInstance.name = $"{stage.grabbablePartPrefab.name}_Stage{currentStageIndex}";

        // Ensure Rigidbody exists for stable XR interaction / trigger callbacks.
        var rb = _spawnedPartInstance.GetComponent<Rigidbody>();
        if (rb == null) rb = _spawnedPartInstance.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // XR Grab is required for VR “dragging”. If missing, add it (minimal defaults).
        var grab = _spawnedPartInstance.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null) grab = _spawnedPartInstance.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Basic sanity: must have at least one collider somewhere.
        if (_spawnedPartInstance.GetComponentInChildren<Collider>() == null)
        {
            Debug.LogError($"{nameof(EngineAssemblySequence)}: Spawned prefab '{stage.grabbablePartPrefab.name}' has no Collider. Add colliders to enable grabbing/triggering.");
            enabled = false;
            return;
        }
    }

    private void SpawnDetectionSphere(AssemblyStage stage)
    {
        _triggerInstance = new GameObject($"PlacementTrigger_Stage{currentStageIndex}");
        _triggerInstance.transform.SetPositionAndRotation(stage.detectionTransform.position, stage.detectionTransform.rotation);

        // Invisible trigger collider.
        var sphere = _triggerInstance.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = stage.detectionRadius;

        // Rigidbody required for reliable trigger behavior in Unity physics.
        var rb = _triggerInstance.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        _trigger = _triggerInstance.AddComponent<PlacementTrigger>();
        _trigger.Initialize(this, _spawnedPartInstance.transform, stage);
    }

    private void HandlePlacedCorrectly(AssemblyStage stage)
    {
        if (_spawnedPartInstance == null) return;

        // Snap & lock the placed part.
      Transform snap =
    stage.finalSnapPose != null ? stage.finalSnapPose :
    stage.enginePartInFinalAssembly != null ? stage.enginePartInFinalAssembly :
    stage.detectionTransform;
        if (snap != null)
        {
            _spawnedPartInstance.transform.SetPositionAndRotation(snap.position, snap.rotation);
        }

        if (_spawnedPartInstance.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Disable XR grabbing to “lock in place”.
        var grab = _spawnedPartInstance.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
        {
            // If currently held, force drop.
            if (grab.isSelected)
            {
                var manager = grab.interactionManager;
                if (manager != null)
                {
                    var interactors = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor>(grab.interactorsSelecting);
                    foreach (var interactor in interactors)
                        manager.SelectExit(interactor, grab);
                }
            }
            grab.enabled = false;
        }

        // Fill the engine part to fully visible.
        SetStagePartAlpha(stage, completedAlpha);

        // Advance.
        BeginStage(currentStageIndex + 1);
    }

    private void SetStagePartAlpha(AssemblyStage stage, float alpha)
    {
        var renderers = GetStageRenderers(stage);
        SetAlpha(renderers, alpha);
    }

    private List<Renderer> GetStageRenderers(AssemblyStage stage)
    {
        var list = new List<Renderer>(16);
        if (stage.enginePartInFinalAssembly == null) return list;

        if (stage.includeChildrenRenderers)
        {
            stage.enginePartInFinalAssembly.GetComponentsInChildren(includeInactive: true, result: list);
        }
        else
        {
            var r = stage.enginePartInFinalAssembly.GetComponent<Renderer>();
            if (r != null) list.Add(r);
        }

        return list;
    }

    private void SetAlpha(List<Renderer> renderers, float alpha)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            SetRendererAlpha(r, alpha);
        }
    }

    private void SetRendererAlpha(Renderer r, float alpha)
    {
        if (!_mpbCache.TryGetValue(r, out var mpb) || mpb == null)
        {
            mpb = new MaterialPropertyBlock();
            _mpbCache[r] = mpb;
        }

        r.GetPropertyBlock(mpb);

        // Read current color if present; otherwise default to white.
        Color c = Color.white;

        // Prefer _BaseColor for URP; if not present, fallback to _Color.
        if (r.sharedMaterial != null)
        {
            if (r.sharedMaterial.HasProperty(baseColorProperty))
                c = r.sharedMaterial.GetColor(baseColorProperty);
            else if (r.sharedMaterial.HasProperty("_Color"))
                c = r.sharedMaterial.GetColor("_Color");
        }

        c.a = alpha;

        if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(baseColorProperty))
            mpb.SetColor(baseColorProperty, c);
        else
            mpb.SetColor("_Color", c);

        r.SetPropertyBlock(mpb);
    }

    private void CompleteSequence()
    {
        currentStageIndex = stages.Count;

        // Fully show everything at the end.
        SetAlpha(_allEngineRenderers, completedAlpha);

        CleanupRuntimeObjects();

        Debug.Log($"{nameof(EngineAssemblySequence)}: Sequence completed.", this);
    }

    private void CleanupRuntimeObjects()
    {
        if (_triggerInstance != null) Destroy(_triggerInstance);
        _triggerInstance = null;
        _trigger = null;

        // Keep placed parts in the world; only destroy if you want a single instance.
        // If you want to keep every placed part, do nothing here.
        // If you want to remove old unplaced parts when stage changes, we already destroy by recreating, but after placement it becomes the placed part.
        // In this implementation: the placed part remains; the next stage spawns a new instance. So do NOT destroy _spawnedPartInstance here.
        _spawnedPartInstance = null;
    }

    // Nested trigger component (kept in the same file/script, per your request).
    private sealed class PlacementTrigger : MonoBehaviour
    {
        private EngineAssemblySequence _owner;
        private Transform _spawnedRoot;
        private AssemblyStage _stage;

        public void Initialize(EngineAssemblySequence owner, Transform spawnedRoot, AssemblyStage stage)
        {
            _owner = owner;
            _spawnedRoot = spawnedRoot;
            _stage = stage;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_owner == null || _spawnedRoot == null || other == null) return;

            // Accept collider belonging to the spawned part (root or any child).
            if (other.transform == _spawnedRoot || other.transform.IsChildOf(_spawnedRoot))
            {
                _owner.HandlePlacedCorrectly(_stage);
            }
        }
    }
}