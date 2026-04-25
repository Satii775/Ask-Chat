using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2f, 0f);
    public Camera cam;

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;

        if (cam != null)
        {
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                cam.transform.rotation * Vector3.up);

            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.x = 0f;
            transform.eulerAngles = eulerAngles;
        }
    }
}