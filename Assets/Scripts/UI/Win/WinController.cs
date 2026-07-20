using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Watches for the throw and runs the end of the match: a beat to let the
/// fall land, then the fight is frozen exactly the way a pause freezes it,
/// and the win screen is pushed. The replay path runs under this freeze the
/// same as it does under the pause menu - timeScale is zero, so the recorder,
/// the springs and the clocks are all holding still.
/// </summary>
public class WinController : MonoBehaviour
{
    //----------Variables----------\\
    //References
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private WinScreen winScreen;
    [SerializeField] private Uke uke;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    //Stats
    [SerializeField] private float winDelay = 0.75f; // seconds to admire the ippon before the freeze

    //----------Event Loop----------\\
    /*On Enable / On Disable
     * 1 - Bind and unbind the throw, one subscription in and one out
     */
    private void OnEnable() { if (uke != null) uke.Thrown += OnThrown; }
    private void OnDisable() { if (uke != null) uke.Thrown -= OnThrown; }

    /*Start
     * 1 - Bind this controller to the win screen's buttons
     */
    private void Start() => winScreen.BindController(this);

    //----------Private Functions----------\\
    private void OnThrown() => StartCoroutine(WinRoutine());

    /*Win Routine
     * 1 - Let the moment breathe
     * 2 - Freeze the fight the same way the pause does
     * 3 - Push the win screen
     */
    private IEnumerator WinRoutine()
    {
        //1
        yield return new WaitForSeconds(winDelay);
        //2
        Time.timeScale = 0f;
        InputReader.Instance.EnableUI();
        //3
        menuManager.Push(winScreen);
    }

    //----------Public Functions----------\\
    /*Quit to menu
     * 1 - Reset timescale
     * 2 - Enable ui controls
     * 3 - Load menu scene
     */
    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        InputReader.Instance.EnableUI();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}