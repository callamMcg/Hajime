using UnityEngine;

public class ReplayController : MonoBehaviour
{
    //------------------Variables------------------//
    // References
    [SerializeField] private ReplayRecorder recorder;
    [SerializeField] private Judoka tori;
    [SerializeField] private Judoka uke;

    // Stats
    [SerializeField] private bool loop = true;

    // Trackers
    public bool IsReplaying { get; private set; }
    private ReplayFrame liveFrame; // the paused moment, restored on exit
    private float playhead;        // seconds from the start of the buffer

    //------------------Unity Functions------------------//
    /* UPDATE - the playback
     * 1 - Advance the playhead on the unscaled clock, the replay runs under pause
     * 2 - At the end of the buffer, wrap round or hold the final frame
     * 3 - Apply the frame beneath the playhead to both judokas
     */
    private void Update()
    {
        if (!IsReplaying) return;

        // 1
        playhead += Time.unscaledDeltaTime;

        // 2
        float duration = recorder.Duration;
        if (playhead > duration)
            playhead = (loop && duration > 0f) ? playhead % duration : duration;

        // 3
        Apply(recorder.GetFrame(recorder.IndexAtTime(playhead)));
    }

    //------------------Public Functions------------------//
    /* BEGIN REPLAY
     * 1 - Nothing to play, or already playing - do nothing
     * 2 - Capture the paused moment so the fight can be put back exactly
     * 3 - Disable the judoka drivers - the recorded frames must be the only
     *     thing writing poses while the replay runs
     * 4 - Rewind to the start of the buffer and show the first frame
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
     * 1 - If not replaying do nothing
     * 2 - Hand control back to the judoka drivers
     * 3 - Restore the paused moment - the springs and clocks were frozen
     *     throughout, so they still agree with this pose
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

    //------------------Private Functions------------------//
    /* APPLY
     * Push one frame through both judokas, bodies first, limbs second.
     * Limb targets are placed in world space, so a target parented to either
     * judoka must find both roots already standing in this frame's pose -
     * otherwise its new local offset is computed against a stale parent and
     * it is dragged out of place when the root moves.
     */
    private void Apply(ReplayFrame frame)
    {
        tori.ApplyBody(frame.tori);
        uke.ApplyBody(frame.uke);
        tori.ApplyLimbs(frame.tori);
        uke.ApplyLimbs(frame.uke);
    }
}