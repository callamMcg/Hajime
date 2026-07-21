using UnityEngine;

/// <summary>
/// De Ashi Barai - the advancing foot sweep.
/// Reaching: the sweeping foot chases uke's leg (FootManager's Swept state).
/// The moment it lands on the shin the timing is judged: only a foot tori has
/// lifted, by flicking the pull up, can be swept out from under him.
/// Executing: with his base gone, uke hangs from the grip and turns about
/// tori's far hand - the one opposite the sweeping foot - swinging down about it
/// until the arc puts him on the mat. There is no fixed arc: it ends when he
/// lands, however far round that takes.
/// </summary>
public class DeAshiBarai : Technique
{
    //------------------Variables------------------//
    // Reach - judging the contact
    [SerializeField] private float contactRadius = 0.3f; // sweeping foot to uke's shin distance that counts as landing on it

    // Swing down - he turns about tori's far hand and comes down with it
    [SerializeField] private float fallAngle = 110f;    // degrees he turns about the grip before he is flat on the mat
    [SerializeField] private float fallDuration = 0.5f; // seconds that turn takes
    [SerializeField] private bool invertWheel = false;  // flip if he swings the wrong way round

    // Fired when the pull fights the sweep: this reach is a hiza guruma, not
    // a de ashi barai. Tori listens and hands the side to that technique.
    public event System.Action<FootId> Redirect;

    // Trackers
    private FootId side; // tori's sweeping foot

    //------------------Public Functions------------------//
    /* BEGIN
     * Start the chase: the sweeping foot goes Swept and chases uke's shin, the
     * other one bases
     */
    public override void Begin(FootId footSide)
    {
        if (IsRunning) return;

        side = footSide;
        ctx.toriFeet.Sweep(side);
        CurrentPhase = TechniquePhase.Reaching;
    }

    // One branch per phase, driven from Tori's Update
    public override void Tick(float dt)
    {
        switch (CurrentPhase)
        {
            case TechniquePhase.Reaching: Reach(); break;
            case TechniquePhase.Executing: SwingDown(); break;
        }
    }

    /* CHECK - the moment is judged on the flick
     * A loaded foot is load bearing: the sweep bounces off it. Only a foot tori
     * has lifted - by flicking the pull up - can be swept away, so the sweep
     * fails unless it lands on the shin inside that window.
     */
    public override bool Check() => ctx.uke.Lift > 0f;

    /* CANCEL
     * Only Reaching can be broken off - once he is swinging the technique is a
     * commitment and plays out
     */
    public override void Cancel()
    {
        if (CurrentPhase != TechniquePhase.Reaching) return;
        ctx.toriFeet.Free();
        Fail();
    }

    //------------------Private Functions------------------//
    /* REACH
     * 1 - Wait for the sweeping foot to land on uke's shin
     * 2 - Judge that moment: a foot still carrying his weight bounces the sweep
     *     off, and the attempt fails
     * 3 - Swept: his base is gone, so he is hung on the far grip and swung down
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
        Topple();
        CurrentPhase = TechniquePhase.Executing;
    }

    /* TOPPLE - the foot is gone from under him
     * He turns about tori's far hand, the one opposite the sweeping foot, so the
     * grip he is hanging from is across his body from the leg that was taken.
     * The axis is tori's z - his forward - so the turn pushes uke round sideways
     * about the grip rather than swinging him through tori. He does not need the
     * arc to carry him down: the descent is driven across the whole turn, so the
     * angle only decides how far round he is pushed on the way to the mat.
     */
    private void Topple()
    {
        FootId farHand = side == FootId.Left ? FootId.Right : FootId.Left;

        Vector3 axis = ctx.tori.transform.forward; // tori's local z
        float angle = fallAngle * (invertWheel ? -1f : 1f);
        ctx.uke.SweptThrow(ctx.tori.HandPoint(farHand), axis, angle, fallDuration);

        ctx.toriFeet.Free();
    }

    /* SWING DOWN
     * Keep handing the fulcrum back - tori's hand is still moving, and uke turns
     * about wherever it is now - until the arc has put him on the mat.
     */
    private void SwingDown()
    {
        FootId farHand = side == FootId.Left ? FootId.Right : FootId.Left;
        ctx.uke.SetVaultPivot(ctx.tori.HandPoint(farHand));

        if (ctx.uke.State == UkeState.Fallen) Score();
    }
}
