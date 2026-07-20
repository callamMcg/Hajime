using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuScreen : MenuScreen
{
    //----------Variables----------\\
    //Screens
    [SerializeField] private MenuScreen controlsScreen;
    [SerializeField] private MenuScreen optionsScreen;
    [SerializeField] private MenuScreen techniquesScreen;

    //Loading Name
    [SerializeField] private string loadingSceneName = "Loading";

    //Research code - revealed once the application has been open long enough
    [SerializeField] private TMP_Text researchCodeLabel;
    [SerializeField] private float unlockMinutes = 30f;
    [SerializeField] private string researchCode = "HajimeResearch";

    //Null until the label has been written once, so the first refresh always applies
    private bool? shown;

    //----------Button Functions----------\\
    public void OpenControls() => Manager.Push(controlsScreen);
    public void OpenOptions() => Manager.Push(optionsScreen);
    public void OpenTechniques() { Manager.Push(techniquesScreen); Debug.Log(techniquesScreen); }
    public void StartGame() => SceneManager.LoadScene(loadingSceneName);
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    //----------Event Loop----------\\
    //The screen object is only active while the menu is showing, so this is a
    //cheap poll that reveals the code the moment the total passes the unlock time
    private void Update() => RefreshResearchCode();

    //----------Override Functions----------\\
    //Show or hide the code as the menu comes up
    protected override void OnEnter() => RefreshResearchCode();

    //Override OnBack with nothing
    public override void OnBack() { }

    //----------Private Functions----------\\
    /*Refresh Research Code
     * 1 - Nothing wired, nothing to do
     * 2 - Unlocked once the tracked application time passes the unlock time
     * 3 - Only touch the label when the state actually changes
     */
    private void RefreshResearchCode()
    {
        //1
        if (researchCodeLabel == null) return;
        //2
        bool unlocked = PlaytimeTracker.Instance != null
                     && PlaytimeTracker.Instance.Reached(unlockMinutes * 60f);
        //3
        if (shown == unlocked) return;
        shown = unlocked;
        researchCodeLabel.text = researchCode;
        researchCodeLabel.gameObject.SetActive(unlocked);
    }
}