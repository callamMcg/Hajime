using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a rolling record of the last few seconds of the match.
/// Once a frame it takes a snapshot of both judokas and stamps it with the time,
/// throwing away anything that has fallen out of the back of the window. That way
/// the replay always has the recent past to show without the recording growing
/// forever.
/// It only records while match time is actually moving, which means pausing
/// freezes the recording automatically - and since the replay itself runs under
/// that freeze, the replay can never end up recording itself.
/// It runs last of everything, so each frame it captures is the finished article
/// rather than one caught halfway through being assembled.
/// </summary>
[DefaultExecutionOrder(1000)]
public class ReplayRecorder : MonoBehaviour
{
    //----------Variables----------\\

    // The judoka doing the throwing
    [SerializeField] private Judoka tori;

    // The judoka being thrown
    [SerializeField] private Judoka uke;

    // How many seconds of the match to keep hold of
    [SerializeField] private float recordSeconds = 30f;

    // Every frame currently held, oldest first
    private readonly List<ReplayFrame> frames = new();

    //----------Event Loop----------\\

    /* LATE UPDATE
     * 1 - Only record while match time is moving. This is what makes pausing
     *     freeze the recording, and stops the replay recording itself
     * 2 - Take a snapshot of both judokas and stamp it with the time
     * 3 - Drop anything that has fallen out of the back of the window, so the
     *     recording stays the same length rather than growing forever
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

    //----------Public Functions----------\\

    /* INDEX AT TIME
     * Turns a number of seconds into the frame that was showing at that moment.
     * 1 - Give back the start if nothing has been recorded yet
     * 2 - Work out the actual moment being asked for, counted from the beginning
     *     of what is held
     * 3 - Search for the last frame at or before it, halving the range each time
     *     rather than walking the whole recording
     * Callers check the recording is not empty before playing anything
     */
    public int IndexAtTime(float seconds)
    {
        // 1
        if (frames.Count == 0) return 0;

        // 2
        float target = frames[0].time + seconds;

        // 3
        int low = 0, high = frames.Count - 1;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (frames[mid].time <= target) low = mid;
            else high = mid - 1;
        }
        return low;
    }

    /* CLEAR
     * 1 - Throw the whole recording away
     */
    public void Clear() => frames.Clear();

    //----------Getters----------\\

    // How many frames are currently held
    public int FrameCount => frames.Count;

    // A particular frame, counting from the oldest
    public ReplayFrame GetFrame(int index) => frames[index];

    // The most recent frame recorded
    public ReplayFrame Latest => frames[^1];

    // How many seconds of match are currently held
    public float Duration => frames.Count < 2 ? 0 : frames[^1].time - frames[0].time;
}
