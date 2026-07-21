using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Watches for the throw that ends the match and runs everything that follows.
/// It waits a moment first, so the landing is allowed to play out and be seen
/// rather than being cut off by the menu appearing over it.
/// Then it freezes the match in exactly the same way pausing does. That matters:
/// with time stopped, the recorder stops recording, the judokas stop moving and
/// their balance springs stop unwinding, which is what lets the replay run over
/// the top of a match that is being held perfectly still.
/// </summary>
public class WinController : MonoBehaviour
{
    //----------Variables----------\\

    // The manager that brings the win screen up
    [SerializeField] private MenuManager menuManager;

    // The screen shown once the match is won
    [SerializeField] private WinScreen winScreen;

    // The judoka being thrown, watched for the landing that ends the match
    [SerializeField] private Uke uke;

    // The menu loaded when leaving the match
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // How long the landing is left to play out before the match is frozen
    [SerializeField] private float winDelay = 0.75f;

    //----------Event Loop----------\\

    /* ON ENABLE
     * 1 - Start listening for uke hitting the mat
     */
    private void OnEnable()
    {
        if (uke != null) uke.Thrown += OnThrown;
    }

    /* ON DISABLE
     * 1 - Stop listening, so this is not still responding once it is gone
     */
    private void OnDisable()
    {
        if (uke != null) uke.Thrown -= OnThrown;
    }

    /* START
     * 1 - Tell the win screen this is its controller, so its buttons can hand
     *     quitting back here
     */
    private void Start() => winScreen.BindController(this);

    //----------Private Functions----------\\

    /* ON THROWN
     * 1 - Uke has landed, so start the end of the match
     */
    private void OnThrown() => StartCoroutine(WinRoutine());

    //----------Async Functions----------\\

    /* WIN ROUTINE
     * 1 - Let the landing play out for a moment, so it can actually be seen
     * 2 - Freeze the match exactly as pausing would, and hand the controls to
     *     the menu
     * 3 - Bring the win screen up
     */
    private IEnumerator WinRoutine()
    {
        // 1
        yield return new WaitForSeconds(winDelay);

        // 2
        Time.timeScale = 0f;
        InputReader.Instance.EnableUI();

        // 3
        menuManager.Push(winScreen);
    }

    //----------Public Functions----------\\

    /* QUIT TO MENU
     * 1 - Start time again. It survives a scene load, so without this the menu
     *     would arrive frozen
     * 2 - Make sure the controls belong to the menu
     * 3 - Leave the match for the main menu
     */
    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        InputReader.Instance.EnableUI();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
