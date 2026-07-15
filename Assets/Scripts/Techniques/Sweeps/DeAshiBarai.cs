using UnityEngine;

/// <summary>
/// De Ashi Barai - the advancing foot sweep.
/// Reaching: the sweeping foot chases uke's leg (FootManager's Swept state).
/// On contact the timing is judged: a grounded foot is load bearing, so the
/// sweep bounces off and fails; a foot that cannot reach the floor is caught.
/// Executing: the caught foot is seized and carried across toward uke's
/// support foot while his body tips over the closing gap. Partway across, the
/// carried foot strikes the support foot: it is knocked onward in the
/// direction of the sweep, so the throw reads as one leg genuinely taking the
/// other out rather than the two feet simply meeting. Arriving is the point
/// of no return - uke falls onto his back and tori's feet are sent home.
/// </summary>
public class DeAshiBarai : Technique
{
    //------------------Variables------------------//
    // Reach - judging the contact
    [SerializeField] private float contactRadius = 0.3f; // sweeping foot to uke's leg distance that counts as contact

    // Sweep - carrying the caught foot
    [SerializeField] private float sweepDuration = 0.3f; // seconds to carry the foot across to the support
    [SerializeField] private float tipPitch = -20f;      // backward pitch fed to uke at full sweep (negative is onto the back)
    [SerializeField] private float tipRoll = 25f;        // roll toward the swept side at full sweep

    // Knock - the support foot's reaction when the carried foot reaches it
    [SerializeField] private float knockRadius = 0.15f;   // carried-to-support distance that counts as a strike
    [SerializeField] private float knockDistance = 0.35f; // how far the support foot is knocked
    [SerializeField] private float knockDuration = 0.15f; // seconds the knock takes to land

    // Direction - the pull judged against the sweep at the moment of contact
    [SerializeField] private float directionTolerance = 3f;      // roll (degrees) below which the pull is too weak to read a side from
    [SerializeField] private bool invertDirectionCheck = false;  // flip if a matching pull reads as opposing in play

    // Fired when the pull fights the sweep: this reach is a hiza guruma, not
    // a de ashi barai. Tori listens and hands the side to that technique.
    public event System.Action<FootId> Redirect;

    // Trackers
    private FootId side;       // tori's sweeping foot
    private FootId caught;     // uke's swept foot - the mirror of side, as wired in the scene
    private FootId support;    // uke's other foot, the destination of the carry
    private float sweptSign;   // -1 uke's left foot is caught, +1 his right
    private float sweepT;      // 0-1 through the carry
    private Vector3 sweepFrom; // where the caught foot was seized
    private Vector3 sweepTo;   // where the support foot stood when the carry began - a fixed anchor, so knocking the support foot later doesn't drag the carry off course
    private bool knocked;      // the carried foot has already struck the support foot this sweep
    private float knockT;      // 0-1 through the knock
    private Vector3 knockFrom; // support foot's position the instant it was struck
    private Vector3 knockTo;   // where the strike sends it
    [SerializeField] private float threshold;

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
            case TechniquePhase.Executing: Sweep(dt); break;
        }
    }
    public override bool Check()
    {
        return ctx.uke.GetHeight > threshold;

    }
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
     * 2 - Judge the moment: a grounded foot is load bearing - the sweep
     *     bounces off and the attempt fails
     * 3 - A foot that cannot reach the floor is caught: seize it, pin uke's
     *     support so he cannot step out of the technique, and commit
     */
    private void Reach()
    {
        // 1
        FootManager.Foot sweeper = ctx.toriFeet.Get(side);
        if (Vector3.Distance(sweeper.target.position, sweeper.sweepTarget.position) > contactRadius) return;

        // 2
        if (ctx.uke.GetHeight > threshold)
        {
            Debug.Log(ctx.uke.GetHeight);
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
        CurrentPhase = TechniquePhase.Executing;
    }

    /* SWEEP
     * 1 - Carry the caught foot across toward the anchored support point
     * 2 - Contact: once the carry closes to knockRadius of the support foot,
     *     it is struck for the first time - knocked onward along the sweep's
     *     own direction of travel, so the impact looks like it came from the
     *     collision rather than an arbitrary shove
     * 3 - Play the knock out over its own short duration, independent of the
     *     carry, so it reads as a distinct impact rather than the two feet
     *     drifting together
     * 4 - Tip uke over the closing gap, in proportion to the carry
     * 5 - Arrival is the point of no return: uke falls onto his back (his
     *     feet ride the fall from inside Fall), tori's feet are sent home,
     *     and the technique scores
     */
    private void Sweep(float dt)
    {
        // 1
        sweepT = Mathf.Min(sweepT + dt / sweepDuration, 1f);
        float ease = sweepT * sweepT * (3f - 2f * sweepT);
        Vector3 carried = Vector3.Lerp(sweepFrom, sweepTo, ease);
        ctx.ukeFeet.PlaceHeld(caught, carried);

        // 2
        if (!knocked && Vector3.Distance(carried, sweepTo) <= knockRadius)
        {
            knocked = true;
            Vector3 travel = sweepTo - sweepFrom; travel.y = 0f;
            Vector3 dir = travel.sqrMagnitude > 0.0001f ? travel.normalized : ctx.tori.transform.forward;
            knockFrom = sweepTo;
            knockTo = sweepTo + dir * knockDistance;
            knockT = 0f;
        }

        // 3
        if (knocked)
        {
            knockT = Mathf.Min(knockT + dt / knockDuration, 1f);
            float knockEase = knockT * knockT * (3f - 2f * knockT);
            ctx.ukeFeet.Knock(support, Vector3.Lerp(knockFrom, knockTo, knockEase));
        }

        // 4
        ctx.uke.Tip(new Vector3(tipPitch, 0f, -sweptSign * tipRoll) * ease);

        // 5
        if (sweepT >= 1f)
        {
            ctx.uke.Fall(sweptSign);
            ctx.toriFeet.Free();
            Score();
        }
    }

}