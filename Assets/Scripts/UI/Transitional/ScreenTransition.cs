using System.Collections;
using UnityEngine;

/// <summary>
/// The fade every menu screen uses to come and go.
/// It is kept here on its own so each screen does not carry its own copy, and so
/// they all fade at the same rate and in the same way.
/// It runs on unscaled time deliberately: the menu is usually open while the game
/// is frozen, and a fade measured in game time would simply never move.
/// </summary>
public static class ScreenTransition
{
    //----------Public Async Functions----------\\

    /* FADE
     * 1 - If there is no duration to fade over, jump straight to the target and
     *     finish
     * 2 - Otherwise start at the beginning and note how long we have been going
     * 3 - Ease from one to the other across the duration, a frame at a time,
     *     counting in unscaled time so a frozen game still fades
     * 4 - Land exactly on the target, so rounding cannot leave it slightly short
     */
    public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        // 1
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        // 2
        float t = 0;
        group.alpha = from;

        // 3
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        // 4
        group.alpha = to;
    }
}
