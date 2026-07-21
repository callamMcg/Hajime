using UnityEngine;

/// <summary>
/// Harai Goshi - the sweeping hip throw.
/// Tori turns his back in across uke and closes the gap so his hip can get
/// under him. While he turns, his reaping foot chases uke's shin and his other
/// foot steps in to plant just outside uke's attacked leg, which becomes the
/// post the whole throw turns over.
/// Once the turn has come round, the reaping foot has found the shin and the
/// post is planted, the moment is judged on the opening tori read when he first
/// pressed. If it was there he commits: the post pins, his facing freezes so it
/// stops chasing uke around, and uke is vaulted over the loading hip.
/// Through the throw tori bows forward while his reaping leg stays attached to
/// uke. Once uke has turned far enough over, tori lets go, settles his feet back
/// underneath himself and eases upright, while uke's vault finishes to the mat
/// under its own steam.
/// </summary>
public class HaraiGoshi : Technique
{
    //----------Variables----------\\

    // Degrees tori turns his back in, with the sign coming from the throw side
    [SerializeField] private float turnAngle = 160f;

    // How near the full turn counts as having come round
    [SerializeField] private float turnTolerance = 12f;

    // How close the reaping foot has to get to uke's shin to count as contact
    [SerializeField] private float contactRadius = 0.3f;

    // The gap tori closes to so his hip can load under uke
    [SerializeField] private float closeRadius = 0.6f;

    // The gap restored once the throw ends or is broken off
    [SerializeField] private float standRadius = 1.2f;

    // How far outside uke's attacked leg the post plants
    [SerializeField] private float outsideOffset = 0.15f;

    // How far along uke's facing the post plants, so it lands beside him
    // rather than square on his toes
    [SerializeField] private float forwardOffset = 0.15f;

    // How far forward tori bows as he drives the throw down
    [SerializeField] private float throwLean = 15f;

    // How quickly he bows into it
    [SerializeField] private float leanInRate = 6f;

    // How quickly he eases back upright afterwards
    [SerializeField] private float standUpRate = 4f;

    // How far uke has to have turned over the hip before tori lets go of him
    [SerializeField] private float releaseAngle = 130f;

    // How quickly tori's frozen facing swings back to square as he stands
    [SerializeField] private float standYawRate = 4f;

    // Degrees uke turns over the hip - 270 settles him flat
    [SerializeField] private float throwAngle = 270f;

    // Flip this if uke goes the wrong way over the hip
    [SerializeField] private bool invertRotation = false;

    // How far in front of tori uke is carried as he comes down
    [SerializeField] private float landForward = 1f;

    // How far forward tori is bowed right now
    private float leanNow;

    // True once tori has let go and started standing up
    private bool squaredUp;

    // The facing held through the throw, eased back to square as he stands
    private float throwYaw;

    // Tori's posting foot, on the throw side
    private FootId plant;

    // Tori's reaping foot, the one that chases uke's shin
    private FootId reap;

    // +1 for a throw to the right, -1 to the left
    private float sign;

    // The opening tori read at the moment he pressed. The turn-in lasts longer
    // than the opening does, so it has to be judged from what he saw then
    private float liftAtPress;

    //----------Public Functions----------\\

    /* BEGIN
     * 1 - Work out the geometry: the throw side is the posting foot, the other
     *     one does the reaping
     * 2 - Turn tori's back in and start closing the gap so his hip can load
     * 3 - Send the reaping foot after uke's shin and step the post in beside
     *     uke's attacked leg
     * 4 - Read the opening now, before the long turn-in has a chance to outlast it
     */
    public override void Begin(FootId side)
    {
        if (IsRunning) return;

        plant = side;
        reap = side == FootId.Left ? FootId.Right : FootId.Left;
        sign = side == FootId.Right ? 1f : -1f;

        ctx.tori.SetLook(sign * turnAngle);
        ctx.tori.SetDistance(closeRadius);
        ctx.tori.SetRadiusRecovery(true);
        ctx.uke.HoldGround(false);

        ctx.toriFeet.Reap(reap);
        ctx.toriFeet.StepTo(plant, PivotSpot());

        leanNow = 0f;
        squaredUp = false;
        liftAtPress = ctx.uke.Lift;
        CurrentPhase = TechniquePhase.Reaching;
    }

    /* TICK
     * 1 - While reaching, keep turning in and watch for everything to line up
     * 2 - Once committed, drive the throw and then get back upright
     */
    public override void Tick(float dt)
    {
        switch (CurrentPhase)
        {
            case TechniquePhase.Reaching: Reach(); break;
            case TechniquePhase.Executing: Throw(); break;
        }
    }

    /* CHECK
     * A leg still carrying uke's weight cannot be reaped up and the hip has
     * nothing to throw. Only a leg tori had lifted - by flicking the pull up as
     * he pressed - goes over, so the throw fails unless that opening was there
     * at the moment he committed.
     */
    public override bool Check() => liftAtPress > 0f;

    /* CANCEL
     * 1 - Ignore this once uke is already being vaulted, as the throw is
     *     committed by then and plays itself out
     * 2 - Otherwise put everything back the way it was and report the miss
     */
    public override void Cancel()
    {
        if (CurrentPhase != TechniquePhase.Reaching) return;

        Reset();
        Fail();
    }

    //----------Private Functions----------\\

