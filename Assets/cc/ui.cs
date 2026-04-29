using System.Collections;
using TMPro;
//using Unity.VisualScripting;
using UnityEngine;

public class ui : MonoBehaviour
{
   [SerializeField] TextMeshProUGUI subitletext = default;
   public static ui instance;
    void Awake()    {
        instance = this;
        clearSubtitles();
    }

    public void setsubtitle(string subtitle, float delay)
    {
        subitletext.text = subtitle;
        StartCoroutine(clearafterseconds(delay));
    }
    public void clearSubtitles()
    {
        subitletext.text = "";
    }

    private IEnumerator clearafterseconds(float delay)
    {
        yield return new WaitForSeconds(delay);
        clearSubtitles();
    }

    // put on canvas/ ui
}
