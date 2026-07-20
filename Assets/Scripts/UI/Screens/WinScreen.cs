using UnityEngine;

public class WinScreen : MenuScreen
{
    //----------Variables----------\\
    //References
    [SerializeField] private MenuScreen replayScreen;

    //Bound controller
    private WinController winController;
    public void BindController(WinController wc) => winController = wc;

    //----------Button Functions----------\\
    public void OpenReplay() => Manager.Push(replayScreen);
    public void QuitToMenu() => winController.QuitToMenu();

    //----------Override Functions----------\\
    //The match is over - back has nothing to resume
    public override void OnBack() { }
}