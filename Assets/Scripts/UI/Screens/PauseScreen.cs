using UnityEngine;

/// <summary>
/// The menu that comes up when the match is paused.
/// Most of its buttons simply open another screen on top of it, but resuming and
/// quitting are not its to do - the match is frozen and only the pause controller
/// knows how to unfreeze it properly. So this holds a handle on that controller
/// and passes those two through to it.
/// Backing out of this screen resumes, which is what makes the pause button work
/// as a toggle.
/// </summary>
public class PauseScreen : MenuScreen
{
    //----------Variables----------\\

    // The screen showing the controls
    [SerializeField] private MenuScreen controlsScreen;

    // The screen listing the techniques
    [SerializeField] private MenuScreen techniquesScreen;

    // The screen that plays the match back
    [SerializeField] private MenuScreen replayScreen;

    // The controller that actually froze the match, and knows how to unfreeze it
    private PauseController pauseController;

    //----------Public Functions----------\\

    /* BIND CONTROLLER
     * 1 - Take the controller that paused the match, so resuming and quitting
     *     can be handed back to it
     */
    public void BindController(PauseController pc) => pauseController = pc;

    //----------Button Functions----------\\

    /* OPEN CONTROLS
     * 1 - Show the controls on top of this screen
     */
    public void OpenControls() => Manager.Push(controlsScreen);

    /* OPEN TECHNIQUES
     * 1 - Show the technique list on top of this screen
     */
    public void OpenTechniques() => Manager.Push(techniquesScreen);

    /* OPEN REPLAY
     * 1 - Show the replay on top of this screen
     */
    public void OpenReplay() => Manager.Push(replayScreen);

    /* RESUME
     * 1 - Hand this to the controller, which closes the menu, gives the controls
     *     back to the match and starts time again
     */
    public void Resume() => pauseController.Resume();

    /* QUIT TO MENU
     * 1 - Hand this to the controller, which unfreezes time before leaving so
     *     the main menu does not arrive frozen
     */
    public void QuitToMenu() => pauseController.QuitToMenu();

    //----------Override Functions----------\\

    /* ON BACK
     * 1 - Backing out of the pause menu means carrying on with the match, which
     *     is what makes the pause button toggle
     */
    public override void OnBack() => pauseController.Resume();
}
