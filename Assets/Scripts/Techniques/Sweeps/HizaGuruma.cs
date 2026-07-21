using UnityEngine;

/// <summary>
/// Hiza Guruma - the knee wheel.
/// Reaching: tori's blocking foot chases uke's knee. The moment is judged on
/// the block having something to work against: uke's leg loaded and driven down
/// by a flicked pull, with tori pulling him across.
/// Executing: the blocked leg is the axle and the pull is the rim - the wheel
/// turns uke over it while his balance spring fights back. Broken, he goes over
/// the block, and his free leg swings round the axle and up as he rotates down
/// to the mat. Held out, he keeps his feet and the attempt fails.
/// </summary>
public class HizaGuruma : Technique
{
    //------------------Variables------------------//
    [SerializeField] private float contactRadius = 0.3f; // sweeping foot to uke's leg distance that counts as contact

    // Pull - breaking uke's balance over the block
    [SerializeField] private float pullDuration = 0.8f;        // seconds tori has to break uke before he recovers
    [SerializeField] private float rampTime = 0.15f;           // seconds for the pull to reach full strength
    [SerializeField] private float pullStrength = 12f;         // lateral pull fed to uke's balance. It must out-pull his spring: he settles at pull * balancePullStrength / recoveryStiffness, and that has to clear breakFraction of his limit or he never goes over
    [SerializeField] private float forwardBias = 0.4f;         // a little forward pitch so uke wheels over rather than toppling flat sideways
    [SerializeField] private float breakFraction = 0.98f;      // fraction of uke's lateral limit that counts as broken
    [SerializeField] private bool invertPullDirection = false; // flip if uke is pulled the wrong way in play
    [SerializeField] private float pullThreshold = 0.3f;       // lateral pull tori must be holding for the wheel to have anything to turn uke over

    // Wheel down - he turns about tori's hand on the sweeping side
    [SerializeField] private float wheelAngle = 180f;    // degrees uke turns about the hand (180 brings him over onto his back)
    [SerializeField] private bool invertWheel = false;   // flip if he wheels the wrong way round

    // Swing - the free leg comes round the axle as uke goes down
    [SerializeField] private float swingArc = 140f;      // degrees the free foot travels round the blocked one
    [SerializeField] private float swingLift = 0.35f;    // metres the free foot rises at the top of its arc
    [SerializeField] private float swingDuration = 0.5f; // seconds the leg takes to come round
    [SerializeField] private bool invertSwing = false;   // flip if the leg swings the wrong way

    // Trackers
    private FootId side;       // tori's acting (blocking) foot
    private float breakSign;   // roll direction uke is driven toward
    private float elapsed;     // seconds into the pull
    private FootId caught;     // uke's blocked foot - the axle the wheel turns over
    private FootId support;    // uke's free foot - the one that swings round as he goes down
    private bool wheeling;     // he is broken and going down, the free leg coming round
    private float swingT;        // 0-1 through the swing
    private Vector3 swingOffset; // the free foot's start, held relative to the axle so the swing rides the body

    //------------------Public Functions------------------//
    /* BEGIN
     * 1 - Work out the geometry: tori's left foot blocks uke's right leg and
     *     mirrored (that is how the sweep targets are wired). The blocked leg is
     *     the axle, the other one is free to swing
     * 2 - Start the chase: the blocking foot goes Swept, the other one bases
     */
    public override void Begin(FootId footSide)
    {
        if (IsRunning) return;

        // 1
        side = footSide;
        caught = side == FootId.Left ? FootId.Right : FootId.Left;
        support = caught == FootId.Left ? FootId.Right : FootId.Left;
        wheeling = false;

        // 2
        ctx.toriFeet.Sweep(side);
        CurrentPhase = TechniquePhase.Reaching;
    }

    // One branch per phase, driven from Tori's Update
    public override void Tick(float dt)
    {
        switch (CurrentPhase)
        {
            case TechniquePhase.Reaching: Reach(); break;
            case TechniquePhase.Executing: if (wheeling) Swing(dt); else Wheel(dt); break;
        }
    }

    /* CHECK - the moment is judged on the flick and tori's pull
     * The wheel turns uke over a planted leg, and only while tori is actually
     * pulling him over it: the leg has to be loaded - driven down by flicking
     * the pull down - and there has to be a pull turning him, or nothing wheels.
     */
    public override bool Check() => ctx.uke.Lift < 0f
                                 && Mathf.Abs(ctx.uke.CurrentPull.x) >= pullThreshold;

