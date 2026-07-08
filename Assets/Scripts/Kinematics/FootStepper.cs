using UnityEngine;

/// <summary>
/// Moves one foot IK target to mimic a side step.
/// While planted the target holds its world position as the body moves above it.
/// Its ideal home is the floor just outboard of the leg root; once the root can
/// no longer comfortably reach the planted point, the foot lifts and arcs home.
/// </summary>

//Run after JudokaBody (order 0) so a planted foot can re-assert its world
//position after the parent has been moved for the frame
[DefaultExecutionOrder(50)]
public class FootStepper : MonoBehaviour
{
    //------------------Variables------------------//
    //References
    [SerializeField] private Transform legRoot;     // same transform the leg's solver uses as rootJoint
    [SerializeField] private Transform body;        // character root, gives facing and lateral direction
    [SerializeField] private FootStepper otherFoot; // opposite foot, so both are never airborne together
    [SerializeField] private Transform opponentsLeg; // opponentsLeg
    [SerializeField] private LayerMask groundLayer;

    //Stance
    [SerializeField] private float side = 1f;           // +1 right foot, -1 left foot
    [SerializeField] private float stanceWidth = 0.12f; // how far outboard of the root the foot rests
    [SerializeField] private float maxReach = 0.85f;    // root to foot distance that forces a step
    [SerializeField] private float castDistance = 1.5f; // how far below the root to look for floor

    //Step
    [SerializeField] private float stepDuration = 0.25f; // seconds in the air
    [SerializeField] private float stepHeight = 0.08f;   // peak of the arc
    [SerializeField] private float stepLead = 0.15f;     // seconds of home velocity added to the landing

    //Trackers
    private Vector3 planted;      // world point the foot is holding
    private Vector3 home;         // ideal ground point, outboard of the root
    private Vector3 homeVelocity; // how fast the home point is travelling
    private Vector3 lastHome;
    private bool hasHome;
    private Vector3 stepFrom;
    private float stepT = 1f;     // step progress, >= 1 means planted

    //Getters
    public bool IsStepping => stepT < 1f;

    [SerializeField] private float moveThreshold = 0.05f;    // m/s of root travel to allow a step
    [SerializeField] private float turnThreshold = 15f;      // deg/s of yaw to allow a step

    private Vector3 lastBodyPos;
    private float lastBodyYaw;
    private bool bodyMoving;

    private bool isSweeping;
    private bool isBasing;

    //------------------Unity Functions------------------//
    /*Start
     * 1 - Find the first home point and plant there, or hold where we were placed
     */
    private void Start()
    {
        FindHome();
        planted = hasHome ? home : transform.position;
        lastHome = home;
    }
    private void Update()
    {
        lastBodyPos = body.position;
        lastBodyYaw = body.eulerAngles.y;
    }
    /*Late Update
     * 1 - Refresh the home point, and its velocity while it stays valid
     * 2 - Advance a step in flight, or hold the plant and test whether one is needed
     * 3 - Keep the foot flat on the floor, facing with the body
     */
    private void LateUpdate()
    {
        //0 - how far the body moved this frame, planar speed + yaw speed
        if (Time.deltaTime > 0f)
        {
            Vector3 delta = body.position - lastBodyPos;
            delta.y = 0f;
            float linear = delta.magnitude / Time.deltaTime;
            float angular = Mathf.Abs(Mathf.DeltaAngle(lastBodyYaw, body.eulerAngles.y)) / Time.deltaTime;
            bodyMoving = linear > moveThreshold || angular > turnThreshold;
        }
        if (isSweeping)
        {
            transform.position = Vector3.Lerp(transform.position, opponentsLeg.position, Time.deltaTime * 10);
            return;
        }
        else if (isBasing) return;
        //1
        bool hadHome = hasHome;
        FindHome();
        homeVelocity = (hasHome && hadHome && Time.deltaTime > 0f)
            ? (home - lastHome) / Time.deltaTime
            : Vector3.zero;
        lastHome = home;

        //2
        if (IsStepping) Step();
        else Hold();

        //3
        Vector3 flatForward = body.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }

    //------------------Private Functions------------------//
    /* FIND HOME
     * 1 - Project the leg root outward to the side of the body
     * 2 - Raycast down for the floor, keeping the old home if nothing is hit
     */
    private void FindHome()
    {
        //1
        Vector3 lateral = body.right;
        lateral.y = 0f;
        lateral.Normalize();
        Vector3 origin = legRoot.position + lateral * (stanceWidth * side);

        //2
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castDistance, groundLayer))
        {
            home = hit.point;
            hasHome = true;
        }
    }

    /* HOLD
     * 1 - Pin the target to its planted world point
     * 2 - Begin a step once the root is too far from the plant to keep reaching it,
     *     waiting while the other foot is mid step
     */
    private void Hold()
    {
        //1
        transform.position = planted;

        //2
        if (!hasHome) return;
        if (otherFoot != null && otherFoot.IsStepping) return;
        if (!bodyMoving) return;                                     // wait: no step while the body is still
        if (Vector3.Distance(legRoot.position, planted) > maxReach)
        {
            stepFrom = planted;
            stepT = 0f;
        }
    }

    /* STEP
     * 1 - Advance and clamp the step timer
     * 2 - Chase the live landing point, home plus a little lead along its travel
     * 3 - Ease along the ground line and add a parabolic lift
     * 4 - On landing, adopt the target as the new plant
     */
    private void Step()
    {
        //1
        stepT = Mathf.Min(stepT + Time.deltaTime / stepDuration, 1f);

        //2
        Vector3 stepTo = home + homeVelocity * stepLead;

        //3
        float ease = stepT * stepT * (3f - 2f * stepT);
        Vector3 pos = Vector3.Lerp(stepFrom, stepTo, ease);
        pos.y += stepHeight * 4f * stepT * (1f - stepT);
        transform.position = pos;

        //4
        if (stepT >= 1f)
        {
            planted = stepTo;
            transform.position = planted;
        }
    }

    public void Sweep() 
    {
        isSweeping = true;
        isBasing = false;
    }
    public void Plant()
    {
        isSweeping = false;
        isBasing = true;
    }
    public void Free()
    {
        isSweeping = false;
        isBasing = false;
    }
}