    /* REACH
     * 1 - Keep the post's step aimed just outside uke's attacked leg while it
     *     travels
     * 2 - Wait for all three things at once: the turn come round, the reaping
     *     foot at the shin, and the post actually landed
     * 3 - Judge the moment, and put everything back if the opening was not there
     * 4 - Commit: pin the post, freeze tori's facing so it stops chasing uke,
     *     and hold him in place as the fixed point the vault turns over
     * 5 - Send uke over the hip, carried forward so he lands in front of tori.
     *     The reaping foot is deliberately left attached, so tori's leg rides
     *     him over
     */
    private void Reach()
    {
        ctx.toriFeet.SetPlaceTarget(plant, PivotSpot());

        FootManager.Foot reaper = ctx.toriFeet.Get(reap);
        bool turned = Mathf.Abs(Mathf.DeltaAngle(ctx.tori.LookAngle, sign * turnAngle)) <= turnTolerance;
        bool atShin = Vector3.Distance(reaper.target.position, reaper.throwTarget.position) <= contactRadius;
        bool planted = ctx.toriFeet.Get(plant).state == FootState.Based;
        if (!turned || !atShin || !planted) return;

        if (!Check())
        {
            Reset();
            Fail();
            return;
        }

        Vector3 pivot = ctx.toriFeet.Get(plant).legRoot.position;
        Vector3 axis = ctx.tori.transform.right;
        float angle = throwAngle * (invertRotation ? -1f : 1f);

        Vector3 forward = ctx.tori.transform.forward;
        forward.y = 0f;
        Vector3 drift = forward.sqrMagnitude > 1e-4f ? forward.normalized * landForward : Vector3.zero;

        ctx.toriFeet.Pin(plant);
        ctx.tori.HoldYaw();
        throwYaw = ctx.tori.HeldYaw;
        ctx.tori.HoldPosition(true);

        ctx.uke.HipThrow(pivot, axis, angle, drift);
        CurrentPhase = TechniquePhase.Executing;
    }

    /* THROW
     * 1 - While driving down, bow forward over the frozen facing with the
     *     reaping leg still attached to uke
     * 2 - Once uke has turned far enough over, let go: settle both feet back
     *     underneath tori and hand uke his footing back
     * 3 - While standing, swing the frozen facing back round to square and ease
     *     the bow out, leaving uke's vault to finish on its own
     * 4 - When uke is down and tori is upright, hand everything back and score
     */
    private void Throw()
    {
        float dt = Time.deltaTime;

        if (!squaredUp)
        {
            leanNow = Mathf.Lerp(leanNow, throwLean, leanInRate * dt);
            ctx.tori.SetLean(new Vector3(leanNow, 0f, 0f));

            if (ctx.uke.VaultTurned >= releaseAngle)
            {
                squaredUp = true;
                ctx.toriFeet.Settle(plant);
                ctx.toriFeet.Settle(reap);
                ctx.tori.SetDistance(standRadius);
                ctx.uke.HoldGround(true);
            }
            return;
        }

        throwYaw = Mathf.LerpAngle(throwYaw, ctx.tori.FacingYaw(), standYawRate * dt);
        ctx.tori.SetHeldYaw(throwYaw);

        leanNow = Mathf.Lerp(leanNow, 0f, standUpRate * dt);
        ctx.tori.SetLean(new Vector3(leanNow, 0f, 0f));

        if (Mathf.Abs(leanNow) <= 0.5f && ctx.uke.State == UkeState.Fallen)
        {
            ctx.tori.SetLean(Vector3.zero);
            ctx.tori.SnapLook(0f);
            ctx.tori.ReleaseYaw();
            ctx.tori.HoldPosition(false);
            Score();
        }
    }

    /* RESET
     * 1 - Give tori his feet, his facing and his freedom to move back
     * 2 - Square him up and restore the standing gap
     * 3 - Hold the gap steady rather than easing it, so a broken-off attempt
     *     does not send tori and uke chasing each other across the mat
     * 4 - Hand uke his footing back and straighten tori up
     */
    private void Reset()
    {
        ctx.toriFeet.Free();
        ctx.tori.ReleaseYaw();
        ctx.tori.HoldPosition(false);
        ctx.tori.SetLook(0f);
        ctx.tori.SetDistance(standRadius);
        ctx.tori.SetRadiusRecovery(false);
        ctx.uke.HoldGround(true);
        ctx.tori.SetLean(Vector3.zero);
    }

    /* PIVOT SPOT
     * 1 - Find uke's attacked leg and the point on the floor beneath it
     * 2 - Work out which way is "outward" by measuring from uke's centre out
     *     through that leg, falling back to tori's side if the two sit on top
     *     of each other
     * 3 - Step that far outside the leg, then a little along uke's facing, so
     *     the post lands beside him rather than on his toes
     * 4 - Keep it down at floor level
     */
    private Vector3 PivotSpot()
    {
        Vector3 attackedLeg = ctx.toriFeet.Get(reap).throwTarget.position;
        Vector3 attackBase = ctx.toriFeet.Get(reap).sweepTarget.position;

        Vector3 outward = attackedLeg - ctx.uke.transform.position;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : ctx.tori.transform.right * sign;

        Vector3 spot = attackBase + outward * outsideOffset;
        spot += ctx.uke.transform.TransformDirection(Vector3.forward * forwardOffset);
        spot.y = attackedLeg.y;
        return spot;
    }
}
