using System.Collections;
using UnityEngine;

public static class ScreenTransition
{
    /*Fade
     * 1 - Set the alpha = to if no duration
     * 2 - Set the alpha to from and initialise tracker
     * 3 - Lerp the alpha from -> to using t / duration
     * 4 - Confirm alpha ends as to
     */
    public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        //1
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }
        //2
        float t = 0;
        group.alpha = from;
        //3
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        //4
        group.alpha = to;
    }
}