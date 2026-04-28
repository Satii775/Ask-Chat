using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuHandler : MonoBehaviour
{
    private DropdownField colorBlindnessDropdown;
    private DropdownField accessibleTextDropdown;

    [SerializeField] private GameObject protFilter;
    [SerializeField] private GameObject deuterFilter;
    [SerializeField] private GameObject tritanFilter;
    [SerializeField] private GameObject monoFilter;

    [SerializeField] private AccessibleTMPText textComponent;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        colorBlindnessDropdown = root.Q<DropdownField>("color-blindness-dropdown");

        if (colorBlindnessDropdown != null)
        {
            colorBlindnessDropdown.RegisterValueChangedCallback(OnColorBlindnessChanged);
        }
        else
        {
            Debug.LogError("DropdownField not found. Ensure the name matches the UI Builder configuration.");
        }

        accessibleTextDropdown = root.Q<DropdownField>("accessible-text-dropdown");

        if (accessibleTextDropdown != null)
        {
            accessibleTextDropdown.RegisterValueChangedCallback(OnAccessibleTextChanged);
        }
        else
        {
            Debug.LogError("DropdownField not found. Ensure the name matches the UI Builder configuration.");
        }
    }

    private void OnDisable()
    {
        if (colorBlindnessDropdown != null)
        {
            colorBlindnessDropdown.UnregisterValueChangedCallback(OnColorBlindnessChanged);
        }
    }

    private void OnColorBlindnessChanged(ChangeEvent<string> evt)
    {
        string selectedMode = evt.newValue;
        Debug.Log($"Accessibility mode switched to: {selectedMode}");

        switch (selectedMode)
        {
            case "None":
                // Disable all filters
                protFilter.SetActive(false);
                deuterFilter.SetActive(false);
                tritanFilter.SetActive(false);
                monoFilter.SetActive(false);
                break;
            case "Protanomaly":
                // Enable Protanomaly filter
                protFilter.SetActive(true);
                deuterFilter.SetActive(false);
                tritanFilter.SetActive(false);
                monoFilter.SetActive(false);
                break;
            case "Deuteranomaly":
                protFilter.SetActive(false);
                deuterFilter.SetActive(true);
                tritanFilter.SetActive(false);
                monoFilter.SetActive(false);
                break;
            case "Tritanopia":
                protFilter.SetActive(false);
                deuterFilter.SetActive(false);
                tritanFilter.SetActive(true);
                monoFilter.SetActive(false);
                break;
            case "Monochromacy":
                protFilter.SetActive(false);
                deuterFilter.SetActive(false);
                tritanFilter.SetActive(false);
                monoFilter.SetActive(true);
                break;
        }
    }

    private void OnAccessibleTextChanged(ChangeEvent<string> evt)
    {
        string selectedOption = evt.newValue;
        Debug.Log($"Accessible text option changed to: {selectedOption}");
        switch (selectedOption)
        {
            case "On":
                textComponent.enabled = true;
                break;
            case "Off":
                textComponent.enabled = false;
                break;
        }
    }
}
