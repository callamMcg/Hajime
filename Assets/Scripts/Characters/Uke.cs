using UnityEngine;

/// <summary>
/// The defender. Fights back with the balance spring and the step reflex
/// until a technique takes over: Tip hands his lean to the technique,
/// Recover gives it back to the spring, and Fall is the point of no return -
/// a scripted trip from tipped to flat on his back, with his feet riding the
/// body down. The Thrown event fires the moment he lands, for the win flow.
/// </summary>

[RequireComponent(typeof(StepReflex), typeof(FootManager))]
public class Uke : Judoka
{
    //------------------Variables------------------//
    // Components
    private StepReflex stepReflex;
    private FootManager feet;

    // Fall - the scripted trip from standing to flat on the back
    [SerializeField] private float fallDuration = 0.7f; // seconds from tipped to flat
    [SerializeField] private float fallPitch = -10f;    // final pitch, negative is onto the back - keep small for a side fall
    [SerializeField] private float fallRoll = 90f;       // final roll toward the swept side - 90 lays him flat on his side
    [SerializeField] private float fallenHeight = 0.3f; // root height once flat - match the rig
    [SerializeField] private float fallDrift = 0.5f;    // metres the body slides backward through the fall
    
    // Vault - the rigid turn over tori's hip (harai goshi and friends)
    [SerializeField] private float vaultDuration = 0.9f;                  // seconds from load to flat
    [SerializeField, Range(0f, 1f)] private float vaultLandBlend = 0.85f; // fraction after which height eases to the mat

    // Flick - a sharp pull up or down pulses uke's height
    [SerializeField] private float flickThreshold = 0.7f; // pull.y magnitude that counts as a flick
    
    // Trackers
    private UkeState state = UkeState.Fighting;
    private float startingRadius;
    private bool holdGround = true; // radius hold: maintain the starting distance from tori
    private Vector3 sweptLean;      // the lean a technique is commanding this frame
    private Vector2 currentPull;    // tori's live pull, recorded through every state - a throw in flight still reads it
    private bool flickArmed = true; // the pull must fall back inside the threshold before another flick counts
    private float fallSide;         // -1 the left foot was swept, +1 the right
    private float fallT;            // 0-1 through the fall
    private Vector3 fallFromLean;   // the pose the fall starts from
    private float fallFromHeight;
    private Vector2 fallFromPlanar;
    private Vector3 vaultPivot;
    private Vector3 vaultAxis;
    private float vaultAngle;
    private Vector3 vaultDrift;      // forward (tori-relative) translation blended in over the vault, so uke ends in front of tori
    private Vector3 vaultStartOffset;
    private Quaternion vaultStartRot;
    private float vaultT;
    // The moment the fall completes - the win flow listens for this
    public event System.Action Thrown;

    //------------------Unity Functions------------------//
    protected override void Awake()
    {
        base.Awake();
        stepReflex = GetComponent<StepReflex>();
        feet = GetComponent<FootManager>();
    }

    protected override void Start()
    {
        base.Start();
        startingRadius = Vector3.Distance(transform.position, opponent.transform.position);
    }

    /* UPDATE - one branch per state
     * Fighting - the live judoka: clock, spring, reflex stepping
     * Swept    - a technique owns the lean; the spring is bypassed
     * Falling  - play the scripted fall
     * Fallen   - do nothing: the body holds the last written pose
     */
    protected override void Update()
    {
        switch (state)
        {
            case UkeState.Fighting: Fight(); break;
            case UkeState.Swept: base.Update(); body.SetLean(sweptLean); break;
            case UkeState.Pressed: PressStep(); break;
            case UkeState.Falling: FallStep(); break;
            case UkeState.Vaulting: VaultStep(); break;
            case UkeState.Fallen: break;
        }
    }

    //------------------Public Functions------------------//
    /* PULL - tori's pull
     * The raw pull is always recorded, so it carries on through a technique -
     * a throw turning uke over the hip can still read what tori is pulling.
     * Only the live judoka's spring integrates it: once a technique owns uke
     * its own pull drives the spring, not the player's raw input.
     */
    public void Pull(Vector2 pull)
    {
        currentPull = pull;
        Flick(pull.y);
        if (state != UkeState.Fighting) return;
        Vector2 p = Vector2.right * pull.x;
        p += Vector2.up * Mathf.Abs(pull.x) / 4;
        balance.SetBalance(p);
    }

    /* TIP - technique control
     * A technique takes the lean: the spring stops arguing and the commanded
     * lean is written instead, until Recover or Fall
     */
    public void Tip(Vector3 lean)
    {
        if (state == UkeState.Falling || state == UkeState.Fallen || state == UkeState.Vaulting) return;
        state = UkeState.Swept;
        sweptLean = lean;
    }

