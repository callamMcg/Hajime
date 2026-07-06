using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private PauseScreen pauseScreen;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public bool IsPaused { get; private set; }

    private bool pauseBound = false;

    private void OnEnable()
    {
        if (InputReader.Instance != null)
        {
            pauseBound = true;
            InputReader.Instance.Pause += TogglePause; 
        }
    }

    private void OnDisable()
    {
        if (InputReader.Instance != null)
            InputReader.Instance.Pause -= TogglePause;
    }

    private void Start()
    {
        if(!pauseBound)
            InputReader.Instance.Pause += TogglePause;
        InputReader.Instance.EnableGameplay();
        pauseScreen.BindController(this);
    }

    private void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        Time.timeScale = 0f;
        InputReader.Instance.EnableUI();   // Cancel/Back now live; Pause action goes quiet
        menuManager.Push(pauseScreen);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        menuManager.CloseAll();            // unwind pause + any sub-screens
        InputReader.Instance.EnableGameplay();
        Time.timeScale = 1f;
    }

    public void QuitToMenu()
    {
        // Restore timescale BEFORE leaving, or the menu scene loads frozen.
        Time.timeScale = 1f;
        IsPaused = false;
        InputReader.Instance.EnableUI();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}