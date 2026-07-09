using UnityEngine;

/// <summary>
/// De Ashi Barai - the advancing foot sweep.
/// Reaching: the sweeping foot chases uke's leg (FootManager's Swept state).
/// On contact the timing is judged: a grounded foot is load bearing, so the
/// sweep bounces off and fails; a foot that cannot reach the floor is caught.
/// Executing: the caught foot is seized and carried across to uke's support
/// foot while his body tips over the closing gap. Arriving is the point of
/// no return - uke falls onto his back and tori's feet are sent home.
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

    // Trackers
    private FootId side;       // tori's sweeping foot
    private FootId caught;     // uke's swept foot - the mirror of side, as wired in the scene
    private FootId support;    // uke's other foot, the destination of the carry
    private float sweptSign;   // -1 uke's left foot is caught, +1 his right
    private float sweepT;      // 0-1 through the carry
    private Vector3 sweepFrom; // where the caught foot was seized

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
        CurrentPhase = Phase.Reaching;
    }

    // One branch per phase, driven from Tori's Update
    public override void Tick(float dt)
    {
        switch (CurrentPhase)
        {
            case Phase.Reaching: Reach(); break;
            case Phase.Executing: Sweep(dt); break;
        }
    }

    /* CANCEL
     * Only Reaching can be broken off - once the foot is caught the
     * technique is a commitment and plays out
     */
    public override void Cancel()
    {
        if (CurrentPhase != Phase.Reaching) return;
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
        if (ctx.ukeFeet.Get(caught).IsGrounded)
        {
            ctx.toriFeet.Free();
            Fail();
            return;
        }

        // 3
        ctx.ukeFeet.Hold(caught);
        ctx.ukeFeet.Pin(support);
        sweepFrom = ctx.ukeFeet.Get(caught).target.position;
        sweepT = 0f;
        CurrentPhase = Phase.Executing;
    }

    /* SWEEP
     * 1 - Carry the caught foot across to the live support foot
     * 2 - Tip uke over the closing gap, in proportion to the carry
     * 3 - Arrival is the point of no return: uke falls onto his back (his
     *     feet ride the fall from inside Fall), tori's feet are sent home,
     *     and the technique scores
     */
    private void Sweep(float dt)
    {
        // 1
        sweepT = Mathf.Min(sweepT + dt / sweepDuration, 1f);
        float ease = sweepT * sweepT * (3f - 2f * sweepT);
        Vector3 to = ctx.ukeFeet.Get(support).target.position;
        ctx.ukeFeet.PlaceHeld(caught, Vector3.Lerp(sweepFrom, to, ease));

        // 2
        ctx.uke.Tip(new Vector3(tipPitch, 0f, -sweptSign * tipRoll) * ease);

        // 3
        if (sweepT >= 1f)
        {
            ctx.uke.Fall(sweptSign);
            ctx.toriFeet.Free();
            Score();
        }
    }
}