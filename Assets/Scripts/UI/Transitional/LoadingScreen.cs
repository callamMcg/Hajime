using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private float minDisplayTime = 0.5f;

    [SerializeField] private string gameSceneName = "Game";

    private void Start()
    {
        StartCoroutine(Begin());
    }

    private IEnumerator Begin()
    {
        string target = gameSceneName;

        AsyncOperation op = SceneManager.LoadSceneAsync(target);
        op.allowSceneActivation = false;

        float elapsed = 0f;

        while (op.progress < 0.9f || elapsed < minDisplayTime)
        {
            elapsed += Time.unscaledDeltaTime;
            if (progressBar != null)
            {
                float progress = op.progress / 0.9f;
                float minT = elapsed / minDisplayTime;

                if(progress < minT)
                    progressBar.value = Mathf.Clamp01(progress);
                else
                    progressBar.value = Mathf.Clamp01(minT);

            }
            yield return null;
        }

        if (progressBar != null)
            progressBar.value = 1f;

        op.allowSceneActivation = true;
    }
}