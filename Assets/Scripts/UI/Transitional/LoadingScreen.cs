using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    //----------Variables----------\\
    //Refernces
    [SerializeField] private Slider progressBar;
    [SerializeField] private string gameSceneName = "Game";

    //Polish
    [SerializeField] private float minDisplayTime = 0.5f;

    //Run async begin
    private void Start()
    {
        StartCoroutine(Begin());
    }

    /*Begin
     * 1 - Begin to load scene in background (do not activte)
     * 2 - whilst unfinished (or below min time)
     *  a - track elapsed time and loading progress
     *  b - update the progress bar to display lower value
     * 3 - Activate new scene
     */
    private IEnumerator Begin()
    {
        //1
        AsyncOperation op = SceneManager.LoadSceneAsync(gameSceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;

        //2
        while (op.progress < 0.9f || elapsed < minDisplayTime)
        {
            //a
            elapsed += Time.unscaledDeltaTime;

            float progress = op.progress / 0.9f;
            float minT = elapsed / minDisplayTime;

            //b
            if (progress < minT)
                progressBar.value = Mathf.Clamp01(progress);
            else
                progressBar.value = Mathf.Clamp01(minT);

            yield return null;
        }

        //3
        if (progressBar != null)
            progressBar.value = 1f;
        op.allowSceneActivation = true;
    }
}