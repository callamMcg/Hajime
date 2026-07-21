using UnityEngine;

/// <summary>
/// The clock a judoka walks to.
/// It watches how fast he is actually travelling and turning, and turns that
/// into a cadence - move faster and he steps faster. That cadence drives a phase
/// that runs 0 to 1 over and over, and the feet read it to know when to lift and
/// when to plant, so they always agree with each other.
/// The same phase gives him his bob: he dips as his weight comes down through
/// both feet and rises over the flight, all of it fading out as he comes to rest
/// so a standing judoka is perfectly still.
/// On top of that sits the pulse - a sharp pull can haul him up or drive him
/// down for a moment, which is the opening a technique looks for. A pulse only
/// has anything to work with if he is already moving.
/// </summary>
[RequireComponent(typeof(JudokaBody))]
public class Gait : MonoBehaviour
{
    //----------Variables----------\\

    // The body this reads its speed from
    private JudokaBody body;

    // How far he travels in one stride, which sets how quickly speed becomes cadence
    [SerializeField] private float strideLength = 0.45f;

    // How wide his stance is, used to count turning on the spot as movement
    [SerializeField] private float stanceRadius = 0.18f;

    // The fastest he is allowed to step
    [SerializeField] private float maxFrequency = 2.6f;

    // The cadence a cycle already under way will always finish at, so the feet
    // are never left stranded mid step
    [SerializeField] private float recoveryFrequency = 1.1f;

    // How quickly cadence and speed catch up to what he is really doing
    [SerializeField] private float smoothing = 6f;

    // How far he rises over the flight of a step
    [SerializeField] private float hopHeight = 0.05f;

    // How far he dips as his weight comes down through both feet
    [SerializeField] private float crouchDepth = 0.03f;

    // The speed at which he is bobbing as much as he ever will
    [SerializeField] private float speedForFullBob = 1.2f;

    // How far a pulse hauls him up or drives him down, at full speed. It scales
    // down with how fast he is actually going, and a still judoka gets nothing
    [SerializeField] private float pulseHeight = 0.12f;

    // How long a pulse swells and fades over, at full speed. Scaled the same way
    [SerializeField] private float pulseDuration = 0.6f;

    // The height he stands at with everything else stripped away
    private float restHeight;

    // Where we are in the current step cycle, from 0 to 1
    private float phase;

    // How many full cycles have gone by
    private int cycle;

    // How quickly the phase is currently running
    private float frequency;

    // How "in motion" he is, from 0 standing still to 1 at full speed
    private float speedFactor;

    // How fast he is travelling across the mat
    private Vector2 planarVelocity;

    // How fast he is turning, in degrees per second
    private float yawVelocity;

    // Where he was last frame, to work the speeds out from
    private Vector2 lastPlanar;

    // Which way he was facing last frame, for the same reason
    private float lastYaw;

    // False until the first frame has given us something to compare against
    private bool hasLast;

    // Set when the feet need the phase to keep running even though he is still
    private bool cycleRequested;

    // Which way the current pulse is going: +1 hauled up, -1 driven down
    private float pulseSign;

    // Seconds left in the current pulse
    private float pulseT;

    // How long this particular pulse runs for, after being scaled by his speed
    private float pulseSpan;

    // How far this particular pulse moves him, scaled the same way
    private float pulseRise;

    //----------Unity Functions----------\\

    /* AWAKE
     * 1 - Find the body this reads its speed from
     * 2 - Remember the height he was placed at, as that is his standing height
     */
    private void Awake()
    {
        body = GetComponent<JudokaBody>();
        restHeight = transform.position.y;
    }

    //----------Public Functions----------\\

    /* TICK
     * Run once a frame, before anything reads his height.
     * 1 - Work out how fast he is travelling and turning by comparing this
     *     frame's pose to last frame's. This reads the pose rather than the
     *     transform, so his own bob can never be mistaken for him moving
     * 2 - Turn travelling and turning into one stride speed, and that into a cadence
     * 3 - A cycle that has already begun always finishes at the recovery cadence
     *     or better, so a step in mid air is never left hanging
     * 4 - Move the phase on. This adds to the phase rather than reading the clock,
     *     so the cadence can change without his height jumping
     * 5 - When the phase wraps round, count the cycle, and let him fall still if
     *     he has actually stopped
     * 6 - Run down whatever is left of any pulse
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

    /* HEIGHT
     * Gives the height the body should sit at, which is three things added up:
     * 1 - The height he simply stands at
     * 2 - His bob, dipping through the double support and rising over the
     *     flight, all of it fading out as he comes to rest
     * 3 - Whatever a flicked pull is currently doing to him
     */
    public float Height() => restHeight
                           + speedFactor * (hopHeight * 4 * phase * (1 - phase) - crouchDepth)
                           + PulseOffset();

    /* REQUEST CYCLE
     * 1 - Ask for one more cycle to run even though he is standing still
     * The feet use this when one of them needs the phase to move so it can get
     * itself back underneath him
     */
    public void RequestCycle() => cycleRequested = true;

    /* PULSE
     * 1 - Work out how much he has to give, from how fast he is already moving.
     *     There is nothing to haul on a judoka who is standing still, so at rest
     *     the flick is ignored outright
     * 2 - Take the direction: +1 hauls him up, -1 drives him down
     * 3 - Scale both how far he moves and how long it lasts by that speed, so a
     *     slow judoka gives a small, brief opening and a fast one a big, long one
     * 4 - Start it running. Each call restarts the swell from the beginning
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

    //----------Private Functions----------\\

    /* PULSE OFFSET
     * 1 - Nothing to add if no pulse is running
     * 2 - Otherwise swell the movement in and fade it back out across the
     *     pulse's length, so he is lifted or dropped and returned without
     *     snapping at either end
     */
    private float PulseOffset()
    {
        if (pulseT <= 0f || pulseSpan <= 0f) return 0f;

        // Runs 1 at the start down to 0 at the end
        float u = pulseT / pulseSpan;
        return pulseSign * pulseRise * Mathf.Sin(Mathf.PI * u);
    }

    //----------Getters----------\\

    // Which way a pulse is currently taking him: +1 up, -1 down, 0 for no pulse.
    // This is the window a technique is looking for
    public float PulseDirection => pulseT > 0f ? pulseSign : 0f;

    // Where we are in the current step cycle
    public float Phase => phase;

    // How many full cycles have gone by
    public int Cycle => cycle;

    // How quickly the phase is running
    public float Frequency => frequency;

    // How "in motion" he is, from 0 to 1
    public float SpeedFactor => speedFactor;

    // How fast he is travelling across the mat
    public Vector2 PlanarVelocity => planarVelocity;

    // How fast he is turning
    public float YawVelocity => yawVelocity;

    // True while a step cycle is actually under way
    public bool IsCycling => phase > 0f;
}
