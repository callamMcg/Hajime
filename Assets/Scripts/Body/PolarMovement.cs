using UnityEngine;

//Just incase
[RequireComponent(typeof(JudokaBody))]
public class PolarMovement : MonoBehaviour
{
    //------------------Variables------------------//
    //Component
    private JudokaBody body;
    //Stats
    public float rotationSpeed; //Deg / Sec
    [SerializeField] private float radiusRecoveryRate;

    //Trackers
    private Transform pivot;
    private float currentAngle;
    private float targetAngle;
    private float radius;
    private float restRadius;

    //------------------Unity Functions------------------//
    private void Awake() { body = GetComponent<JudokaBody>(); }

    //------------------Public Functions------------------//
    //read polar position and set the target angle to the current angle plus the mod
    public void SetTarget(Transform centre, float degrees)
    {
        pivot = centre;
        Vector2 ground = body.Planar() - new Vector2(pivot.position.x, pivot.position.z);
        radius = ground.magnitude;
        currentAngle = Mathf.Atan2(ground.y, ground.x) * Mathf.Rad2Deg;
        targetAngle  = currentAngle + (degrees * Mathf.Rad2Deg);
    }

    // Set the rest distance to the current distance to centre
    public void CaptureRestRadius(Transform centre)
    {
        Vector2 ground = body.Planar() - new Vector2(centre.position.x, centre.position.z);
        restRadius = ground.magnitude;
    }

    /* MOVE 
     * 1 - step the angle
     * 2 - recover the radius
     * 3 push the ground position 
     */
    public void Move()
    {
        if (pivot == null) return;
        //1
        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);

        //2
        if (restRadius > 0f)
            radius = restRadius + (radius - restRadius) * Mathf.Exp(-radiusRecoveryRate * Time.deltaTime);
        
        float rad = currentAngle * Mathf.Deg2Rad;

        //3
        Vector2 ground;
        ground.x = pivot.position.x + radius * Mathf.Cos(rad);
        ground.y = pivot.position.z + radius * Mathf.Sin(rad);
        body.SetPlanar(ground);
    }

    public void SetDistance(float distance) { restRadius = distance; }
}
