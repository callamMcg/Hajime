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
    protected virtual void Update()
    {
        gait.Tick();
        body.SetHeight(gait.Height());
        facing.LookAt(opponent);
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

    public void Apply(JudokaSnapshot s)
    {
        body.ApplyPose(s.body);
        Place(leftLeg.Target, s.leftLeg);
        Place(rightLeg.Target, s.rightLeg);
        Place(leftArm.Target, s.leftArm);
        Place(rightArm.Target, s.rightArm);
    }
    private static LimbPose Grab(Transform t) => new LimbPose { pos = t.position, rot = t.rotation };
    private static void Place(Transform t, LimbPose p) { t.position = p.pos; t.rotation = p.rot; }
}