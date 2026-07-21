using UnityEngine;

/// <summary>
/// Plays the recorded match back.
/// While it is running it switches the judokas' own drivers off, so the recorded
/// frames are the only thing deciding where anyone is - otherwise the judokas
/// would carry on fighting underneath the replay and fight it for control.
/// It runs on unscaled time, since the match is frozen throughout, and can be
/// played at different speeds or looped round.
/// Before it starts it takes a snapshot of the frozen moment it interrupted, and
/// puts everyone back there when it finishes, so watching a replay leaves the
/// match exactly as it found it.
/// </summary>
public class ReplayController : MonoBehaviour
{
    //----------Variables----------\\

    // Where the recorded frames are kept
    [SerializeField] private ReplayRecorder recorder;

    // The judoka doing the throwing
    [SerializeField] private Judoka tori;

    // The judoka being thrown
    [SerializeField] private Judoka uke;

    // Whether playback starts again from the beginning when it runs out
    [SerializeField] private bool loop = true;

    // True while the replay is actually playing
    public bool IsReplaying { get; private set; }

    // The frozen moment the replay interrupted, put back when it finishes
    private ReplayFrame liveFrame;

    // How far through the recording we currently are, in seconds
    private float playhead;

    // How fast it is playing, where 1 is normal speed
    private float speed = 1;

    //----------Event Loop----------\\

    /* UPDATE
     * 1 - Do nothing unless the replay is playing
     * 2 - Move the playhead on. This counts unscaled time, since the match is
     *     frozen while the replay is up
     * 3 - At the end, either wrap round to the beginning or sit on the last frame
     * 4 - Find the frame under the playhead and put both judokas into it
     */
    private void Update()
    {
        // 1
        if (!IsReplaying) return;

        // 2
        playhead += Time.unscaledDeltaTime * speed;

        // 3
        float duration = recorder.Duration;
        if (playhead > duration)
            playhead = (loop && duration > 0f) ? playhead % duration : duration;

        // 4
        Apply(recorder.GetFrame(recorder.IndexAtTime(playhead)));
    }

    //----------Public Functions----------\\

    /* BEGIN REPLAY
     * 1 - Do nothing if it is already playing, or nothing has been recorded
     * 2 - Take a snapshot of the frozen moment being interrupted, so the match
     *     can be put back exactly as it was
     * 3 - Switch the judokas' own drivers off, so the recorded frames are the
     *     only thing moving them
     * 4 - Rewind to the beginning and show the first frame
     */
    public void BeginReplay()
    {
        // 1
        if (IsReplaying || recorder.FrameCount == 0) return;

        // 2
        liveFrame = new ReplayFrame { tori = tori.Capture(), uke = uke.Capture() };

        // 3
        tori.enabled = false;
        uke.enabled = false;

        // 4
        playhead = 0f;
        IsReplaying = true;
        Apply(recorder.GetFrame(0));
    }

    /* END REPLAY
     * 1 - Do nothing if it was not playing
     * 2 - Give the judokas their own drivers back
     * 3 - Put everyone back in the moment the replay interrupted. Their springs
     *     and clocks were frozen the whole time, so they still agree with it
     */
    public void EndReplay()
    {
        // 1
        if (!IsReplaying) return;
        IsReplaying = false;

        // 2
        tori.enabled = true;
        uke.enabled = true;

        // 3
        Apply(liveFrame);
    }

    /* TOGGLE PLAY
     * 1 - Start the playback if it is stopped, or stop it if it is running
     */
    public void TogglePlay()
    {
        IsReplaying = !IsReplaying;
    }

    /* FAST FORWARD
     * 1 - Bring slow motion back to normal speed first
     * 2 - Or step normal speed up to double
     */
    public void FastForward()
    {
        if (speed < 1) speed = 1;
        else if (speed == 1) speed = 2;
    }

    /* SLOW DOWN
     * 1 - Bring fast forward back to normal speed first
     * 2 - Or step normal speed down to half
     */
    public void SlowDown()
    {
        if (speed > 1) speed = 1;
        else if (speed == 1) speed = 0.5f;
    }

    //----------Getters----------\\

    // The current speed written out for the screen to show, such as "x0.5"
    public string SpeedDisplay => "x" + speed.ToString();

    //----------Private Functions----------\\

    /* APPLY
     * 1 - Move both judokas' bodies into the recorded frame first
     * 2 - Only then place their arms and legs
     * The order matters. Limb targets are placed in world space, and a target
     * parented to a judoka needs both bodies already standing in this frame's
     * pose - otherwise it is measured against a body still in the last one and
     * gets dragged out of place when that body finally moves
     */
    private void Apply(ReplayFrame frame)
    {
        // 1
        tori.ApplyBody(frame.tori);
        uke.ApplyBody(frame.uke);

        // 2
        tori.ApplyLimbs(frame.tori);
        uke.ApplyLimbs(frame.uke);
    }
}
