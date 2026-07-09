using UnityEngine;

/// <summary>
/// This script manages the motions applied to the judokas body
/// This script is the only one to edit the transform 
/// And should be the only source of reference for the position and rotation
/// </summary>


public class JudokaBody : MonoBehaviour
{
    //------------------Variables------------------//
    // Positional descriptions of the judoka
    private Vector2 planar; // ground position: x = X, y = Z
    private float height; // Y
                          // (this could be put into one vector3 but I made the functionality at different times and its not needed)
    private Vector3 lean; // balance lean: pitch in x, roll in z
    private float yaw; // facing rotation around y

    //------------------Unity Functions------------------//
    private void Awake()
    {
        planar = new Vector2(transform.position.x, transform.position.z);
        height = transform.position.y;
    }

    // Gameplay writes land at the end of the frame, once every system has spoken
    private void LateUpdate() => Write();

    /* WRITE
     * The one place that writes the transform
     * 1 - position = planar (x, z) + height (y)
     * 2 - rotation = lean pitch (x) + facing yaw (y) + lean roll (z)
     */
    private void Write()
    {
        // 1
        transform.position = new Vector3(planar.x, height, planar.y);
        // 2
        transform.rotation = Quaternion.Euler(lean.x, yaw, lean.z);
    }

    //------------------Public Functions------------------//
    //Setters
    public void SetPlanar(Vector2 position) { planar = position; }
    public void MovePlanar(Vector2 delta) { planar += delta; }
    public void SetHeight(float y) { height = y; }
    public void SetLean(Vector3 amount) { lean = amount; }
    public void SetYaw(float degrees) { yaw = degrees; }

    /* APPLY POSE - the replay's entry point
     * Adopt the pose and write it through immediately, without waiting for
     * LateUpdate: the replay places limb targets in world space straight
     * after this call, and a target parented to a judoka needs the root
     * already standing in the right place, or it is dragged when the root
     * finally moves. LateUpdate then rewrites the same values, harmlessly.
     */
    public void ApplyPose(BodyPose p)
    {
        planar = p.planar;
        height = p.height;
        lean = p.lean;
        yaw = p.yaw;
        Write();
    }

    //Getters
    public BodyPose GetPose() => new BodyPose { planar = planar, height = height, lean = lean, yaw = yaw };

    public Vector2 Planar() { return planar; }
    public Vector3 WorldPosition() { return new Vector3(planar.x, height, planar.y); }
}