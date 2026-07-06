using UnityEngine;

/// <summary>
/// This script manages the yaw of the judoka and hands it to the body
/// </summary>

//Just Incase
[RequireComponent(typeof(JudokaBody))]
public class Facing : MonoBehaviour
{
    //------------------Variables------------------//
    //Components
    private JudokaBody body;
    //Trackers
    private float yaw;
    //------------------Unity Functions------------------//
    private void Awake() { body = GetComponent<JudokaBody>(); }

    //------------------Public Functions------------------//
    // turn to face a target around y only, then push the yaw 
    public void LookAt(Transform target)
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        offset = offset.normalized;
        yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        body.SetYaw(yaw);
    }

    //Getter
    public float Yaw => yaw;
}