    /* RECOVER
     * The technique lets go before the point of no return: the spring adopts
     * the tipped pose as its own state and fights back from there, so the
     * stumble and the recovery come out of the same physics as everything else
     */
    public void Recover()
    {
        if (state == UkeState.Swept)
        {
            balance.AdoptLean(sweptLean);
            state = UkeState.Fighting;
        }
        else if (state == UkeState.Pressed)
        {
            // the spring already holds uke's real lean - just hand control back
            state = UkeState.Fighting;
        }
    }

    /* PRESS - technique control
     * The technique pulls on uke while his feet are pinned. Unlike Tip, the
     * balance spring is left running, so uke resists and his own lean builds
     * against the pull; the technique watches for it to pass the limit.
     */
    public void Press(Vector2 pull)
    {
        if (state == UkeState.Falling || state == UkeState.Fallen || state == UkeState.Vaulting) return;
        state = UkeState.Pressed;
        balance.SetBalance(pull);
    }

    /* FALL - the point of no return
     * Capture where the body is, hand both feet to the fall so they ride the
     * rotating root, and let Update play it out.
     * side: -1 the left foot was swept, +1 the right
     */
    public void Fall(float side)
    {
        if (state == UkeState.Falling || state == UkeState.Fallen || state == UkeState.Vaulting) return;

        BodyPose pose = body.GetPose();
        fallFromLean = pose.lean;
        fallFromHeight = pose.height;
        fallFromPlanar = pose.planar;

        fallSide = side;
        fallT = 0f;
        feet.Limp();
        state = UkeState.Falling;
    }
    // A closing technique turns off the range hold so tori can load the hip;
    // it is handed back when the attempt ends
    public void HoldGround(bool on) => holdGround = on;

    //------------------Private Functions------------------//
    /* FLICK - a sharp pull up or down
     * The first frame the pull crosses the threshold fires a one shot pulse
     * that hauls uke up or drives him down. It cannot fire again until the pull
     * has fallen back inside the threshold, so holding the stick over does
     * nothing - only the flick itself counts.
     */
    private void Flick(float y)
    {
        if (Mathf.Abs(y) < flickThreshold)
        {
            flickArmed = true;
            return;
        }
        if (!flickArmed) return;
        flickArmed = false;
        gait.Pulse(Mathf.Sign(y));
    }

    /* FIGHT - the live judoka, exactly as before the technique layer
     * 1 - Clock, height and facing from the base
     * 2 - Integrate the balance spring
     * 3 - Step toward safety, held at the starting distance from tori
     */
private void Fight()
    {
        // 1
        base.Update();
        // 2
        balance.Step();
        // 3 - hold the starting distance from tori, unless a technique has released it
        if (!holdGround) return;
        Vector2 pivot = new Vector2(opponent.transform.position.x, opponent.transform.position.z);
        Vector2 stepped = body.Planar() + stepReflex.GetStep() * Time.deltaTime;
        Vector2 dir = (stepped - pivot).normalized;
        body.SetPlanar(pivot + dir * startingRadius);
    }

    /* PRESS STEP - the pulled judoka, resisting on pinned feet
     * The spring integrates the technique's pull, but the step reflex and
     * the radius hold are dropped: uke cannot step out from under the pull,
     * he can only resist it with the spring until it breaks him or spends
     * itself and he is let go.
     */
    private void PressStep()
    {
        base.Update();   // clock, height, keep facing tori
        balance.Step();  // integrate the pull against the spring
    }

    /* FALL STEP
     * 1 - Advance and ease the fall timer
     * 2 - Rotate onto the back and toward the swept side, sinking to the mat
     * 3 - Drift backward along the facing, the way a swept man travels
     * 4 - On landing, hold the pose and announce the throw
     */
    private void FallStep()
    {
        // 1
        fallT = Mathf.Min(fallT + Time.deltaTime / fallDuration, 1f);
        float ease = fallT * fallT * (3f - 2f * fallT);

        // 2
        Vector3 finalLean = new Vector3(fallPitch, 0f, -fallSide * fallRoll);
        body.SetLean(Vector3.Lerp(fallFromLean, finalLean, ease));
        body.SetHeight(Mathf.Lerp(fallFromHeight, fallenHeight, ease));

        // 3
        float rad = body.GetPose().yaw * Mathf.Deg2Rad;
        Vector2 back = -new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        body.SetPlanar(fallFromPlanar + back * (fallDrift * ease));

        // 4
        if (fallT >= 1f)
        {
            state = UkeState.Fallen;
            Thrown?.Invoke();
        }
    }
    /* HIP THROW - the point of no return for a hip throw
     * Capture where uke stands relative to the pivot (tori's loading hip) and
     * his current orientation, hand his feet to the fall so the legs ride the
     * body over, and let Update turn him a full arc about tori's right axis.
     * The drift carries him forward, out in front of tori, as he goes over.
     */
    public void HipThrow(Vector3 pivot, Vector3 axis, float angle, Vector3 drift)
    {
        if (state == UkeState.Falling || state == UkeState.Fallen || state == UkeState.Vaulting) return;

        vaultPivot = pivot;
        vaultAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.right;
        vaultAngle = angle;
        vaultDrift = drift;
        vaultStartOffset = body.WorldPosition() - pivot;
        vaultStartRot = transform.rotation;
        vaultT = 0f;

        feet.Limp();
        state = UkeState.Vaulting;
    }

