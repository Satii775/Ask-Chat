using System.Diagnostics;
//using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class vocals : MonoBehaviour
{
    private AudioSource source;
    public static vocals instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this; 
      
    }

       private void Start()
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        public void say(audioObject clip)
        {
            if (source.isPlaying)
            {
            
                source.Stop();
            }
            source.PlayOneShot(clip.clip);
            

            ui.instance.setsubtitle(clip.subtitle, clip.clip.length);
        }

    // put on player
  
}
