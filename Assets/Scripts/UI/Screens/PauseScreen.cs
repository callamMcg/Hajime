using UnityEngine;

public class PauseScreen : MenuScreen
{
    [SerializeField] private MenuScreen controlsScreen;
    [SerializeField] private MenuScreen techniquesScreen;

    private PauseController pauseController;
    public void BindController(PauseController pc) => pauseController = pc;

    public void OpenControls() => Manager.Push(controlsScreen);
    public void OpenTechniques() => Manager.Push(techniquesScreen);

    public void Resume() => pauseController.Resume();
    public void QuitToMenu() => pauseController.QuitToMenu();

    // Back on the pause ROOT means resume, not pop into nothing.
    public override void OnBack() => pauseController.Resume();
}