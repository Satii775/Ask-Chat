using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SeatedModeManager : MonoBehaviour
{
    public static SeatedModeManager Instance { get; private set; }

    public enum HeightMode { Standing, Seated }

    [Header("Settings")]
    [Tooltip("Virtual eye height in meters. Both standing and seated players end up here.")]
    [SerializeField] private float targetEyeHeight = 1.7f;

    public HeightMode CurrentMode { get; private set; } = HeightMode.Standing;
    public bool HasChosenMode { get; private set; } = false;

    public float StoredOffset { get; private set; } = 0f;

    private XROrigin xrOrigin;
    private Transform headCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindXRRig();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (HasChosenMode)
            StartCoroutine(ApplyOffsetAfterFrame());
    }

    private IEnumerator ApplyOffsetAfterFrame()
    {
        yield return null;
        yield return null;
        FindXRRig();
        ApplyStoredOffsetToCurrentRig();
    }

    private void FindXRRig()
    {
        xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
            headCamera = xrOrigin.Camera.transform;
    }

    public void ApplyHeightNormalization(HeightMode mode)
    {
        FindXRRig();

        if (xrOrigin == null || headCamera == null)
        {
            Debug.LogError("[SeatedModeManager] No XROrigin found in scene.");
            return;
        }

        // In Floor tracking mode, headCamera.localPosition.y is the player's real
        // head height above the floor.
        float realHeadHeight = headCamera.localPosition.y;
        StoredOffset = targetEyeHeight - realHeadHeight;
        CurrentMode = mode;
        HasChosenMode = true;

        ApplyStoredOffsetToCurrentRig();

        Debug.Log($"[SeatedModeManager] Mode={mode} | RealHead={realHeadHeight:F2}m | " +
                  $"Offset={StoredOffset:F2}m | Target={targetEyeHeight:F2}m");
    }

    private void ApplyStoredOffsetToCurrentRig()
    {
        if (xrOrigin == null || xrOrigin.CameraFloorOffsetObject == null)
        {
            Debug.LogError("[SeatedModeManager] XROrigin or CameraFloorOffsetObject missing — " +
                           "make sure the XR Origin's 'Camera Floor Offset Object' field is assigned.");
            return;
        }

        var offsetTransform = xrOrigin.CameraFloorOffsetObject.transform;
        var pos = offsetTransform.localPosition;
        pos.y = StoredOffset;
        offsetTransform.localPosition = pos;
    }
}

