using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of which menu screen is showing and moves between them.
/// Screens are kept in a stack, so opening one from another remembers where you
/// came from and backing out returns you there. Every move fades the old screen
/// out before the new one comes in, and only one move is allowed to be running
/// at a time - otherwise two fades could overlap and leave two screens visible
/// or none at all.
/// It also listens for the cancel button, and hands that to whichever screen is
/// on top so each one decides for itself what backing out means.
/// </summary>
public class MenuManager : MonoBehaviour
{
    //----------Variables----------\\

    // The screen the menu opens on
    [SerializeField] private MenuScreen rootScreen;

    // Whether to open that screen automatically when the scene starts
    [SerializeField] private bool openRootOnStart = true;

    // Every screen currently open, most recent on top
    private readonly Stack<MenuScreen> stack = new();

    // True while a fade is running, which blocks any other move from starting
    private bool transitioning;

    //----------Event Loop----------\\

    /* START
     * 1 - Open the first screen, if there is one and we were asked to
     * 2 - Start listening for the cancel button so it can back out of screens
     */
    private void Start()
    {
        if (openRootOnStart && rootScreen != null)
            Push(rootScreen);

        if (InputReader.Instance != null)
            InputReader.Instance.Cancel += Back;
    }

    /* ON DISABLE
     * 1 - Stop listening for the cancel button, so this does not keep
     *     responding once it is gone
     */
    private void OnDisable()
    {
        if (InputReader.Instance != null)
            InputReader.Instance.Cancel -= Back;
    }

    //----------Public Functions----------\\

    /* PUSH
     * 1 - Open a screen on top of whatever is already showing, remembering the
     *     one underneath so it can be come back to
     */
    public void Push(MenuScreen screen) => StartCoroutine(PushRoutine(screen));

    /* POP
     * 1 - Close the screen on top and return to the one underneath
     */
    public void Pop() => StartCoroutine(PopRoutine());

    /* SWITCH TO
     * 1 - Close everything and open one screen in its place, with nothing left
     *     underneath to go back to
     */
    public void SwitchTo(MenuScreen screen) => StartCoroutine(SwitchRoutine(screen));

    /* CLOSE ALL
     * 1 - Close every screen and leave the menu showing nothing at all
     */
    public void CloseAll() => StartCoroutine(CloseAllRoutine());

    /* BACK
     * 1 - Ignore this while a fade is running, or if nothing is open
     * 2 - Otherwise hand it to the screen on top and let it decide what backing
     *     out of itself means
     */
    public void Back()
    {
        if (transitioning || stack.Count == 0) return;

        stack.Peek().OnBack();
    }

    //----------Async Functions----------\\

    /* PUSH ROUTINE
     * 1 - Refuse to start if another move is already running
     * 2 - Fade out whatever is currently showing and switch it off
     * 3 - Tell the new screen this is its manager, so it can ask for moves itself
     * 4 - Remember it as the one now on top
     * 5 - Fade it in, then let other moves happen again
     */
    private IEnumerator PushRoutine(MenuScreen screen)
    {
        // 1
        if (transitioning) yield break;
        transitioning = true;

        // 2
        if (stack.Count > 0 && screen != null)
            yield return stack.Peek().Exit(deactivate: true);

        // 3
        screen.Bind(this);

        // 4
        stack.Push(screen);

        // 5
        yield return screen.Enter();
        transitioning = false;
    }

    /* POP ROUTINE
     * 1 - Refuse to start if another move is running, or if nothing is open
     * 2 - Fade out the screen on top and forget it
     * 3 - Fade the one underneath back in, if there is one
     */
    private IEnumerator PopRoutine()
    {
        // 1
        if (transitioning || stack.Count == 0) yield break;
        transitioning = true;

        // 2
        yield return stack.Pop().Exit(deactivate: true);

        // 3
        if (stack.Count > 0)
            yield return stack.Peek().Enter();

        transitioning = false;
    }

    /* SWITCH ROUTINE
     * 1 - Refuse to start if another move is running
     * 2 - Fade out and forget everything that was open, so there is nothing left
     *     to go back to
     * 3 - Take the new screen on and remember it as the only one
     * 4 - Fade it in
     */
    private IEnumerator SwitchRoutine(MenuScreen screen)
    {
        // 1
        if (transitioning) yield break;
        transitioning = true;

        // 2
        while (stack.Count > 0)
            yield return stack.Pop().Exit(deactivate: true);

        // 3
        screen.Bind(this);
        stack.Push(screen);

        // 4
        yield return screen.Enter();
        transitioning = false;
    }

    /* CLOSE ALL ROUTINE
     * 1 - Refuse to start if another move is running
     * 2 - Fade out and forget every open screen, leaving the menu showing nothing
     */
    private IEnumerator CloseAllRoutine()
    {
        // 1
        if (transitioning) yield break;
        transitioning = true;

        // 2
        while (stack.Count > 0)
            yield return stack.Pop().Exit(deactivate: true);

        transitioning = false;
    }
}