    /* CANCEL
     * Only Reaching can be broken off - once the block is set the technique is
     * a commitment and plays out
     */
    public override void Cancel()
    {
        if (CurrentPhase != TechniquePhase.Reaching) return;
        ctx.toriFeet.Free();
        Fail();
    }

    //------------------Private Functions------------------//
    /* REACH
     * 1 - Wait for the blocking foot to arrive at uke's knee
     * 2 - Judge the moment: the wheel needs a loaded leg to turn over, so a
     *     foot off the floor fails the attempt
     * 3 - Seize the blocked foot as the axle, pin uke's free foot so he cannot
     *     step out of the technique, and commit
     */
    private void Reach()
    {
        // 1
        FootManager.Foot sweeper = ctx.toriFeet.Get(side);
        if (Vector3.Distance(sweeper.target.position, sweeper.sweepTarget.position) > contactRadius) return;

        // 2
        if (!Check())
        {
            ctx.toriFeet.Free();
            Fail();
            return;
        }

        // 3
        ctx.ukeFeet.Hold(caught);
        ctx.ukeFeet.Pin(support);
        elapsed = 0f;   // restart the window - without this a second attempt is already past pullDuration
        breakSign = Mathf.Sign(ctx.uke.CurrentPull.x) * (invertPullDirection ? -1f : 1f); // the pull tori is holding drives the wheel
        CurrentPhase = TechniquePhase.Executing;
    }

    /* WHEEL
     * 1 - Ramp the pull in and feed it to uke. Press leaves his balance spring
     *     running, so this fights his own resistance rather than overriding it
     * 2 - Broken: his lean has been driven to the limit - he goes over the block
     * 3 - Held out: the window closes with uke still on his feet - hand his
     *     balance back, free both sets of feet, and fail
     */
    private void Wheel(float dt)
    {
        // 1
        elapsed += dt;
        float ramp = Mathf.Clamp01(elapsed / rampTime);
        ctx.uke.Press(new Vector2(breakSign * pullStrength * ramp, forwardBias * ramp));

        // 2
        if (ctx.uke.PastLateralLimit(breakFraction))
        {
            Topple();
            return;
        }

        // 3
        if (elapsed >= pullDuration)
        {
            ctx.uke.Recover();
            ctx.ukeFeet.Free();
            ctx.toriFeet.Free();
            Fail();
        }
    }

    /* TOPPLE - he is over the block
     * He turns about tori's hand on the sweeping side, wheeling round it the way
     * the pull was driving him. Turning about a point up at the hand keeps the
     * arc above the mat, and the vault's landing blend sets him down flat.
     * The throw hands both of uke's feet to the body, so the free one is seized
     * straight back off it: the blocked leg rides the body down as the axle
     * while the free leg is driven round it.
     */
    private void Topple()
    {
        Vector3 axis = ctx.tori.transform.forward; // he wheels sideways over the block
        float angle = wheelAngle * breakSign * (invertWheel ? -1f : 1f);
        ctx.uke.HipThrow(ctx.tori.HandPoint(side), axis, angle, Vector3.zero);

        ctx.toriFeet.Free();

        ctx.ukeFeet.Hold(support);
        swingOffset = ctx.ukeFeet.Get(support).target.position - ctx.ukeFeet.Get(caught).target.position;
        swingT = 0f;
        wheeling = true;
    }

    /* SWING
     * 1 - Keep handing the fulcrum back: tori's hand is still moving, and uke
     *     turns about wherever it is now
     * 2 - Carry the free foot round the axle, rising through the middle of the
     *     arc so the leg comes up and over rather than scuffing the mat. The
     *     axle is read live, so the swing rides the body as it turns over
     * 3 - Landed: hand both legs back to the fallen body and score
     */
    private void Swing(float dt)
    {
        // 1
        ctx.uke.SetVaultPivot(ctx.tori.HandPoint(side));

        // 2
        swingT = Mathf.Min(swingT + dt / swingDuration, 1f);
        float ease = swingT * swingT * (3f - 2f * swingT);

        float arc = swingArc * breakSign * (invertSwing ? -1f : 1f);
        Quaternion turn = Quaternion.AngleAxis(arc * ease, Vector3.up);
        Vector3 axle = ctx.ukeFeet.Get(caught).target.position;
        Vector3 pos = axle + turn * swingOffset;
        pos.y += swingLift * Mathf.Sin(Mathf.PI * ease);
        ctx.ukeFeet.PlaceHeld(support, pos);

        // 3
        if (ctx.uke.State == UkeState.Fallen)
        {
            ctx.ukeFeet.Limp(); // both legs ride the body again, as they do in any fall
            Score();
        }
    }
}
