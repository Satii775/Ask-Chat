using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeightChoiceUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button standingButton;
    [SerializeField] private Button seatedButton;

    [Header("Next Scene")]
    [Tooltip("Name of the gameplay scene to load after the player chooses.")]
    [SerializeField] private string nextSceneName = "Gameplay";

    [Tooltip("Seconds to wait after the player clicks before loading the next scene. " +
             "Gives them a moment to feel the height change.")]
    [SerializeField] private float postSelectDelay = 0.6f;

    private bool selectionMade = false;

    private void Start()
    {
        if (standingButton != null)
            standingButton.onClick.AddListener(() => OnHeightChosen(SeatedModeManager.HeightMode.Standing));

        if (seatedButton != null)
            seatedButton.onClick.AddListener(() => OnHeightChosen(SeatedModeManager.HeightMode.Seated));
    }

    private void OnHeightChosen(SeatedModeManager.HeightMode mode)
    {
        if (selectionMade) return;
        selectionMade = true;

        if (SeatedModeManager.Instance == null)
        {
            Debug.LogError("[HeightChoiceUI] No SeatedModeManager in scene. Add one to your earliest scene.");
            return;
        }

        SeatedModeManager.Instance.ApplyHeightNormalization(mode);

        // Disable buttons so the player can't double-tap during the delay.
        if (standingButton != null) standingButton.interactable = false;
        if (seatedButton != null) seatedButton.interactable = false;

        StartCoroutine(LoadNextSceneAfterDelay());
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        if (postSelectDelay > 0f)
            yield return new WaitForSeconds(postSelectDelay);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
