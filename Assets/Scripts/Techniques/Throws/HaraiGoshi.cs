using UnityEngine;

/// <summary>
/// Harai Goshi - the sweeping hip throw.
/// Reaching: tori turns his back in across uke (the look angle swings round)
/// and closes the distance while the reaping foot chases uke's shin. The
/// planted foot bases as the pivot post the throw turns over.
/// Contact: when the turn has come round and the reaping foot reaches the
/// shin, the moment is judged - uke must be tall and loaded (above the height
/// threshold) for the hip to get under him. If he is, the throw commits.
/// Executing: uke is vaulted a full turn over tori's loading hip - a rigid
/// rotation about the hip, on tori's own right axis - and settled flat, his
/// feet riding the body over. Tori holds the turned-in pose until uke lands,
/// then squares back up.
/// </summary>
public class HaraiGoshi : Technique
{
    //------------------Variables------------------//
    // Reach - turning in and finding the shin
    [SerializeField] private float turnAngle = 160f;     // degrees tori turns his back in (sign from the throw side)
    [SerializeField] private float turnTolerance = 12f;  // how close to the full turn counts as "come round"
    [SerializeField] private float contactRadius = 0.3f; // reaping foot to uke's shin distance that counts as contact
    [SerializeField] private float closeRadius = 0.6f;   // distance tori closes to for the hip to load
    [SerializeField] private float standRadius = 1.2f;   // distance restored when the throw ends or fails

    // Contact - the success gate
    [SerializeField] private float threshold = 0f;       // uke's height must exceed this to be thrown (loaded, not crouched)

    [SerializeField] private float throwLean = 15f;   // forward pitch tori drives down through the throw (negate if he bows backward)
    [SerializeField] private float leanInRate = 6f;   // how fast tori bows into the throw
    [SerializeField] private float standUpRate = 4f;  // how fast tori squares back up after uke lands

    private float leanNow;   // tori's current forward pitch
    private bool squaredUp;  // tori has begun standing up after the landing

    // Throw - the vault over the hip
    [SerializeField] private float throwAngle = 270f;    // degrees uke turns over the hip (270 settles him flat)
    [SerializeField] private bool invertRotation = false;// flip if uke goes the wrong way over the hip

    // Trackers
    private FootId plant; // tori's planted pivot foot (the throw side)
    private FootId reap;  // tori's reaping foot - chases uke's shin
    private float sign;   // +1 right throw, -1 left

    //------------------Public Functions------------------//
    /* BEGIN
     * 1 - Geometry: the throw side is the planted pivot foot, the other foot reaps
     * 2 - Turn the back in and close the distance so the hip can load
     * 3 - Set the reaping foot chasing the shin; the plant foot bases
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

        ctx.toriFeet.Reap(reap);   
        // 3
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
     * 1 - Wait for the turn to come round AND the reaping foot to reach the shin
     * 2 - Judge the moment: below the height threshold uke is crouched and
     *     unloaded, the hip cannot lift him, and the attempt fails
     * 3 - Commit: capture the pivot (tori's loading hip) and axis (tori's right),
     *     free tori's feet, and vault uke over the hip
     */
    private void Reach()
    {
        // 1
        FootManager.Foot reaper = ctx.toriFeet.Get(reap);
        bool turned = Mathf.Abs(Mathf.DeltaAngle(ctx.tori.LookAngle, sign * turnAngle)) <= turnTolerance;
        bool atShin = Vector3.Distance(reaper.target.position, reaper.throwTarget.position) <= contactRadius; 
        if (!turned || !atShin) return;

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
        ctx.toriFeet.Pin(plant);
        ctx.toriFeet.Pin(reap);

        ctx.uke.HipThrow(pivot, axis, angle);
        CurrentPhase = TechniquePhase.Executing;
    }

    /* THROW - hold the turned-in pose until uke lands, then square up and score */
    /* THROW
     * Bow forward over the hip, holding the turned-in yaw, while uke goes over.
     * Once uke lands, free the feet so the legs come back under the body,
     * square up to uke, ease the lean out, and score.
     */
    private void Throw()
    {
        float dt = Time.deltaTime;

        // still throwing - bow into it
        if (ctx.uke.State != UkeState.Fallen)
        {
            leanNow = Mathf.Lerp(leanNow, throwLean, leanInRate * dt);
            ctx.tori.SetLean(new Vector3(leanNow, 0f, 0f));
            return;
        }

        // uke has landed - stand up with the legs coming back under
        if (!squaredUp)
        {
            squaredUp = true;
            ctx.toriFeet.Free();
            ctx.tori.SetLook(0f);
            ctx.tori.SetDistance(standRadius);
            ctx.uke.HoldGround(true);
        }

        leanNow = Mathf.Lerp(leanNow, 0f, standUpRate * dt);
        ctx.tori.SetLean(new Vector3(leanNow, 0f, 0f));

        if (Mathf.Abs(leanNow) <= 0.5f)
        {
            ctx.tori.SetLean(Vector3.zero);
            Score();
        }
    }
    private void Reset()
    {
        ctx.toriFeet.Free();
        ctx.tori.SetLook(0f);
        ctx.tori.SetDistance(standRadius);
        ctx.uke.HoldGround(true);
        ctx.tori.SetLean(Vector3.zero);
    }
}