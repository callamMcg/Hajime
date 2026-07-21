using UnityEngine;

/// <summary>
/// De Ashi Barai - the advancing foot sweep.
/// Tori's foot chases uke's shin. The instant it lands on him the timing is
/// judged: only a foot tori has already lifted, by flicking the pull up, can be
/// swept away - a foot still carrying weight bounces the sweep off.
/// If it is caught, uke's base is gone and he is left hanging on the grip. He
/// then turns about tori's far hand - the one opposite the sweeping foot - and
/// is pushed round it while being brought down, so he arrives flat on the mat
/// exactly as the turn finishes.
/// </summary>
public class DeAshiBarai : Technique
{
    //----------Variables----------\\

    // How close the sweeping foot has to get to uke's shin to count as landing on it
    [SerializeField] private float contactRadius = 0.3f;

    // Degrees uke is pushed round the grip before he is flat on the mat
    [SerializeField] private float fallAngle = 110f;

    // Seconds that turn takes from the sweep landing to him going down
    [SerializeField] private float fallDuration = 0.5f;

    // Flips both sweeps at once, for if they each come out the wrong way round.
    // Which way round a given sweep goes is taken from the sweeping foot, so
    // this is only for turning the whole thing over
    [SerializeField] private bool invertWheel = false;

    // Raised when the pull fights the sweep, meaning this reach is really a hiza
    // guruma. Tori listens and hands the same side over to that technique.
    public event System.Action<FootId> Redirect;

    // The foot tori is sweeping with
    private FootId side;

    //----------Public Functions----------\\

    /* BEGIN
     * 1 - Remember which foot is doing the sweeping
     * 2 - Send that foot chasing uke's shin, and let the other one take his
     *     weight while it does
     */
    public override void Begin(FootId footSide)
    {
        side = footSide;

        ctx.toriFeet.Sweep(side);
        CurrentPhase = TechniquePhase.Reaching;
    }

    /* TICK
     * 1 - While reaching, keep watching for the foot to land on the shin
     * 2 - Once he is going down, keep the turn pointed at tori's hand
     */
    public override void Tick(float dt)
    {
        switch (CurrentPhase)
        {
            case TechniquePhase.Reaching: Reach(); break;
            case TechniquePhase.Executing: SwingDown(); break;
        }
    }

    /* CHECK
     * A foot still carrying uke's weight is planted too hard to move, so the
     * sweep just bounces off it. Only a foot tori has lifted - by flicking the
     * pull up - can be swept away, so the attempt fails unless the sweep lands
     * inside that window.
     */
    public override bool Check() => ctx.uke.Lift > 0f;

    /* CANCEL
     * 1 - Ignore this entirely once he is already going down, as by then the
     *     technique is committed and plays itself out
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
     * 1 - Wait until the sweeping foot has actually reached uke's shin
     * 2 - Judge that moment: if his weight is still on the foot the sweep
     *     bounces off and the attempt is over
     * 3 - Otherwise his base is gone, so start swinging him down
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

        Topple();
        CurrentPhase = TechniquePhase.Executing;
    }

    /* TOPPLE
     * 1 - Pick the hand across from the sweeping foot, so the grip he hangs
     *     from is on the opposite side of his body to the leg that was taken
     * 2 - Turn him about tori's z, which pushes him round sideways rather than
     *     swinging him through tori
     * 3 - Take which way round he goes from the foot that swept him. Taking his
     *     other leg away tips him the other way, so a right sweep runs opposite
     *     to a left one
     * 4 - Hand him over to that turn, which also brings him down to the mat
     * Tori deliberately keeps hold of his own feet here. The sweeping foot stays
     * with uke's leg the whole way down and only comes back to the floor once he
     * has actually landed
     */
    private void Topple()
    {
        FootId farHand = side == FootId.Left ? FootId.Right : FootId.Left;

        Vector3 axis = ctx.tori.transform.forward;
        float sweepSign = side == FootId.Left ? 1f : -1f;
        float angle = fallAngle * sweepSign * (invertWheel ? -1f : 1f);
        ctx.uke.SweptThrow(ctx.tori.HandPoint(farHand), axis, angle, fallDuration);
    }

    /* SWING DOWN
     * 1 - Tori's hand is still moving, so keep telling uke where it is now and
     *     let him turn about wherever it has got to
     * 2 - Once the turn has laid him out on the mat, give tori his feet back so
     *     the sweeping one finally comes down, and call the throw landed
     */
    private void SwingDown()
    {
        FootId farHand = side == FootId.Left ? FootId.Right : FootId.Left;
        ctx.uke.SetVaultPivot(ctx.tori.HandPoint(farHand));

        if (ctx.uke.State == UkeState.Fallen)
        {
            ctx.toriFeet.Free();
            Score();
        }
    }
}
