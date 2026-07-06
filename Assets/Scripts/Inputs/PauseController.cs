using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    //----------Variables----------\\
    //References
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private PauseScreen pauseScreen;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    //Trackers 
    public bool IsPaused { get; private set; }
    private bool pauseBound = false; //Engine testing 

    //----------Event Loop----------\\
    /*On Enable
     * 1 - bind pause with toggle pause
     */
    private void OnEnable()
    {
        if (InputReader.Instance != null)
        {
            pauseBound = true;
            InputReader.Instance.Pause += TogglePause; 
        }
    }

    /*On Disable
     * 1 - unbind pause with toggle pause
     */
    private void OnDisable()
    {
        if (InputReader.Instance != null)
            InputReader.Instance.Pause -= TogglePause;
    }

    /*On Start
     * 1 - Enable gameplay and bind this controller to the pause screen
     */
    private void Start()
    {
        //Engine testing
        if (!pauseBound) 
        {
            InputReader.Instance.Pause += TogglePause;
            pauseBound = true;
        }
        //End engine testing

        InputReader.Instance.EnableGameplay();
        pauseScreen.BindController(this);
    }

    //----------Private functions----------\\
    /*Toggle Pause
     * 1 - If paused resume else pause
     */
    private void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    /*Pause
     * 1 - If paused do nothing
     * 2 - Stop time and enable ui controls
     * 3 - Push the pause screen
     */
    public void Pause()
    {
        //1
        if (IsPaused) return;
        IsPaused = true;
        //2
        Time.timeScale = 0f;
        InputReader.Instance.EnableUI();   
        //3
        menuManager.Push(pauseScreen);
    }

    /*Resume
     * 1 - If not paused do nothing
     * 2 - Remove all the menus
     * 3 - Enable gameplay controls and reset time
     */
    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        menuManager.CloseAll();            
        InputReader.Instance.EnableGameplay();
        Time.timeScale = 1f;
    }

    /*Quit to menu
     * 1 - Reset timescale
     * 2 - Enable ui controls
     * 3 - Load menu scene
     */
    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        InputReader.Instance.EnableUI();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}