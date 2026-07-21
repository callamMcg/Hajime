using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The screen shown once the match has been won.
/// From here the match can be watched back, restarted, or left for the main menu.
/// Quitting is handed to the win controller, which knows how to unfreeze the
/// match before leaving it. Restarting is done here, but has to undo that freeze
/// itself - time keeps whatever value it had across a scene load, so a reload
/// without restoring it would bring the match back frozen solid.
/// There is nothing to resume, so backing out of this screen does nothing.
/// </summary>
public class WinScreen : MenuScreen
{
    //----------Variables----------\\

    // The screen that plays the match back
    [SerializeField] private MenuScreen replayScreen;

    // The controller that ended the match, and knows how to unfreeze it
    private WinController winController;

    //----------Public Functions----------\\

    /* BIND CONTROLLER
     * 1 - Take the controller that ended the match, so quitting can be handed
     *     back to it
     */
    public void BindController(WinController wc) => winController = wc;

    //----------Button Functions----------\\

    /* OPEN REPLAY
     * 1 - Show the replay on top of this screen
     */
    public void OpenReplay() => Manager.Push(replayScreen);

    /* QUIT TO MENU
     * 1 - Hand this to the controller, which unfreezes time before leaving so
     *     the main menu does not arrive frozen
     */
    public void QuitToMenu() => winController.QuitToMenu();

    /* RESTART GAME
     * 1 - Start time again. It survives a scene load, so without this the match
     *     would come back frozen exactly as the win screen left it
     * 2 - Hand the controls back to the match, since the menu had them
     * 3 - Load the match again from the beginning
     */
    public void RestartGame()
    {
        Time.timeScale = 1f;
        InputReader.Instance.EnableGameplay();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    //----------Override Functions----------\\

    /* ON BACK
     * The match is over, so there is nothing to go back to. Backing out here
     * deliberately does nothing
     */
    public override void OnBack() { }
}
