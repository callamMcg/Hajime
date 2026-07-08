using UnityEngine;

/// <summary>
/// The judoka's movement clock.
/// Measures how fast the body is travelling and turning, turns that into a
/// stride frequency, and integrates one 0-1 phase that the whole gait shares:
/// the bob is a pure function of the phase, and the FootManager times both
/// feet from the same value, so the feet and the bob can never fall out of time.
/// </summary>

[RequireComponent(typeof(JudokaBody))]
public class Gait : MonoBehaviour
{
    //------------------Variables------------------//
    // Components
    private JudokaBody body;

    // Stride - speed into cadence
    [SerializeField] private float strideLength = 0.45f;     // metres of travel per full cycle
    [SerializeField] private float stanceRadius = 0.18f;     // converts turning speed into equivalent travel
    [SerializeField] private float maxFrequency = 2.6f;      // cycles per second cap
    [SerializeField] private float recoveryFrequency = 1.1f; // minimum cadence once a cycle has started
    [SerializeField] private float smoothing = 6f;           // how quickly cadence and bob follow speed

    // Bob - phase into height
    [SerializeField] private float hopHeight = 0.05f;      // rise at mid flight, at full speed
    [SerializeField] private float crouchDepth = 0.03f;    // dip at double support, at full speed
    [SerializeField] private float speedForFullBob = 1.2f; // stride speed that reaches the full bob

    // Trackers
    private float restHeight;     // standing height captured on wake
    private float phase;          // 0-1 through the current cycle, parked at 0 when idle
    private int cycle;          // completed cycle count, the feet stamp against this
    private float frequency;      // current cycles per second
    private float speedFactor;    // 0-1, how "in motion" the judoka is
    private Vector2 planarVelocity;
    private float yawVelocity;    // signed degrees per second
    private Vector2 lastPlanar;
    private float lastYaw;
    private bool hasLast;
    private bool cycleRequested; // the FootManager wants a cycle to run

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
    }

    // Absolute Y for the body: standing height, dipped through double support
    // and lifted over the flight, both fading away as the judoka comes to rest
    public float Height() => restHeight + speedFactor * (hopHeight * 4f * phase * (1f - phase) - crouchDepth);

    // Ask for one full cycle even while the body is still, e.g. feet re-homing
    public void RequestCycle() => cycleRequested = true;

    //------------------Getters------------------//
    public float Phase => phase;
    public int Cycle => cycle;
    public float Frequency => frequency;
    public float SpeedFactor => speedFactor;
    public Vector2 PlanarVelocity => planarVelocity;
    public float YawVelocity => yawVelocity;
    public bool IsCycling => phase > 0f;
}