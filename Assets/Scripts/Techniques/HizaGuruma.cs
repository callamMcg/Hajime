using UnityEngine;

/// <summary>
/// Hiza Guruma - the knee wheel.
/// Reached for when a foot arrives at uke's leg but the pull is wrong for a
/// sweep (De Ashi Barai hands the side over here). Where the sweep breaks uke
/// by timing, the wheel breaks him by force: tori's foot blocks the shin as an
/// axle, uke's feet are pinned so he cannot step to recover, and tori pulls
/// him over the block for a short window. The pull fights uke's own balance
/// spring - if it drives his lean past the limit he goes over; if he holds out
/// the window, the pull is spent and he is let go to recover.
/// </summary>
public class HizaGuruma : Technique
{
    //------------------Variables------------------//
    // Pull - breaking uke's balance over the block
    [SerializeField] private float pullDuration = 0.8f;        // seconds tori has to break uke before he recovers
    [SerializeField] private float rampTime = 0.15f;           // seconds for the pull to reach full strength
    [SerializeField] private float pullStrength = 2.5f;        // lateral pull fed to uke's balance (a full stick pull is 1) - the main tuning knob against uke's balance stats
    [SerializeField] private float forwardBias = 0.4f;         // a little forward pitch so uke wheels over rather than toppling flat sideways
    [SerializeField] private float breakFraction = 0.98f;      // fraction of uke's lateral limit that counts as broken
    [SerializeField] private bool invertPullDirection = false; // flip if uke is pulled the wrong way in play

    // Trackers
    private FootId side;      // tori's acting (blocking) foot
    private float sweptSign;  // -1 uke's left foot is the axle, +1 his right (mirrors De Ashi Barai)
    private float breakSign;  // roll direction uke is driven toward
    private float elapsed;    // seconds into the pull

    //------------------Public Functions------------------//
    /* BEGIN
     * Entered already committed - the contact that would have been a sweep has
     * happened, so there is no reaching phase.
     * 1 - Geometry mirrors De Ashi Barai: uke's blocked foot is the wheel's
     *     axle and gives the sign; breakSign is the roll uke is pulled toward
     * 2 - Pin both of uke's feet so he cannot step out from under the pull.
     *     Tori's feet are left as the sweep set them - the acting foot resting
     *     against uke's leg as the block, the other based
     */
    public override void Begin(FootId footSide)
    {
        if (IsRunning) return;

        // 1
        side = footSide;
        FootId axle = side == FootId.Left ? FootId.Right : FootId.Left;
        sweptSign = axle == FootId.Left ? -1f : 1f;
        breakSign = invertPullDirection ? -sweptSign : sweptSign;

        // 2
        ctx.ukeFeet.Pin(FootId.Left);
        ctx.ukeFeet.Pin(FootId.Right);

        elapsed = 0f;
        CurrentPhase = Phase.Executing;
    }

    // Driven from Tori's Update
    public override void Tick(float dt)
    {
        switch (CurrentPhase)
        {
            //case Phase.Reaching: Reach(); break;
            case Phase.Executing: Wheel(dt); break;
        }
    }



    /* CANCEL
     * Committed the moment it begins - like De Ashi Barai's execute phase, the
     * wheel plays out to a throw or a recovery and ignores the release
     */
    public override void Cancel() { }

    //------------------Private Functions------------------//
    /* WHEEL
     * 1 - Ramp the pull in and feed it to uke. Press leaves his balance spring
     *     running, so this fights his own resistance rather than overriding it
     * 2 - Broken: his lean has been driven to the limit - he goes over the
     *     block. Fall continues in the same direction the pull was driving,
     *     tori's feet are freed, and the technique scores
     * 3 - Held out: the window closes with uke still on his feet - hand his
     *     balance back, free both sets of feet, and fail
     */
    private void Wheel(float dt)
    {
        // 1
        elapsed += dt;
        float ramp = Mathf.Clamp01(elapsed / rampTime);
        ctx.uke.Press(new Vector2(breakSign * pullStrength * ramp, forwardBias * ramp));

        // 2
        if (ctx.uke.PastLateralLimit(breakFraction))
        {
            ctx.uke.Fall(-breakSign);
            ctx.toriFeet.Free();
            Score();
            return;
        }

        // 3
        if (elapsed >= pullDuration)
        {
            ctx.uke.Recover();
            ctx.ukeFeet.Free();
            ctx.toriFeet.Free();
            Fail();
        }
    }
}

