using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class AccessibleTMPText_Normal : MonoBehaviour
{

    private List<TextMeshPro> TMPUIText = new List<TextMeshPro>();



    private void OnEnable()
    {
        TMPUIText.Clear();

        TMPUIText = GetComponentsInChildren<TextMeshPro>(includeInactive: true).Distinct().ToList(); // make unique for re-parenting cases

        TransformText();
    }

    private void OnDisable()
    {
        TMPUIText.Clear();
    }

    // Built in Unity method: fires once a child gameObject is added, removed, or re-parented
    private void OnTransformChildrenChanged()
    {
        TMPUIText = GetComponentsInChildren<TextMeshPro>(includeInactive: true).Distinct().ToList();

        TransformText();
    }


    private void TransformText()
    {
        for (int i = 0; i < TMPUIText.Count; i++)
        {
            TextMeshPro guiText = TMPUIText[i];

            string original = guiText.text;

            if (string.IsNullOrWhiteSpace(original))
                continue;

            string[] words = original.Split(' ');

            StringBuilder stringBuilder = new StringBuilder(); // regular strings are immutable

            for (int j = 0; j < words.Length; j++)
            {
                string word = words[j];

                if (string.IsNullOrEmpty(word))
                {
                    stringBuilder.Append(" ");
                    continue;
                }

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

            guiText.text = stringBuilder.ToString();
        }
    }
}

