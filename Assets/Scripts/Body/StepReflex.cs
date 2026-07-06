using UnityEngine;

//Just incase
[RequireComponent(typeof(Balance), typeof(Facing))]
public class StepReflex : MonoBehaviour
{
    //------------------Variables------------------//
    //Components
    private Balance balance;
    private Facing  facing;
    //Stats
    [SerializeField] private float stepSpeed;

    //------------------Unity Functions------------------//
    //Get components
    private void Awake()
    {
        balance = GetComponent<Balance>();
        facing  = GetComponent<Facing>();
    }

    /* GET STEP
     * return the velocity (x,z) of the feet chasing the centre of mass.
     * 1 - Zero until the lean passes the step threshold
     * 2 - Return the direction magnified by how off balance they are and their speed
     */
    public Vector2 GetStep()
    {
        float rate = balance.Rate();
        // 1
        if (rate <= 0f) return Vector2.zero;
        //2
        return Direction() * (rate * stepSpeed); 
    }

    // World direction the body is tipping
    private Vector2 Direction()
    {
        float rad = facing.Yaw * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector2 right = new Vector2(forward.y, -forward.x);
        Vector3 norm = balance.NormalisedLean();
        return (forward * norm.x - right * norm.z).normalized;
    }
}
