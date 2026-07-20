using UnityEngine;

/// <summary>
/// Harai Goshi - the sweeping hip throw.
/// Reaching: tori turns his back in across uke (the look angle swings round)
/// and closes the distance. The reaping foot chases uke's shin while the pivot
/// foot steps in to plant just outside uke's attacked leg - the post the throw
/// turns over.
/// Contact: when the turn has come round, the reaping foot reaches the shin and
/// the pivot foot is planted, the moment is judged. If uke is loaded the throw
/// commits: the pivot foot pins, tori's facing freezes so his yaw no longer
/// tracks uke, and uke is vaulted over the loading hip.
/// Executing: tori bows forward over the frozen yaw, the reaping leg staying
/// attached to uke as he goes over. Once uke has turned past the release angle,
/// tori settles both feet to a standing stance under his root and eases the
/// lean and yaw back to square, while uke's vault finishes to the mat on its own.
/// </summary>
public class HaraiGoshi : Technique
{
    //------------------Variables------------------//
    // Reach - turning in, finding the shin, planting the pivot foot
    [SerializeField] private float turnAngle = 160f;      // degrees tori turns his back in (sign from the throw side)
    [SerializeField] private float turnTolerance = 12f;   // how close to the full turn counts as "come round"
    [SerializeField] private float contactRadius = 0.3f;  // reaping foot to uke's shin distance that counts as contact
    [SerializeField] private float closeRadius = 0.6f;    // distance tori closes to for the hip to load
    [SerializeField] private float standRadius = 1.2f;    // distance restored when the throw ends or fails
    [SerializeField] private float outsideOffset = 0.15f; // how far outside uke's attacked leg the pivot foot plants
    [SerializeField] private float forwardOffset = 0.15f; // how far outside uke's attacked leg the pivot foot plants

    // Contact - the success gate
    [SerializeField] private float threshold = 0f;       // uke's height must exceed this to be thrown (loaded, not crouched)

    [SerializeField] private float throwLean = 15f;    // forward pitch tori drives down through the throw (negate if he bows backward)
    [SerializeField] private float leanInRate = 6f;    // how fast tori bows into the throw
    [SerializeField] private float standUpRate = 4f;   // how fast tori eases the lean out as he stands
    [SerializeField] private float releaseAngle = 130f;// uke's over-the-hip rotation at which tori lets go and stands up
    [SerializeField] private float standYawRate = 4f;  // how fast the frozen yaw re-squares to uke as tori stands

    private float leanNow;     // tori's current forward pitch
    private bool squaredUp;    // tori has begun standing up after the release
    private float throwYaw;    // the frozen yaw held through the throw, eased back to square as he stands

    // Throw - the vault over the hip
    [SerializeField] private float throwAngle = 270f;    // degrees uke turns over the hip (270 settles him flat)
    [SerializeField] private bool invertRotation = false;// flip if uke goes the wrong way over the hip
    [SerializeField] private float landForward = 1f;     // how far in front of tori uke drifts to land (along tori's forward)

    // Trackers
    private FootId plant; // tori's planted pivot foot (the throw side)
    private FootId reap;  // tori's reaping foot - chases uke's shin
    private float sign;   // +1 right throw, -1 left

    //------------------Public Functions------------------//
    /* BEGIN
     * 1 - Geometry: the throw side is the planted pivot foot, the other foot reaps
     * 2 - Turn the back in and close the distance so the hip can load
     * 3 - Reaping foot chases the shin; the pivot foot steps in to plant just
     *     outside uke's attacked leg
     */
    public override void Begin(FootId side)
    {
        if (IsRunning) return;

        // 1
        plant = side;
        reap = side == FootId.Left ? FootId.Right : FootId.Left;
        sign = side == FootId.Right ? 1f : -1f;

        // 2
        ctx.tori.SetLook(sign * turnAngle);
        ctx.tori.SetDistance(closeRadius);
        ctx.uke.HoldGround(false);

        // 3
        ctx.toriFeet.Reap(reap);                 // reaping foot chases uke's shin, pivot foot bases
        ctx.toriFeet.StepTo(plant, PivotSpot());  // pivot foot steps in beside uke's attacked leg

        leanNow = 0f;
        squaredUp = false;
        CurrentPhase = TechniquePhase.Reaching;
    }

    public override void Tick(float dt)
    {
        switch (CurrentPhase)
        {
            case TechniquePhase.Reaching: Reach(); break;
            case TechniquePhase.Executing: Throw(); break;
        }
    }

    // Uke can be thrown while he is tall and loaded, above the height threshold
    public override bool Check() { Debug.Log(ctx.uke.GetHeight); return true; }
            //ctx.uke.GetHeight > threshold;}

    /* CANCEL - only Reaching can be broken off; once uke is vaulting the
     * throw is committed and plays out */
    public override void Cancel()
    {
        if (CurrentPhase != TechniquePhase.Reaching) return;
        Reset();
        Fail();
    }

