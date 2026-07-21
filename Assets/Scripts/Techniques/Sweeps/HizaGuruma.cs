using UnityEngine;

/// <summary>
/// Hiza Guruma - the knee wheel.
/// Tori's foot goes to uke's knee and blocks it. The moment it lands the timing
/// is judged: the block only has something to work against if uke's leg is
/// loaded - driven down by a flicked pull - and tori is actually hauling him
/// across at the same time.
/// From there the blocked leg is the axle and the pull is the rim. Tori keeps
/// pulling while uke's own balance fights back, and if the pull wins he goes
/// over the block: he turns about tori's near hand while his free leg swings
/// round the axle and up. If uke holds out until the window closes, he keeps
/// his feet and the attempt is wasted.
/// </summary>
public class HizaGuruma : Technique
{
    //----------Variables----------\\

    // How close the blocking foot has to get to uke's knee to count as contact
    [SerializeField] private float contactRadius = 0.3f;

    // Seconds tori has to break uke before he recovers his balance
    [SerializeField] private float pullDuration = 0.8f;

    // Seconds the pull takes to build up to its full strength
    [SerializeField] private float rampTime = 0.15f;

    // How hard tori hauls on uke. This has to out-pull uke's own balance spring:
    // he settles at pull * his pullStrength / his recoveryStiffness, and that has
    // to clear breakFraction of his limit or he simply never goes over
    [SerializeField] private float pullStrength = 12f;

    // A little forward lean fed in as well, so he wheels over rather than
    // toppling flat out sideways
    [SerializeField] private float forwardBias = 0.4f;

    // How far uke has to be leaned, as a fraction of his limit, to count as broken
    [SerializeField] private float breakFraction = 0.98f;

    // Flip this if uke gets hauled the wrong way in play
    [SerializeField] private bool invertPullDirection = false;

    // How hard tori must be pulling sideways for the wheel to have anything to
    // turn uke over with
    [SerializeField] private float pullThreshold = 0.3f;

    // Degrees uke turns about the hand as he comes over - 180 brings him onto his back
    [SerializeField] private float wheelAngle = 180f;

    // Flip this if he wheels round the wrong way
    [SerializeField] private bool invertWheel = false;

    // Degrees the free foot travels round the blocked one
    [SerializeField] private float swingArc = 140f;

    // How high the free foot rises at the top of its arc
    [SerializeField] private float swingLift = 0.35f;

    // Seconds the free leg takes to come round
    [SerializeField] private float swingDuration = 0.5f;

    // Flip this if the free leg swings the wrong way
    [SerializeField] private bool invertSwing = false;

    // Tori's blocking foot
    private FootId side;

    // Which way round uke is being driven
    private float breakSign;

    // Seconds spent pulling so far
    private float elapsed;

    // Uke's blocked foot, the axle the wheel turns over
    private FootId caught;

    // Uke's free foot, the one that swings round as he goes down
    private FootId support;

    // True once he is broken and going down, with the free leg coming round
    private bool wheeling;

    // How far through the leg swing we are, from 0 to 1
    private float swingT;

    // Where the free foot started, kept relative to the axle so the swing rides
    // the body instead of being pinned to a spot on the mat
    private Vector3 swingOffset;

    //----------Public Functions----------\\

    /* BEGIN
     * 1 - Work out the geometry. Tori's left foot blocks uke's right leg and
     *     mirrored, which is how the targets are wired in the scene. The blocked
     *     leg becomes the axle and the other one is free to swing
     * 2 - Send the blocking foot chasing uke's knee, and let tori's other foot
     *     take his weight while it does
     */
    public override void Begin(FootId footSide)
    {
        if (IsRunning) return;

        side = footSide;
        caught = side == FootId.Left ? FootId.Right : FootId.Left;
        support = caught == FootId.Left ? FootId.Right : FootId.Left;
        wheeling = false;

        ctx.toriFeet.Sweep(side);
        CurrentPhase = TechniquePhase.Reaching;
    }

    /* TICK
     * 1 - While reaching, watch for the block to land on the knee
     * 2 - Once committed, either keep hauling on him or, if he is already going
     *     over, keep driving his free leg round
     */
    public override void Tick(float dt)
    {
        switch (CurrentPhase)
        {
            case TechniquePhase.Reaching: Reach(); break;
            case TechniquePhase.Executing: if (wheeling) Swing(dt); else Wheel(dt); break;
        }
    }

    /* CHECK
     * The wheel needs two things at once. The leg has to be loaded, driven down
     * by a flicked pull, or the block has nothing to work against. And tori has
     * to actually be hauling him sideways, or there is nothing turning him over.
     */
    public override bool Check() => ctx.uke.Lift < 0f
                                 && Mathf.Abs(ctx.uke.CurrentPull.x) >= pullThreshold;

