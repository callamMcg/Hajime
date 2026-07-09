using UnityEngine;
using static FootManager;

/// <summary>
/// Times both feet of one judoka from the Gait's shared phase.
/// Every cycle: both feet are planted through the bob's low point, the lead
/// foot lifts, the trailing foot follows, both are airborne over the bob's
/// peak, then the lead plants first and the trail catches up. Planted feet
/// hold their world position - no sliding - but rotate with the body. A foot
/// that cannot reach the floor hovers beneath its hip until it can.
/// </summary>

// Runs after JudokaBody (order 0) has written the frame's transform, so the
// feet are placed against the body's final position
[RequireComponent(typeof(Gait))]
[DefaultExecutionOrder(50)]
public class FootManager : MonoBehaviour
{
    //------------------Foot------------------//
    [System.Serializable]
    public class Foot
    {
        // References
        public Transform legRoot;     // hip joint, the same transform the leg's solver uses as its root
        public Transform target;      // the IK end target this manager drives
        public Transform sweepTarget; // the opponent's leg, chased while sweeping

        // Trackers
        [HideInInspector] public float side;         // -1 left, +1 right
        [HideInInspector] public FootState state;
        [HideInInspector] public Vector3 planted;    // world point being held
        [HideInInspector] public Vector3 home;       // ideal ground point under the stance
        [HideInInspector] public Vector3 homeVelocity;
        [HideInInspector] public Vector3 lastHome;
        [HideInInspector] public bool hasHome;
        [HideInInspector] public Vector3 swingFrom;  // where the current swing began
        [HideInInspector] public float liftPhase;    // phase at which the swing began
        [HideInInspector] public int liftCycle = -1; // stamp so a foot lifts once per cycle
        [HideInInspector] public float progress;     // 0-1 through the current swing

        // Getters
        public bool IsGrounded => state == FootState.Planted || state == FootState.Based;
        public float SwingProgress => state == FootState.Swinging ? progress : (IsGrounded ? 1f : 0f);
    }

    //------------------Variables------------------//
    // Components
    private Gait gait;

    // Feet
    [SerializeField] private Foot left;
    [SerializeField] private Foot right;
    [SerializeField] private LayerMask groundLayer;

    // Stance
    [SerializeField] private float stanceWidth = 0.12f;      // how far outboard of the root each foot rests
    [SerializeField] private float maxReach = 0.85f;         // root-to-foot distance beyond which the foot hovers
    [SerializeField] private float castDistance = 1.5f;      // how far below the root to look for floor
    [SerializeField] private float restlessDistance = 0.08f; // home drift that asks the gait for a cycle

    // Cycle windows - flight lasts (swingFraction - stagger) of the cycle, and
    // fades out at low speed as the stagger stretches to meet the swing
    [SerializeField, Range(0.1f, 0.5f)] private float swingFraction = 0.35f; // fraction of the cycle each foot is airborne
    [SerializeField, Range(0f, 0.5f)] private float stagger = 0.18f;       // phase gap between lead and trail lift-off

    // Step
    [SerializeField] private float liftHeight = 0.08f; // peak of the swing arc at full speed
    [SerializeField] private float stepLead = 0.15f;   // seconds of home velocity added to the landing
    [SerializeField] private float turnSpeed = 480f;   // degrees per second a foot turns to match the body

    // Trackers
    private Foot lead;  // steps first this cycle
    private Foot trail;
    private Foot[] feet;
    private int seenCycle = -1;

    //------------------Bounds Access------------------//
    // The bounds watcher reads foot positions and grounded state to decide
    // whether a planted foot has stepped off the mat
    public Transform LeftFoot => left.target;
    public Transform RightFoot => right.target;
    public bool LeftGrounded => left.IsGrounded;
    public bool RightGrounded => right.IsGrounded;

    //------------------Unity Functions------------------//
    private void Awake()
    {
        gait = GetComponent<Gait>();
        left.side = -1f;
        right.side = 1f;
        lead = right;
        trail = left;
        feet = new Foot[] { left, right };
    }

    /* START
     * Find each foot's first home and plant there, or hold where it was placed
     */
    private void Start()
    {
        foreach (Foot foot in feet)
        {
            UpdateHome(foot, Time.deltaTime);
            foot.planted = foot.hasHome ? foot.home : foot.target.position;
            foot.lastHome = foot.home;
            foot.state = FootState.Planted;
            foot.target.position = foot.planted;
        }
    }

