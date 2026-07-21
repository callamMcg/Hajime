using UnityEngine;

public class ReplayCam : MonoBehaviour
{
    [SerializeField] private GameObject tori;
    [SerializeField] private float lookHeight; 
    [SerializeField] private float fov;
    
    private Camera cam;
    void Start()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fov;
    }

    void Update()
    {
        Vector3 look = tori.transform.position;
        look.y = lookHeight;
        transform.LookAt(look);
        cam.fieldOfView = fov;
    }
}