    /* CANCEL
     * 1 - Ignore this once the block is set, as by then the technique is
     *     committed and plays itself out
     * 2 - Otherwise give tori his feet back and report the attempt as a miss
     */
    public override void Cancel()
    {
        if (CurrentPhase != TechniquePhase.Reaching) return;

        ctx.toriFeet.Free();
        Fail();
    }

    //----------Private Functions----------\\

    /* REACH
     * 1 - Wait until the blocking foot has arrived at uke's knee
     * 2 - Judge that moment, and give up if the opening is not there
     * 3 - Seize the blocked foot as the axle and pin the free one, so uke
     *     cannot simply step out of the technique
     * 4 - Restart the pull window and take the direction to haul him from the
     *     way tori is pulling right now
     */
    private void Reach()
    {
        FootManager.Foot sweeper = ctx.toriFeet.Get(side);
        if (Vector3.Distance(sweeper.target.position, sweeper.sweepTarget.position) > contactRadius) return;

        if (!Check())
        {
            ctx.toriFeet.Free();
            Fail();
            return;
        }

        ctx.ukeFeet.Hold(caught);
        ctx.ukeFeet.Pin(support);

        // Without resetting this a second attempt would already be out of time
        elapsed = 0f;
        breakSign = Mathf.Sign(ctx.uke.CurrentPull.x) * (invertPullDirection ? -1f : 1f);
        CurrentPhase = TechniquePhase.Executing;
    }

    /* WHEEL
     * 1 - Build the pull up and feed it to uke. His balance spring is left
     *     running, so this is a tug of war against his own resistance rather
     *     than simply overriding him
     * 2 - If his lean gets driven far enough he is broken, and goes over the block
     * 3 - If the window runs out first he keeps his feet, so hand his balance
     *     back, release everyone's feet and report the miss
     */
    private void Wheel(float dt)
    {
        elapsed += dt;
        float ramp = Mathf.Clamp01(elapsed / rampTime);
        ctx.uke.Press(new Vector2(breakSign * pullStrength * ramp, forwardBias * ramp));

        if (ctx.uke.PastLateralLimit(breakFraction))
        {
            Topple();
            return;
        }

        if (elapsed >= pullDuration)
        {
            ctx.uke.Recover();
            ctx.ukeFeet.Free();
            ctx.toriFeet.Free();
            Fail();
        }
    }

    /* TOPPLE
     * 1 - Turn him about tori's hand on the blocking side, wheeling him the way
     *     the pull was already driving him. Turning about a point up at the hand
     *     keeps the arc above the mat, and the landing settles him flat
     * 2 - The throw just handed both of uke's feet to his body, so take the free
     *     one straight back off it. The blocked leg rides the body down as the
     *     axle while the free leg gets driven round it
     * 3 - Remember where the free foot sits relative to the axle, so the swing
     *     follows the body rather than a fixed spot on the mat
     * Tori deliberately keeps hold of his own feet here. The blocking foot stays
     * on uke's knee the whole way down and only comes back to the floor once he
     * has actually landed
     */
    private void Topple()
    {
        Vector3 axis = ctx.tori.transform.forward;
        float angle = wheelAngle * breakSign * (invertWheel ? -1f : 1f);
        ctx.uke.HipThrow(ctx.tori.HandPoint(side), axis, angle, Vector3.zero);

        ctx.ukeFeet.Hold(support);
        swingOffset = ctx.ukeFeet.Get(support).target.position - ctx.ukeFeet.Get(caught).target.position;
        swingT = 0f;
        wheeling = true;
    }

    /* SWING
     * 1 - Tori's hand is still moving, so keep telling uke to turn about
     *     wherever it has got to
     * 2 - Carry the free foot round the axle, lifting it through the middle of
     *     the arc so the leg comes up and over rather than scuffing the mat.
     *     The axle is read fresh each frame so the swing rides the body as it
     *     turns over
     * 3 - Once he has landed, hand both of his legs back to the fallen body,
     *     give tori his feet back so the blocking one finally comes down, and
     *     call the throw landed
     */
    private void Swing(float dt)
    {
        ctx.uke.SetVaultPivot(ctx.tori.HandPoint(side));

        swingT = Mathf.Min(swingT + dt / swingDuration, 1f);
        float ease = swingT * swingT * (3f - 2f * swingT);

        float arc = swingArc * breakSign * (invertSwing ? -1f : 1f);
        Quaternion turn = Quaternion.AngleAxis(arc * ease, Vector3.up);
        Vector3 axle = ctx.ukeFeet.Get(caught).target.position;
        Vector3 pos = axle + turn * swingOffset;
        pos.y += swingLift * Mathf.Sin(Mathf.PI * ease);
        ctx.ukeFeet.PlaceHeld(support, pos);

        if (ctx.uke.State == UkeState.Fallen)
        {
            ctx.ukeFeet.Limp();
            ctx.toriFeet.Free();
            Score();
        }
    }
}