    //------------------Private Functions------------------//
    /* REACH
     * 1 - Keep the pivot foot's step aimed just outside uke's attacked leg, and
     *     wait for the turn to come round, the reaping foot to reach the shin,
     *     and the pivot foot to land
     * 2 - Judge the moment: below the height threshold uke is crouched and
     *     unloaded, the hip cannot lift him, and the attempt fails
     * 3 - Commit: pin the pivot foot as the post, capture the pivot (tori's
     *     loading hip) and axis (tori's right), freeze tori's yaw so it no
     *     longer tracks uke, and vault uke over the hip. The reaping foot is
     *     left attached to uke so tori's leg rides him over.
     */
    private void Reach()
    {
        // 1
        ctx.toriFeet.SetPlaceTarget(plant, PivotSpot());
        FootManager.Foot reaper = ctx.toriFeet.Get(reap);
        bool turned = Mathf.Abs(Mathf.DeltaAngle(ctx.tori.LookAngle, sign * turnAngle)) <= turnTolerance;
        bool atShin = Vector3.Distance(reaper.target.position, reaper.throwTarget.position) <= contactRadius;
        bool planted = ctx.toriFeet.Get(plant).state == FootState.Based;
        if (!turned || !atShin || !planted) return;

        // 2
        if (!Check())
        {
            Reset();
            Fail();
            return;
        }

        // 3
        Vector3 pivot = ctx.toriFeet.Get(plant).legRoot.position;
        Vector3 axis = ctx.tori.transform.right;
        float angle = throwAngle * (invertRotation ? -1f : 1f);
        Vector3 forward = ctx.tori.transform.forward; forward.y = 0f;
        Vector3 drift = forward.sqrMagnitude > 1e-4f ? forward.normalized * landForward : Vector3.zero;
        ctx.toriFeet.Pin(plant);     // pivot foot holds as the post; the reaping foot stays attached to uke
        ctx.tori.HoldYaw();          // yaw no longer tracks uke through the throw
        throwYaw = ctx.tori.HeldYaw;
        ctx.tori.HoldPosition(true); // plant the hip as the fixed pivot the vault turns over

        ctx.uke.HipThrow(pivot, axis, angle, drift); // drift carries uke forward to land in front of tori
        CurrentPhase = TechniquePhase.Executing;
    }

    /* THROW
     * Driving down: bow forward over the frozen yaw while uke vaults over the
     * hip, the reaping leg still attached to him, until he has turned past the
     * release angle.
     * Standing: settle both feet to the standing home under tori - their targets
     * move to a fixed stance relative to his root - ease the lean out and
     * re-square the frozen yaw to face uke, while uke's vault finishes to the
     * mat on its own. Once he is down and the lean is out, release the yaw and score.
     */
    private void Throw()
    {
        float dt = Time.deltaTime;

        // driving down into the throw, until uke has gone far enough over the hip
        if (!squaredUp)
        {
            leanNow = Mathf.Lerp(leanNow, throwLean, leanInRate * dt);
            ctx.tori.SetLean(new Vector3(leanNow, 0f, 0f));

            if (ctx.uke.VaultTurned >= releaseAngle)
            {
                squaredUp = true;
                ctx.toriFeet.Settle(plant); // targets ease to the standing stance under tori
                ctx.toriFeet.Settle(reap);
                ctx.tori.SetDistance(standRadius);
                ctx.uke.HoldGround(true);
            }
            return;
        }

        // standing up: re-square the frozen yaw and ease the lean out while uke
        // finishes falling to the mat on his own
        throwYaw = Mathf.LerpAngle(throwYaw, ctx.tori.FacingYaw(), standYawRate * dt);
        ctx.tori.SetHeldYaw(throwYaw);

        leanNow = Mathf.Lerp(leanNow, 0f, standUpRate * dt);
        ctx.tori.SetLean(new Vector3(leanNow, 0f, 0f));

        // uke down and the lean out - hand the yaw back, square the look, and score
        if (Mathf.Abs(leanNow) <= 0.5f && ctx.uke.State == UkeState.Fallen)
        {
            ctx.tori.SetLean(Vector3.zero);
            ctx.tori.SnapLook(0f);
            ctx.tori.ReleaseYaw();
            ctx.tori.HoldPosition(false);
            Score();
        }
    }

    private void Reset()
    {
        ctx.toriFeet.Free();
        ctx.tori.ReleaseYaw();
        ctx.tori.HoldPosition(false);
        ctx.tori.SetLook(0f);
        ctx.tori.SetDistance(standRadius);
        ctx.uke.HoldGround(true);
        ctx.tori.SetLean(Vector3.zero);
    }

    /* PIVOT SPOT - the ground point just outside uke's attacked leg, where
     * tori's pivot foot plants to post the throw. Outward is measured from uke's
     * centre through his attacked leg, falling back to tori's right if the two
     * are coincident.
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