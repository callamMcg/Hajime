using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a rolling window of the last few seconds of the fight.
/// Once per frame, after every other system has finished writing, it captures
/// both judokas into a ReplayFrame and stamps it with the game clock.
/// It only records while game time is moving, so pausing freezes the buffer
/// automatically - the replay can never record itself.
/// </summary>

// Run after JudokaBody and FootManager's LateUpdates, so a frame is only
// captured once every writer has finished with it
[DefaultExecutionOrder(1000)]
public class ReplayRecorder : MonoBehaviour
{
    //------------------Variables------------------//
    // References
    [SerializeField] private Judoka tori;
    [SerializeField] private Judoka uke;

    // Stats
    [SerializeField] private float recordSeconds = 30f; // length of the rolling window

    // The buffer, oldest frame first
    private readonly List<ReplayFrame> frames = new();

    //------------------Unity Functions------------------//
    /* LATE UPDATE - the capture
     * 1 - Only record while game time is moving, so pause (and therefore
     *     the replay itself) leaves the buffer untouched
     * 2 - Capture both judokas and stamp the frame with the game clock
     * 3 - Drop frames that have fallen out of the rolling window
     */
    private void LateUpdate()
    {
        // 1
        if (Time.deltaTime <= 0f) return;

        // 2
        frames.Add(new ReplayFrame
        {
            time = Time.time,
            tori = tori.Capture(),
            uke = uke.Capture(),
        });

        // 3
        while (frames.Count > 1 && frames[^1].time - frames[0].time > recordSeconds)
            frames.RemoveAt(0);
    }

    //------------------Public Functions------------------//
    // Frame access, index 0 is the oldest
    public int FrameCount => frames.Count;
    public ReplayFrame GetFrame(int index) => frames[index];
    public ReplayFrame Latest => frames[^1];

    // Seconds of footage held
    public float Duration => frames.Count < 2 ? 0f : frames[^1].time - frames[0].time;

    /* INDEX AT TIME
     * Map seconds-from-the-start-of-the-buffer to a frame index.
     * Binary search for the last frame at or before that moment,
     * clamped to the ends of the buffer.
     * (Callers guard the empty buffer before playing.)
     */
    public int IndexAtTime(float seconds)
    {
        if (frames.Count == 0) return 0;
        float target = frames[0].time + seconds;

        int low = 0, high = frames.Count - 1;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (frames[mid].time <= target) low = mid;
            else high = mid - 1;
        }
        return low;
    }

    public void Clear() => frames.Clear();
}