    /* LATE UPDATE
     * 1 - Refresh each foot's home point and its velocity
     * 2 - On a new cycle, choose which foot leads
     * 3 - Build the two lift windows, centred on the bob's peak
     * 4 - Drive the lead then the trail, so a landing frees the other to lift
     * 5 - If a foot needs the phase to move - mid swing, out of place, or
     *     hovering over reachable floor - keep the gait cycling
     */
    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1
        foreach (Foot foot in feet) UpdateHome(foot, dt);

        // 2
        if (gait.Cycle != seenCycle)
        {
            seenCycle = gait.Cycle;
            ChooseLead();
        }

        // 3
        float effStagger = Mathf.Lerp(swingFraction, stagger, gait.SpeedFactor);
        float windowStart = 0.5f - (swingFraction + effStagger) * 0.5f;

        // 4
        Drive(lead, windowStart, dt);
        Drive(trail, windowStart + effStagger, dt);

        // 5
        if (WantsCycle()) gait.RequestCycle();
    }

    //------------------Private Functions------------------//
    /* DRIVE
     * Planted  - pin the world position, rotate with the body, lift when the
     *            phase enters this foot's window, hover if out of reach
     * Swinging - progress by phase, chase the moving landing point, arc the
     *            lift, plant on completion
     * Hovering - dangle beneath the hip until the floor is back in reach,
     *            then swing home
     * Swept    - chase the opponent's leg (technique control)
     * Based    - pinned by a technique, will not lift
     */
    private void Drive(Foot foot, float liftAt, float dt)
    {
        switch (foot.state)
        {
            case FootState.Swept:
                {
                    foot.target.position = Vector3.Lerp(foot.target.position, foot.sweepTarget.position, 10f * dt);
                    return;
                }
            case FootState.Based:
                {
                    foot.target.position = foot.planted;
                    Turn(foot, turnSpeed, dt);
                    return;
                }
            case FootState.Planted:
                {
                    // out of reach - the body has been lifted or dragged away
                    if (!foot.hasHome || !CanReach(foot, foot.planted))
                    {
                        foot.state = FootState.Hovering;
                        return;
                    }
                    // lift once the phase enters this foot's window
                    if (gait.IsCycling && foot.liftCycle != seenCycle && Wrapped(gait.Phase - liftAt) < swingFraction)
                    {
                        BeginSwing(foot);
                        return;
                    }
                    foot.target.position = foot.planted;
                    Turn(foot, turnSpeed, dt);
                    return;
                }
            case FootState.Swinging:
                {
                    foot.progress = Mathf.Clamp01(Wrapped(gait.Phase - foot.liftPhase) / swingFraction);
                    float u = foot.progress;

                    // the landing chases the live home, with a little lead along its travel
                    Vector3 landing = foot.home + foot.homeVelocity * stepLead;
                    if (!foot.hasHome || !CanReach(foot, landing, 1.1f))
                    {
                        foot.state = FootState.Hovering;
                        return;
                    }

                    float ease = u * u * (3f - 2f * u);
                    Vector3 pos = Vector3.Lerp(foot.swingFrom, landing, ease);
                    pos.y += liftHeight * Mathf.Lerp(0.35f, 1f, gait.SpeedFactor) * 4f * u * (1f - u);
                    foot.target.position = pos;
                    Turn(foot, turnSpeed * 2f, dt);

                    if (u >= 1f)
                    {
                        foot.planted = landing;
                        foot.target.position = landing;
                        foot.state = FootState.Planted;
                    }
                    return;
                }
            case FootState.Hovering:
                {
                    // dangle beneath the hip
                    Vector3 dangle = foot.legRoot.position
                                   + Flat(transform.right) * (stanceWidth * foot.side)
                                   + Vector3.down * (maxReach * 0.9f);
                    foot.target.position = Vector3.Lerp(foot.target.position, dangle, 12f * dt);
                    Turn(foot, turnSpeed, dt);

                    // floor back in reach - swing home (WantsCycle keeps the phase moving)
                    if (foot.hasHome && gait.IsCycling && CanReach(foot, foot.home, 0.95f))
                        BeginSwing(foot);
                    return;
                }
        }
    }

    private void BeginSwing(Foot foot)
    {
        foot.state = FootState.Swinging;
        foot.swingFrom = foot.target.position;
        foot.liftPhase = gait.Phase;
        foot.liftCycle = seenCycle;
        foot.progress = 0f;
    }

    /* CHOOSE LEAD
     * Score each foot for the coming cycle: the foot on the side of travel
     * leads, turning favours the foot on the inside of the turn, and the
     * current lead keeps a small edge so the choice is stable when the
     * movement is ambiguous. The trail then restores the stance behind it -
     * tsugi ashi in every direction, and the feet can never cross.
     */
    private void ChooseLead()
    {
        if (Score(trail) > Score(lead) + 0.15f)
        {
            Foot swap = lead;
            lead = trail;
            trail = swap;
        }
    }

    private float Score(Foot foot)
    {
        Vector3 outboard = Flat(transform.right) * foot.side;
        float score = 0f;

        Vector2 travel = gait.PlanarVelocity;
        if (travel.sqrMagnitude > 0.0004f)
            score += Vector2.Dot(new Vector2(outboard.x, outboard.z), travel.normalized);

        score += Mathf.Clamp(gait.YawVelocity / 90f, -1f, 1f) * 0.5f * foot.side;
        return score;
    }

    /* UPDATE HOME
     * 1 - Project the leg root outboard of the body
     * 2 - Raycast down for the floor; no hit means no home, so the foot
     *     hovers rather than reaching for a floor that is not there
     * 3 - Track how fast the home is travelling, for landing prediction
     */
    private void UpdateHome(Foot foot, float dt)
    {
        bool had = foot.hasHome;

        // 1
        Vector3 origin = foot.legRoot.position + Flat(transform.right) * (stanceWidth * foot.side);

        // 2
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castDistance, groundLayer))
        {
            foot.home = hit.point;
            foot.hasHome = true;
        }
        else foot.hasHome = false;

        // 3
        foot.homeVelocity = (foot.hasHome && had && dt > 0f)
            ? (foot.home - foot.lastHome) / dt
            : Vector3.zero;
        foot.lastHome = foot.home;
    }

    // A cycle should run, or keep running, when a foot depends on the phase
    private bool WantsCycle()
    {
        foreach (Foot foot in feet)
        {
            if (foot.state == FootState.Swinging) return true; // never strand a swing
            if (foot.state == FootState.Hovering && foot.hasHome && CanReach(foot, foot.home, 0.95f)) return true;
            if (foot.state == FootState.Planted && foot.hasHome)
            {
                Vector3 drift = foot.home - foot.planted;
                drift.y = 0f;
                if (drift.magnitude > restlessDistance) return true; // dragged off its home
            }
        }
        return false;
    }

    private bool CanReach(Foot foot, Vector3 point, float margin = 1f)
    {
        return Vector3.Distance(foot.legRoot.position, point) <= maxReach * margin;
    }

    // Rotate the foot to face with the body around y only
    private void Turn(Foot foot, float speed, float dt)
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return;
        Quaternion face = Quaternion.LookRotation(forward.normalized, Vector3.up);
        foot.target.rotation = Quaternion.RotateTowards(foot.target.rotation, face, speed * dt);
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.normalized; }

    private static float Wrapped(float x) => x - Mathf.Floor(x);

    //------------------Technique Control------------------//
    // Sweep with one foot: it chases the opponent's leg while the other bases
    public void Sweep(FootId id)
    {
        Foot sweeping = Get(id);
        Foot basing = Get(id == FootId.Left ? FootId.Right : FootId.Left);

        sweeping.state = FootState.Swept;
        if (basing.state != FootState.Based)
        {
            if (!basing.IsGrounded) basing.planted = basing.hasHome ? basing.home : basing.target.position;
            basing.state = FootState.Based;
        }
    }

    // Release technique control: the base is simply planted again, a swept
    // foot hovers and then swings itself home once it can reach the floor
    public void Free()
    {
        foreach (Foot foot in feet)
        {
            if (foot.state == FootState.Swept) foot.state = FootState.Hovering;
            else if (foot.state == FootState.Based) foot.state = FootState.Planted;
        }
    }

    //------------------Getters------------------//
    public Foot Get(FootId id) => id == FootId.Left ? left : right;
    public Foot Left => left;
    public Foot Right => right;
    public Foot Lead => lead;
    public Foot Trail => trail;
    public bool BothAirborne => !left.IsGrounded && !right.IsGrounded;
    public bool BothGrounded => left.IsGrounded && right.IsGrounded;
}