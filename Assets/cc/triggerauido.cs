using UnityEngine;

public class triggerauido : MonoBehaviour
{
  public audioObject clip;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            vocals.instance.say(clip);
        }
    }

    // put on trigger box collider
}
