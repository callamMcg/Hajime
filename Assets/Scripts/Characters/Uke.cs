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

    // Trackers
    private UkeState state = UkeState.Fighting;
    private float startingRadius;
    private Vector3 sweptLean;      // the lean a technique is commanding this frame
    private float fallSide;         // -1 the left foot was swept, +1 the right
    private float fallT;            // 0-1 through the fall
    private Vector3 fallFromLean;   // the pose the fall starts from
    private float fallFromHeight;
    private Vector2 fallFromPlanar;

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
            case UkeState.Falling: FallStep(); break;
            case UkeState.Fallen: break;
        }
    }

    //------------------Public Functions------------------//
    // Tori's pull lands as a lean input (only integrated while Fighting)
    public void Pull(Vector2 pull)
    {
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
        if (state == UkeState.Falling || state == UkeState.Fallen) return;
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
        if (state != UkeState.Swept) return;
        balance.AdoptLean(sweptLean);
        state = UkeState.Fighting;
    }

    /* FALL - the point of no return
     * Capture where the body is, hand both feet to the fall so they ride the
     * rotating root, and let Update play it out.
     * side: -1 the left foot was swept, +1 the right
     */
    public void Fall(float side)
    {
        if (state == UkeState.Falling || state == UkeState.Fallen) return;

        BodyPose pose = body.GetPose();
        fallFromLean = pose.lean;
        fallFromHeight = pose.height;
        fallFromPlanar = pose.planar;

        fallSide = side;
        fallT = 0f;
        feet.Limp();
        state = UkeState.Falling;
    }

    //------------------Private Functions------------------//
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
        // 3
        Vector2 pivot = new Vector2(opponent.transform.position.x, opponent.transform.position.z);
        Vector2 stepped = body.Planar() + stepReflex.GetStep() * Time.deltaTime;
        Vector2 dir = (stepped - pivot).normalized;
        body.SetPlanar(pivot + dir * startingRadius);
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

    //------------------Getters------------------//
    public UkeState State => state;
}