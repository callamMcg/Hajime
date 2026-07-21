using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The screen shown while the match is being loaded.
/// It starts the match loading in the background but holds it back from actually
/// appearing until two things are true: the load has finished, and the screen has
/// been up for a moment. That second condition is deliberate - a fast load would
/// otherwise flash this screen up and rip it away again, which reads as a glitch.
/// The bar shows whichever of those two is further behind, so it never races to
/// full and then sits there waiting.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    //----------Variables----------\\

    // The bar that fills as the match loads
    [SerializeField] private Slider progressBar;

    // The scene being loaded
    [SerializeField] private string gameSceneName = "Game";

    // The shortest time this screen is allowed to be up for, so a quick load
    // does not flash past
    [SerializeField] private float minDisplayTime = 0.5f;

    //----------Event Loop----------\\

    /* START
     * 1 - Begin loading as soon as the screen appears
     */
    private void Start()
    {
        StartCoroutine(Begin());
    }

    //----------Async Functions----------\\

    /* BEGIN
     * 1 - Start loading the match in the background, but forbid it from
     *     appearing until we say so
     * 2 - Keep going until the load has finished and the screen has been up long
     *     enough, whichever takes longer
     *   a - Track how long we have been waiting, and how far the load has got.
     *       Unity calls a load done at 0.9, so that is scaled back up to a full
     *       bar's worth
     *   b - Show whichever of the two is further behind, so the bar always
     *       reflects the thing still being waited on
     * 3 - Fill the bar and let the match appear
     */
    private IEnumerator Begin()
    {
        // 1
        AsyncOperation op = SceneManager.LoadSceneAsync(gameSceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;

        // 2
        while (op.progress < 0.9f || elapsed < minDisplayTime)
        {
            // a
            elapsed += Time.unscaledDeltaTime;

            float progress = op.progress / 0.9f;
            float minT = elapsed / minDisplayTime;

            // b
            if (progress < minT)
                progressBar.value = Mathf.Clamp01(progress);
            else
                progressBar.value = Mathf.Clamp01(minT);

            yield return null;
        }

        // 3
        if (progressBar != null)
            progressBar.value = 1f;

        op.allowSceneActivation = true;
    }
}
