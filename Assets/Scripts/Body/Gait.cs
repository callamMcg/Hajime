using UnityEngine;

[RequireComponent(typeof(JudokaBody))]
public class Gait : MonoBehaviour
{
    //------------------Variables------------------//
    // Components
    private JudokaBody body;

    // Stride - speed into cadence
    [SerializeField] private float strideLength = 0.45f;
    [SerializeField] private float stanceRadius = 0.18f;
    [SerializeField] private float maxFrequency = 2.6f;
    [SerializeField] private float recoveryFrequency = 1.1f;
    [SerializeField] private float smoothing = 6f;

    // Bob - phase into height
    [SerializeField] private float hopHeight = 0.05f;
    [SerializeField] private float crouchDepth = 0.03f;
    [SerializeField] private float speedForFullBob = 1.2f;

    // Pulse - a flicked pull hauls the body up or drives it down for a moment.
    // Both of these are the values at full speed: they scale down with how fast
    // the judoka is actually moving, and a still one gives nothing at all
    [SerializeField] private float pulseHeight = 0.12f;  // metres the pulse lifts (up) or drops (down) the body at full speed
    [SerializeField] private float pulseDuration = 0.6f; // seconds the pulse swells and fades back over at full speed

    // Trackers
    private float restHeight; 
    private float phase; // 0-1 through the current cycle
    private int cycle; // completed cycle count
    private float frequency;
    private float speedFactor; // 0-1, how "in motion" the judoka is
    private Vector2 planarVelocity;
    private float yawVelocity; // signed degrees per second
    private Vector2 lastPlanar;
    private float lastYaw;
    private bool hasLast;
    private bool cycleRequested; // the FootManager wants a cycle to run
    private float pulseSign;     // +1 hauled up, -1 driven down
    private float pulseT;        // seconds left in the pulse
    private float pulseSpan;     // this pulse's duration, scaled by the speed it was thrown at
    private float pulseRise;     // this pulse's height, scaled the same way

    //------------------Unity Functions------------------//
    private void Awake()
    {
        body = GetComponent<JudokaBody>();
        restHeight = transform.position.y;
    }

    //------------------Public Functions------------------//
    /* TICK - once per frame, before Height() is read
     * 1 - Measure planar and yaw speed from the body's pose (not the transform,
     *     so the bob's own height can never feed back into the speed)
     * 2 - Stride speed = travel + turning; cadence = stride speed / stride length
     * 3 - A cycle that has begun always finishes at the recovery cadence or
     *     better, so the feet are never stranded mid swing
     * 4 - Integrate the phase. Never evaluate a wave against raw time - adding
     *     frequency * dt lets the frequency change without the height jumping
     * 5 - On the wrap, count the cycle, and fall idle if the body has stopped
     * 6 - Run down any pulse a flicked pull has kicked off
     */
    public void Tick()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1
        BodyPose pose = body.GetPose();
        if (!hasLast) { lastPlanar = pose.planar; lastYaw = pose.yaw; hasLast = true; }
        planarVelocity = (pose.planar - lastPlanar) / dt;
        yawVelocity = Mathf.DeltaAngle(lastYaw, pose.yaw) / dt;
        lastPlanar = pose.planar;
        lastYaw = pose.yaw;

        // 2
        float strideSpeed = planarVelocity.magnitude + Mathf.Abs(yawVelocity) * Mathf.Deg2Rad * stanceRadius;
        float raw = Mathf.Clamp(strideSpeed / strideLength, 0f, maxFrequency);

        // 3
        bool wanted = cycleRequested;
        cycleRequested = false;
        float target = raw;
        if (phase > 0f || wanted) target = Mathf.Max(target, recoveryFrequency);

        frequency = Mathf.Lerp(frequency, target, smoothing * dt);
        speedFactor = Mathf.Lerp(speedFactor, Mathf.Clamp01(strideSpeed / speedForFullBob), smoothing * dt);

        // 4
        phase += frequency * dt;

        // 5
        if (phase >= 1f)
        {
            phase -= 1f;
            cycle++;
            if (raw < 0.05f && !wanted) { phase = 0f; frequency = 0f; }
        }

        // 6
        if (pulseT > 0f) pulseT = Mathf.Max(pulseT - dt, 0f);
    }

    // Absolute Y for the body: standing height, dipped through double support
    // and lifted over the flight, both fading away as the judoka comes to rest,
    // plus whatever a flicked pull is doing to him
    public float Height() => restHeight
                           + speedFactor * (hopHeight * 4 * phase * (1 - phase) - crouchDepth)
                           + PulseOffset();

    // Ask for one full cycle even while the body is still (feet re-homing)
    public void RequestCycle() => cycleRequested = true;

    /* PULSE - a flicked pull hauls the body up (+1) or drives it down (-1)
     * One shot: each call restarts the swell. Both the height and the length
     * scale with how fast the judoka is already travelling when the flick lands
     * - there is nothing to haul on a body that is standing still, so at rest
     * the flick is ignored outright.
     */
    public void Pulse(float sign)
    {
        float scale = Mathf.Clamp01(speedFactor);
        if (scale <= 0f) return;

        pulseSign = Mathf.Sign(sign);
        pulseRise = pulseHeight * scale;
        pulseSpan = pulseDuration * scale;
        pulseT = pulseSpan;
    }

    //------------------Private Functions------------------//
    /* PULSE OFFSET
     * The pulse swells in and fades back out across its duration, so a flick
     * lifts (or drops) the body and returns it without popping at either end
     */
    private float PulseOffset()
    {
        if (pulseT <= 0f || pulseSpan <= 0f) return 0f;
        float u = pulseT / pulseSpan; // 1 at the start, 0 at the end
        return pulseSign * pulseRise * Mathf.Sin(Mathf.PI * u);
    }

    //------------------Getters------------------//
    // +1 while a flick is hauling the body up, -1 while it is driving it down,
    // 0 when no pulse is running - the window a technique looks for
    public float PulseDirection => pulseT > 0f ? pulseSign : 0f;

    public float Phase => phase;
    public int Cycle => cycle;
    public float Frequency => frequency;
    public float SpeedFactor => speedFactor;
    public Vector2 PlanarVelocity => planarVelocity;
    public float YawVelocity => yawVelocity;
    public bool IsCycling => phase > 0f;
}