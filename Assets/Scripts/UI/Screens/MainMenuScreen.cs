using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// The main menu - the way into the match and into everything around it.
/// Its buttons either open another screen on top of this one or leave the menu
/// entirely, by loading the match or closing the game.
/// It also holds the research code, which stays hidden until the application has
/// been open for long enough in total. That total is kept between sessions, so
/// the time builds up across however many times the game is played rather than
/// having to be earned in one sitting.
/// </summary>
public class MainMenuScreen : MenuScreen
{
    //----------Variables----------\\

    // The screen showing the controls
    [SerializeField] private MenuScreen controlsScreen;

    // The screen holding the settings
    [SerializeField] private MenuScreen optionsScreen;

    // The screen listing the techniques
    [SerializeField] private MenuScreen techniquesScreen;

    // The loading screen, which then brings the match in behind it
    [SerializeField] private string loadingSceneName = "Loading";

    // The label the research code appears in. Leave it switched off in the
    // scene - this script decides when it shows
    [SerializeField] private TMP_Text researchCodeLabel;

    // How long the application has to have been open, in total, to earn the code
    [SerializeField] private float unlockMinutes = 30f;

    // The code itself
    [SerializeField] private string researchCode = "HajimeResearch";

    // Whether the code is currently showing. Left unset until the first check,
    // so that first check always writes the label whichever way it goes
    private bool? shown;

    //----------Event Loop----------\\

    /* UPDATE
     * 1 - Keep checking whether the code has been earned yet
     * This screen object is only switched on while the menu is actually showing,
     * so this costs nothing the rest of the time, and it means the code can
     * appear while the player is sat on the menu rather than only after leaving
     * and coming back
     */
    private void Update() => RefreshResearchCode();

    //----------Button Functions----------\\

    /* OPEN CONTROLS
     * 1 - Show the controls on top of this screen
     */
    public void OpenControls() => Manager.Push(controlsScreen);

    /* OPEN OPTIONS
     * 1 - Show the settings on top of this screen
     */
    public void OpenOptions() => Manager.Push(optionsScreen);

    /* OPEN TECHNIQUES
     * 1 - Show the technique list on top of this screen
     */
    public void OpenTechniques() { Manager.Push(techniquesScreen); Debug.Log(techniquesScreen); }

    /* START GAME
     * 1 - Leave the menu for the loading screen, which brings the match in
     */
    public void StartGame() => SceneManager.LoadScene(loadingSceneName);

    /* QUIT GAME
     * 1 - Close the game
     * 2 - In the editor there is nothing to close, so stop playing instead
     */
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    //----------Override Functions----------\\

    /* ON ENTER
     * 1 - Check the code as the menu comes up, so it is right immediately
     */
    protected override void OnEnter() => RefreshResearchCode();

    /* ON BACK
     * There is nothing behind the main menu to go back to, so backing out here
     * deliberately does nothing
     */
    public override void OnBack() { }

    //----------Private Functions----------\\

    /* REFRESH RESEARCH CODE
     * 1 - Do nothing if no label has been set up to show the code in
     * 2 - Work out whether the total time in the application has passed the
     *     amount needed to earn it
     * 3 - Do nothing further unless that answer has actually changed, so the
     *     label is only ever written when it needs to be
     * 4 - Write the code in and show or hide the label to match
     */
    private void RefreshResearchCode()
    {
        // 1
        if (researchCodeLabel == null) return;

        // 2
        bool unlocked = PlaytimeTracker.Instance != null
                     && PlaytimeTracker.Instance.Reached(unlockMinutes * 60f);

        // 3
        if (shown == unlocked) return;

        // 4
        shown = unlocked;
        researchCodeLabel.text = researchCode;
        researchCodeLabel.gameObject.SetActive(unlocked);
    }
}
