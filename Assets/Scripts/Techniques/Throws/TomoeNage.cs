using UnityEngine;

/// <summary>
/// Tomoe Nage - the circle throw, and a sacrifice: tori gives up his own footing
/// to take uke over the top of him.
/// Both throw buttons at once commits it, and from that first frame it plays
/// straight through - there is nothing to judge and nothing to call off.
/// Uke is sent over the moment it starts, so the launch and the drop are one
/// motion: tori goes down onto his back with both feet riding uke's hips while
/// uke turns over his shoulders. Those shoulders are the point he turns about,
/// and they ride down to the mat with tori, so uke gets pulled down and round
/// with them. The drop itself is what carries him over - nothing pushes him.
/// Tori then stays on his back. Only his legs are handed back, so he holds the
/// finished pose rather than climbing to his feet.
/// </summary>
public class TomoeNage : Technique
{
    //----------Variables----------\\

    // Seconds tori takes to go from standing to flat on his back
    [SerializeField] private float dropDuration = 0.35f;

    // How low he sits once he is down
    [SerializeField] private float dropHeight = 0.35f;

    // How far back he pitches as he lays himself out
    [SerializeField] private float dropPitch = -75f;

    // How far apart the two feet sit across uke's hips
    [SerializeField] private float hipWidth = 0.12f;

    // How far below uke's middle the feet plant
    [SerializeField] private float hipDrop = 0.1f;

    // Degrees uke turns over tori's shoulders - 220 settles him flat
    [SerializeField] private float throwAngle = 220f;

    // Flip this if uke turns the wrong way over the top
    [SerializeField] private bool invertRotation = false;

    // How far through the drop we are, from 0 to 1
    private float dropT;

    // The height tori started the drop from
    private float fromHeight;

    //----------Public Functions----------\\

    /* BEGIN
     * 1 - Take tori's body over: freeze his facing and plant him, since he is
     *     going down and should not be sliding about
     * 2 - Remember how tall he was, so the drop has somewhere to start from
     * 3 - Let go of uke's footing so he can be turned over
     * 4 - Seize both of tori's feet so they can ride uke's hips all the way through
     * 5 - Send uke over straight away, turning about the shoulders
     */
    public override void Begin(FootId side)
    {
        if (IsRunning) return;

        ctx.tori.HoldYaw();
        ctx.tori.HoldPosition(true);
        fromHeight = ctx.tori.StandingHeight;

        ctx.uke.HoldGround(false);

        ctx.toriFeet.Hold(FootId.Left);
        ctx.toriFeet.Hold(FootId.Right);

        RollOver();

        dropT = 0f;
        CurrentPhase = TechniquePhase.Executing;
    }

    /* TICK
     * 1 - Do nothing unless the throw is actually running
     * 2 - Otherwise keep dropping tori and turning uke over him
     */
    public override void Tick(float dt)
    {
        if (CurrentPhase != TechniquePhase.Executing) return;

        Drop(dt);
    }

    /* CHECK
     * The circle throw always goes. There is no opening to read and no timing
     * to get right, so there is nothing here to judge.
     */
    public override bool Check() => true;

    /* CANCEL
     * There is nothing to call off. The sacrifice is committed from the moment
     * it starts, so this deliberately does nothing.
     */
    public override void Cancel() { }

    //----------Private Functions----------\\

    /* ROLL OVER
     * 1 - Turn uke about tori's shoulders rather than a spot on the floor
     * 2 - Drop keeps handing those shoulders back as they ride down to the mat,
     *     so uke is pulled down and round with them instead of orbiting a fixed
     *     point. That is why nothing needs to push him forward to clear the top
     */
    private void RollOver()
    {
        Vector3 axis = ctx.tori.transform.right;
        float angle = throwAngle * (invertRotation ? -1f : 1f);

        ctx.uke.HipThrow(ctx.tori.Shoulders, axis, angle, Vector3.zero);
    }

    /* DROP
     * 1 - Hand the turning point back every frame, so as tori's shoulders ride
     *     down they drag uke down and round with them
     * 2 - Ease tori down onto his back, pitching him out as he goes
     * 3 - Keep both feet planted on uke's hips while he goes over. These are
     *     re-asserted after the body has moved, so tori's own motion cannot
     *     drag his feet off uke
     * 4 - Once tori is down and uke is flat, the throw is finished
     */
    private void Drop(float dt)
    {
        ctx.uke.SetVaultPivot(ctx.tori.Shoulders);

        if (dropT < 1f)
        {
            dropT = Mathf.Min(dropT + dt / dropDuration, 1f);
            float ease = dropT * dropT * (3f - 2f * dropT);

            ctx.tori.HoldHeight(Mathf.Lerp(fromHeight, dropHeight, ease));
            ctx.tori.SetLean(new Vector3(dropPitch * ease, 0f, 0f));

            ctx.toriFeet.PlaceHeld(FootId.Left, HipSpot(-1f));
            ctx.toriFeet.PlaceHeld(FootId.Right, HipSpot(1f));
        }

        if (dropT >= 1f && ctx.uke.State == UkeState.Fallen) Finish();
    }

    /* FINISH
     * 1 - Ease tori's legs down to the mat beside him, since they are still up
     *     in the air where uke's hips used to be
     * 2 - Hand uke his footing back
     * 3 - Score, and deliberately leave tori's height, facing and position held
     *     so he stays lying on his back instead of climbing up
     */
    private void Finish()
    {
        ctx.toriFeet.Settle(FootId.Left);
        ctx.toriFeet.Settle(FootId.Right);

        ctx.uke.HoldGround(true);
        Score();
    }

    /* HIP SPOT
     * 1 - Start from uke's middle
     * 2 - Step out to one side of it, so the two feet straddle his hips
     * 3 - Drop a little below it, so they sit on the hip rather than the chest
     * side is -1 for the left foot and +1 for the right
     */
    private Vector3 HipSpot(float side)
    {
        return ctx.uke.transform.position
             + ctx.uke.transform.right * (hipWidth * side)
             - Vector3.up * hipDrop;
    }
}
