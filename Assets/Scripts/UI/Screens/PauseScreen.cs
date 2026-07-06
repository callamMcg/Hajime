using UnityEngine;

public class PauseScreen : MenuScreen
{
    //----------Variables----------\\
    //References
    [SerializeField] private MenuScreen controlsScreen;
    [SerializeField] private MenuScreen techniquesScreen;
    [SerializeField] private MenuScreen replayScreen;

    //Bound controller
    private PauseController pauseController;
    public void BindController(PauseController pc) => pauseController = pc;

    //----------Button functions----------\\
    public void OpenControls() => Manager.Push(controlsScreen);
    public void OpenTechniques() => Manager.Push(techniquesScreen);
    public void OpenReplay() => Manager.Push(replayScreen);
    public void Resume() => pauseController.Resume();
    public void QuitToMenu() => pauseController.QuitToMenu();
    
    //----------Override functions----------\\
    public override void OnBack() => pauseController.Resume();
}