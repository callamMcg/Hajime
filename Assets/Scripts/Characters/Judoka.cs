using UnityEngine;

//Insure all of the required components are attached to the character
[RequireComponent(typeof(JudokaBody), typeof(Balance), typeof(Facing))]
[RequireComponent(typeof(PolarMovement), typeof(Gait))]
public abstract class Judoka : MonoBehaviour
{
    //------------------Variables------------------//
    // Components
    protected JudokaBody body;
    protected Balance balance;
    protected Facing facing;
    protected PolarMovement movement;
    protected Gait gait;
   
    //Limbs
    [SerializeField] protected IKContext leftLeg;
    [SerializeField] protected IKContext rightLeg;
    [SerializeField] protected IKContext leftArm;
    [SerializeField] protected IKContext rightArm;


    // Reference
    [SerializeField] protected Transform opponent;

    // Stats
    [SerializeField] protected Vector3 balanceLimits;

    //Tracker
    private float lookAngle;
    private float lookAngleTarget;
    public float LookAngle => lookAngle;
    public void SetLookAngle(float deg) { lookAngleTarget = deg; }

    // Yaw hold: a technique can freeze the facing so the yaw stops tracking the
    // opponent (e.g. tori through a hip throw, while uke is vaulted around him)
    private bool yawHeld;
    private float heldYaw;
    public float HeldYaw => heldYaw;
    public void HoldYaw() { yawHeld = true; heldYaw = body.GetPose().yaw; } // freeze where it stands
    public void SetHeldYaw(float degrees) { heldYaw = degrees; }            // command the frozen yaw (ease back to square)
    public void ReleaseYaw() { yawHeld = false; }                          // hand the yaw back to opponent tracking
    public float FacingYaw() => facing.YawTo(opponent);                    // the yaw that squares up to the opponent right now
    public void SnapLook(float deg) { lookAngle = deg; lookAngleTarget = deg; } // set the look offset without easing (no pop on release)

    //------------------Unity Functions------------------//

    // Get the components
    protected virtual void Awake()
    {
        body = GetComponent<JudokaBody>();
        balance = GetComponent<Balance>();
        facing = GetComponent<Facing>();
        movement = GetComponent<PolarMovement>();
        gait = GetComponent<Gait>();
    }

    // Apply the balance limits
    protected virtual void Start() => balance.SetLimits(balanceLimits);

    // Advance the movement clock, hand its height to the body, face the opponent
    // (unless a technique has frozen the yaw, in which case hold it)
    protected virtual void Update()
    {
        gait.Tick();
        body.SetHeight(gait.Height());
        if (yawHeld) { body.SetYaw(heldYaw); return; }
        lookAngle = Mathf.Lerp(lookAngle, lookAngleTarget, Time.deltaTime * 3);
        facing.LookAt(opponent, lookAngle);
    }

    //------------------Replay Functions------------------//

    public JudokaSnapshot Capture() => new JudokaSnapshot
    {
        body = body.GetPose(),
        leftLeg = Grab(leftLeg.Target),
        rightLeg = Grab(rightLeg.Target),
        leftArm = Grab(leftArm.Target),
        rightArm = Grab(rightArm.Target),
    };

    // Apply everything: the body first, then the limbs against the moved body
    public void Apply(JudokaSnapshot s)
    {
        ApplyBody(s);
        ApplyLimbs(s);
    }

    // The two halves, separable so a controller replaying two judokas can
    // move both bodies before placing any limb - a target parented to either
    // judoka must not be placed until both roots are standing in the frame
    public void ApplyBody(JudokaSnapshot s) => body.ApplyPose(s.body);

    public void ApplyLimbs(JudokaSnapshot s)
    {
        Place(leftLeg.Target, s.leftLeg);
        Place(rightLeg.Target, s.rightLeg);
        Place(leftArm.Target, s.leftArm);
        Place(rightArm.Target, s.rightArm);
    }

    // Null tolerant: limbs without a target yet (the arms) record as identity
    // and are skipped on apply - they join the replay the day they are wired
    private static LimbPose Grab(Transform t) => t == null ? default : new LimbPose { pos = t.position, rot = t.rotation };
    private static void Place(Transform t, LimbPose p) { if (t == null) return; t.position = p.pos; t.rotation = p.rot; }
}