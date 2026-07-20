using UnityEngine;

public class HizaGuruma : Technique
{
    //------------------Variables------------------//
    [SerializeField] private float contactRadius = 0.3f; // sweeping foot to uke's leg distance that counts as contact

    // Pull - breaking uke's balance over the block
    [SerializeField] private float pullDuration = 0.8f;        // seconds tori has to break uke before he recovers
    [SerializeField] private float rampTime = 0.15f;           // seconds for the pull to reach full strength
    [SerializeField] private float pullStrength = 2.5f;        // lateral pull fed to uke's balance (a full stick pull is 1) - the main tuning knob against uke's balance stats
    [SerializeField] private float forwardBias = 0.4f;         // a little forward pitch so uke wheels over rather than toppling flat sideways
    [SerializeField] private float breakFraction = 0.98f;      // fraction of uke's lateral limit that counts as broken
    [SerializeField] private bool invertPullDirection = false; // flip if uke is pulled the wrong way in play
    [SerializeField] private float pullThreshold = 0.3f;       // lateral pull tori must be holding for the wheel to have anything to turn uke over
    // Trackers
    private FootId side;      // tori's acting (blocking) foot
    private float sweptSign;  // -1 uke's left foot is the axle, +1 his right (mirrors De Ashi Barai)
    private float breakSign;  // roll direction uke is driven toward
    private float elapsed;    // seconds into the pull
    private FootId caught;     // uke's swept foot - the mirror of side, as wired in the scene
    private FootId support;    // uke's other foot, the destination of the carry
    private float sweepT;      // 0-1 through the carry
    private Vector3 sweepFrom; // where the caught foot was seized
    private Vector3 sweepTo;   // where the support foot stood when the carry began - a fixed anchor, so knocking the support foot later doesn't drag the carry off course
    private bool knocked;      // the carried foot has already struck the support foot this sweep
    private float knockT;      // 0-1 through the knock
    private Vector3 knockFrom; // support foot's position the instant it was struck
    private Vector3 knockTo;   // where the strike sends it

    //------------------Public Functions------------------//
    /* BEGIN
     * 1 - Work out the geometry: tori's left foot chases uke's right leg and
     *     mirrored (that is how the sweep targets are wired), the support is
     *     uke's other foot, and the sign is the side uke will fall toward
     * 2 - Start the chase: the sweeping foot goes Swept, the other one bases
     */
    public override void Begin(FootId footSide)
    {
        if (IsRunning) return;

        // 1
        side = footSide;
        caught = side == FootId.Left ? FootId.Right : FootId.Left;
        support = caught == FootId.Left ? FootId.Right : FootId.Left;
        sweptSign = caught == FootId.Left ? -1f : 1f;

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
            case TechniquePhase.Executing: Wheel(dt); break;
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
     * Only Reaching can be broken off - once the foot is caught the
     * technique is a commitment and plays out
     */
    public override void Cancel()
    {
        if (CurrentPhase != TechniquePhase.Reaching) return;
        ctx.toriFeet.Free();
        Fail();
    }

    //------------------Private Functions------------------//
    /* REACH
     * 1 - Wait for the sweeping foot to arrive at uke's leg
     * 2 - Judge the moment: the wheel needs a planted leg to turn over, so a
     *     foot off the floor fails the attempt
     * 3 - Seize the blocked foot, pin uke's support so he cannot step out of
     *     the technique, and commit
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
        sweepFrom = ctx.ukeFeet.Get(caught).target.position;
        sweepTo = ctx.ukeFeet.Get(support).target.position;
        sweepT = 0f;
        knocked = false;
        elapsed = 0f;   // restart the window - without this a second attempt is already past pullDuration
        breakSign = Mathf.Sign(ctx.uke.CurrentPull.x) * (invertPullDirection ? -1f : 1f); // the pull tori is holding drives the wheel
        CurrentPhase = TechniquePhase.Executing;
    }
    //------------------Private Functions------------------//
    /* WHEEL
     * 1 - Ramp the pull in and feed it to uke. Press leaves his balance spring
     *     running, so this fights his own resistance rather than overriding it
     * 2 - Broken: his lean has been driven to the limit - he goes over the
     *     block. Fall continues in the same direction the pull was driving,
     *     tori's feet are freed, and the technique scores
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
            ctx.uke.Fall(-breakSign);
            ctx.toriFeet.Free();
            Score();
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
}

