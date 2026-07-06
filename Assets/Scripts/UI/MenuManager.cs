using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    //----------Variables----------\\
    //Opening screen
    [SerializeField] private MenuScreen rootScreen;
    [SerializeField] private bool openRootOnStart = true;

    //Stack of the screens
    private readonly Stack<MenuScreen> stack = new();

    //Tracker
    private bool transitioning;

    //----------Event Loop----------\\
    /*Start
     * If there is a root screen and starting in root open the root screen
     */
    private void Start()
    {
        if (openRootOnStart && rootScreen != null)
            Push(rootScreen);

        if (InputReader.Instance != null)
            InputReader.Instance.Cancel += Back;
    }

    /*On OnDisable
     * Remove Back from cancel button
     */
    private void OnDisable()
    {
        if (InputReader.Instance != null)
            InputReader.Instance.Cancel -= Back;
    }

    //----------Public Functions----------\\
    /*Push
     * Start pushing the screen
     */
    public void Push(MenuScreen screen) => StartCoroutine(PushRoutine(screen));

    /*Pop
     * Start popping the screen
     */
    public void Pop() => StartCoroutine(PopRoutine());

    /*Switch To
     * Start switching screens
     */
    public void SwitchTo(MenuScreen screen) => StartCoroutine(SwitchRoutine(screen));

    /*Close All
     * Start to close all screens
     */
    public void CloseAll() => StartCoroutine(CloseAllRoutine());

    /*Back
     * 1 - If there is nothing in the stack or transitioning do nothing
     * 2 - Else get the front screen and go back from there
     */
    public void Back()
    {
        if (transitioning || stack.Count == 0) return;
        stack.Peek().OnBack();
    }

    //----------Async Functions----------\\
    /*Push Routine
     * 1 - wait for current trainsition to stop then start this transition
     * 2 - Exit the current screen if there is one
     * 3 - Bind the screen with this manager
     * 4 - Add the screen to the stack
     * 5 - Wait for the fade into the new screen
     */
    private IEnumerator PushRoutine(MenuScreen screen)
    {
        //1
        if (transitioning) yield break;
        transitioning = true;
        //2
        if (stack.Count > 0 && screen != null)
            yield return stack.Peek().Exit(deactivate: true);
        //3
        screen.Bind(this);
        //4
        stack.Push(screen);
        //5
        yield return screen.Enter();
        transitioning = false;
    }

    /*Pop Routine
     * 1 - Wait for the current transition then start this transition
     * 2 - Exit the current screen and remove it from the stack
     * 3 - Wait for the entery of the screen ontop of the stack
     */
    private IEnumerator PopRoutine()
    {
        if (transitioning || stack.Count == 0) yield break;
        transitioning = true;

        yield return stack.Pop().Exit(deactivate: true);
        if (stack.Count > 0)
            yield return stack.Peek().Enter();

        transitioning = false;
    }

    /*Switch Routine
     * 1 - Wait for the current tranition and start this transition
     * 2 - Wait for the transition out of the current screen
     * 3 - Bind this manager to the new screen and push it to the stack
     * 4 - Wait to enter the new screen
     */
    private IEnumerator SwitchRoutine(MenuScreen screen)
    {
        //1
        if (transitioning) yield break;
        transitioning = true;
        //2
        while (stack.Count > 0)
            yield return stack.Pop().Exit(deactivate: true);
        //3
        screen.Bind(this);
        stack.Push(screen);
        //4
        yield return screen.Enter();
        transitioning = false;
    }

    /*Close All Routine
     * 1 - Wait for the current tranisiton then start this one
     * 2 - Wait for the fade of the current screen
     */
    private IEnumerator CloseAllRoutine()
    {
        //1
        if (transitioning) yield break;
        transitioning = true;
        //2
        while (stack.Count > 0)
            yield return stack.Pop().Exit(deactivate: true);

        transitioning = false;
    }
}