    /* SET VAULT PIVOT - move the fulcrum while the vault is in flight
     * A sacrifice throw turns uke over a point that is itself dropping to the
     * mat (tori's shoulders), so uke is pulled down and round with it rather
     * than orbiting a fixed spot.
     */
    public void SetVaultPivot(Vector3 pivot)
    {
        if (state != UkeState.Vaulting) return;
        vaultPivot = pivot;
    }

    /* VAULT STEP
     * 1 - Advance and ease the vault timer
     * 2 - Rotate uke rigidly about the pivot: his position orbits the hip and
     *     his whole orientation turns by the same angle, drifting forward over
     *     the throw so he ends in front of tori
     * 3 - Over the last stretch, ease his height onto the mat so he lands flat
     *     regardless of the arc's radius
     * 4 - Write the pose, and on landing hold it and announce the throw
     */
    private void VaultStep()
    {
        // 1
        float dt = Time.deltaTime;
        vaultT = Mathf.Min(vaultT + dt / vaultDuration, 1f);
        float ease = vaultT * vaultT * (3f - 2f * vaultT);

        // 2
        Quaternion turn = Quaternion.AngleAxis(vaultAngle * ease, vaultAxis);
        Vector3 pos = vaultPivot + turn * vaultStartOffset;
        pos += vaultDrift * ease;   // carry him forward, out in front of tori
        Quaternion orient = turn * vaultStartRot;

        // 3
        if (vaultT > vaultLandBlend)
        {
            float b = Mathf.InverseLerp(vaultLandBlend, 1f, vaultT);
            b = b * b * (3f - 2f * b);
            pos.y = Mathf.Lerp(pos.y, fallenHeight, b);
        }

        // 4
        body.SetPlanar(new Vector2(pos.x, pos.z));
        body.SetHeight(pos.y);
        body.SetWorldRotation(orient);

        

        if (vaultT >= 1f)
        {
            state = UkeState.Fallen;
            Thrown?.Invoke();
        }
    }
    //------------------Getters------------------//
    public UkeState State => state;

    // Tori's live pull, still updated while a technique owns uke - a throw in
    // flight reads this to know what tori is still pulling through it
    public Vector2 CurrentPull => currentPull;

    // +1 while a flicked pull has him hauled up, -1 while it has him driven
    // down, 0 otherwise - the opening a technique is judged against
    public float Lift => gait.PulseDirection;

    // How much he is in motion, 0-1 - a sacrifice throw judges his momentum by this
    public float Speed => gait.SpeedFactor;

    // Degrees uke has turned through the vault so far (0 at load, |angle| at flat)
    // - a hip throw reads this to know when uke has gone far enough over to release
    public float VaultTurned
    {
        get
        {
            float ease = vaultT * vaultT * (3f - 2f * vaultT);
            return Mathf.Abs(vaultAngle) * ease;
        }
    }

    // Current balance lean (pitch x, roll z) - the accumulated result of
    // tori's pull, read by a technique to weigh the pull's direction
    public Vector3 Lean => balance.GetLean();

    // True once the lateral lean has been driven to (near) its limit - the
    // point a pulling technique considers uke broken
    public bool PastLateralLimit(float fraction = 1f) => Mathf.Abs(balance.NormalisedLean().z) >= fraction;

    /* PULL OPPOSES SWEEP
 * The sweep topples uke toward a roll of sign -sweptSign (see the Tip
 * call, -sweptSign * tipRoll). A de ashi barai wants the pull leaning
 * uke that same way; a pull leaning him the other way makes it a hiza
 * guruma. A pull weaker than the tolerance has no clear side, so it is
 * not counted as opposition and the foot sweep goes ahead.
 */
    public bool PullOpposesSweep(float sweep, float pull)
    {
        if (sweep * pull < 0)
            return true;
        return false;
    }

    public float GetHeight => gait.Height();
}