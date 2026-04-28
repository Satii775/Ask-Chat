using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class AccessibleTMPText : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _root;

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();

        // Wait for the next frame or until the layout is ready if necessary, 
        // but generally rootVisualElement is available in OnEnable
        _root = _uiDocument.rootVisualElement;

        if (_root != null)
        {
            TransformAllText();

            // Optional: Register a callback if the UI hierarchy changes dynamically
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }

    private void OnDisable()
    {
        if (_root != null)
        {
            _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        // This acts similarly to OnTransformChildrenChanged for UI Toolkit
        TransformAllText();
    }

    public void TransformAllText()
    {
        if (_root == null) return;

        // Query all elements that contain text (Labels, Buttons, etc.)
        List<TextElement> textElements = _root.Query<TextElement>().ToList();

        foreach (var element in textElements)
        {
            string original = element.text;

            if (string.IsNullOrWhiteSpace(original))
                continue;

            // Prevent double-processing if the text is already bolded by this script
            if (original.Contains("<b>"))
                continue;

            element.text = ProcessBionicText(original);
        }
    }

    private string ProcessBionicText(string original)
    {
        string[] words = original.Split(' ');
        StringBuilder stringBuilder = new StringBuilder();

        for (int j = 0; j < words.Length; j++)
        {
            string word = words[j];

            if (string.IsNullOrEmpty(word))
            {
                stringBuilder.Append(" ");
                continue;
            }

            // Calculate bionic bolding (40% of word)
            int boldCount = Mathf.CeilToInt(word.Length * 0.4f);
            boldCount = Mathf.Clamp(boldCount, 1, word.Length);

            string boldPart = word.Substring(0, boldCount);
            string restPart = word.Substring(boldCount);

            stringBuilder.Append("<b>");
            stringBuilder.Append(boldPart);
            stringBuilder.Append("</b>");
            stringBuilder.Append(restPart);

            if (j < words.Length - 1)
                stringBuilder.Append(" ");
        }

        return stringBuilder.ToString();
    }
}
