using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScreen : MenuScreen
{
    //----------Variables----------\\
    //Screens
    [SerializeField] private MenuScreen controlsScreen;
    [SerializeField] private MenuScreen optionsScreen;
    [SerializeField] private MenuScreen techniquesScreen;

    //Loading Name
    [SerializeField] private string loadingSceneName = "Loading";

    //----------Button Functions----------\\
    public void OpenControls() => Manager.Push(controlsScreen);
    public void OpenOptions() => Manager.Push(optionsScreen);
    public void OpenTechniques() => Manager.Push(techniquesScreen);
    public void StartGame() => SceneManager.LoadScene(loadingSceneName);
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    //----------Override Functions----------\\
    //Override OnBack with nothing
    public override void OnBack() { }
}