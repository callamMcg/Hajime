using UnityEngine;

/// <summary>
/// Tomoe Nage - the circle throw, and a sacrifice: tori gives up his own base
/// to take uke over the top. Both throw buttons at once commits it, and it
/// plays straight through - there is nothing to judge and nothing to cancel.
/// Uke is sent over the moment it starts, so the launch and the drop are one
/// motion: tori goes down onto his back with both feet riding uke's hips while
/// uke turns over his shoulders. The shoulders are the fulcrum and they ride
/// down to the mat with him, so uke is pulled down and round with them - the
/// drop itself is what carries him over, with nothing pushing him forward.
/// Finish: tori stays on his back. Only the legs are handed back; he holds the
/// finished pose rather than climbing to his feet.
/// </summary>
public class TomoeNage : Technique
{
    //------------------Variables------------------//
    // Drop - giving up the base
    [SerializeField] private float dropDuration = 0.35f; // seconds from standing to flat on the back
    [SerializeField] private float dropHeight = 0.35f;   // root height once he is down
    [SerializeField] private float dropPitch = -75f;     // backward pitch that lays him out

    // Feet - riding uke's hips
    [SerializeField] private float hipWidth = 0.12f;     // how far apart the feet sit across uke's hips
    [SerializeField] private float hipDrop = 0.1f;       // how far below uke's root the feet plant

    // Throw - uke goes over the top
    [SerializeField] private float throwAngle = 220f;     // degrees uke turns over tori's shoulders (220 settles him flat)
    [SerializeField] private bool invertRotation = false; // flip if uke turns the wrong way over the top

    // Trackers
    private float dropT;      // 0-1 down onto the back
    private float fromHeight; // the height the drop started from

    //------------------Public Functions------------------//
    /* BEGIN - both throw buttons: tori commits from the first frame
     * 1 - Take the body: freeze the facing and plant him, he is going down
     * 2 - Let go of uke's ground hold so he can be turned over
     * 3 - Seize both feet so they can ride uke's hips all the way through
     * 4 - Send uke over straight away, turning about the shoulders
     */
    public override void Begin(FootId side)
    {
        if (IsRunning) return;

        // 1
        ctx.tori.HoldYaw();
        ctx.tori.HoldPosition(true);
        fromHeight = ctx.tori.StandingHeight;

        // 2
        ctx.uke.HoldGround(false);

        // 3
        ctx.toriFeet.Hold(FootId.Left);
        ctx.toriFeet.Hold(FootId.Right);

        // 4
        RollOver();

        dropT = 0f;
        CurrentPhase = TechniquePhase.Executing; // committed - no reaching phase to break off
    }

    public override void Tick(float dt)
    {
        if (CurrentPhase != TechniquePhase.Executing) return;
        Drop(dt);
    }

    // The circle throw always goes - there is nothing to judge
    public override bool Check() => true;

    // Nothing to cancel - the sacrifice is committed the moment it starts
    public override void Cancel() { }

    //------------------Private Functions------------------//
    /* ROLL OVER - uke turns over tori's shoulders
     * The shoulders are the fulcrum, and Drop keeps handing them back as they
     * ride down to the mat: uke is pulled down and round with them rather than
     * orbiting a fixed spot, so no forward carry is needed to clear the top.
     */
    private void RollOver()
    {
        Vector3 axis = ctx.tori.transform.right;
        float angle = throwAngle * (invertRotation ? -1f : 1f);

        ctx.uke.HipThrow(ctx.tori.Shoulders, axis, angle, Vector3.zero);
    }

    /* DROP
     * 1 - Hand the fulcrum back every frame: as the shoulders ride down to the
     *     mat they pull uke down and round with them
     * 2 - Ease down onto the back, both feet riding uke's hips as he goes over.
     *     PlaceHeld is re-asserted after the body writes, so the feet stay on
     *     him rather than being dragged off by tori's own motion
     * 3 - Once tori is down and uke is flat, the throw is finished
     */
    private void Drop(float dt)
    {
        // 1
        ctx.uke.SetVaultPivot(ctx.tori.Shoulders);

        // 2
        if (dropT < 1f)
        {
            dropT = Mathf.Min(dropT + dt / dropDuration, 1f);
            float ease = dropT * dropT * (3f - 2f * dropT);
            ctx.tori.HoldHeight(Mathf.Lerp(fromHeight, dropHeight, ease));
            ctx.tori.SetLean(new Vector3(dropPitch * ease, 0f, 0f));
            ctx.toriFeet.PlaceHeld(FootId.Left, HipSpot(-1f));
            ctx.toriFeet.PlaceHeld(FootId.Right, HipSpot(1f));
        }

        // 3
        if (dropT >= 1f && ctx.uke.State == UkeState.Fallen) Finish();
    }

    /* FINISH - the throw is done and tori stays on his back
     * Only the legs are handed back, easing down to the mat beside him. The
     * height, facing and position holds are all left in place, so he holds the
     * finished pose instead of climbing back to his feet.
     */
    private void Finish()
    {
        ctx.toriFeet.Settle(FootId.Left);
        ctx.toriFeet.Settle(FootId.Right);
        ctx.uke.HoldGround(true);
        Score();
    }

    /* HIP SPOT - where one foot rides on uke's hips
     * side: -1 the left foot, +1 the right
     */
    private Vector3 HipSpot(float side)
    {
        return ctx.uke.transform.position
             + ctx.uke.transform.right * (hipWidth * side)
             - Vector3.up * hipDrop;
    }
}
