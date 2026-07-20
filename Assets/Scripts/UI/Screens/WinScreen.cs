using UnityEngine;
using UnityEngine.SceneManagement;

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

    /*Restart Game
     * 1 - Undo the win freeze: restore time and hand control back to gameplay
     * 2 - Reload the current scene
     */
    public void RestartGame()
    {
        Time.timeScale = 1f;
        InputReader.Instance.EnableGameplay();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    //----------Override Functions----------\\
    //The match is over - back has nothing to resume
    public override void OnBack() { }
}