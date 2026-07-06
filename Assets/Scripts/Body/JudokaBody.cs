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

    /* APPLY
     * The one place in that writes the transform
     * 1 - position = planar (x, z) + height (y)
     * 2 - rotation = lean pitch (x) + facing yaw (y) + lean roll (z)
     */
    private void LateUpdate()
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
    public void ApplyPose(BodyPose p)
    {
        planar = p.planar;
        height = p.height;
        lean = p.lean;
        yaw = p.yaw;
    }

    //Getters
    public BodyPose GetPose() => new BodyPose { planar = planar, height = height, lean = lean, yaw = yaw };

    public Vector2 Planar() { return planar;  }
    public Vector3 WorldPosition() { return new Vector3(planar.x, height, planar.y); }
}
