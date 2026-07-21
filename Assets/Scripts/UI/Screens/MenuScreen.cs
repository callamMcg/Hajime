using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The base every menu screen is built on.
/// It handles the parts they all share: fading in and out, being switched on and
/// off, and refusing to be clicked while it is still fading so a button cannot
/// be pressed on a screen that is halfway gone.
/// It also puts the cursor on a sensible first button as it opens, so the menu
/// can be driven on a pad without needing a mouse.
/// Screens inherit from this and fill in what they do when they open, when they
/// close, and what backing out of them should mean.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class MenuScreen : MonoBehaviour
{
    //----------Variables----------\\

    // The button the cursor lands on when this screen opens
    [SerializeField] private GameObject firstSelected;

    // How long this screen takes to fade in or out
    [SerializeField] private float fadeDuration = 0.2f;

    // What actually fades, and what blocks clicks while it does
    private CanvasGroup group;

    // The manager that opens and closes this screen, so it can ask for moves itself
    protected MenuManager Manager { get; private set; }

    //----------Public Functions----------\\

    /* BIND
     * 1 - Take the manager that is looking after this screen, so it can ask to
     *     open or close others
     */
    public void Bind(MenuManager manager) => Manager = manager;

    //----------Event Loop----------\\

    /* AWAKE
     * 1 - Find what does the fading
     * 2 - Start fully invisible and unclickable
     * 3 - Switch the screen off. It has to be left switched on in the scene so
     *     it can be found and set up, so it hides itself here instead
     */
    protected virtual void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0;
        SetInteractable(false);
        gameObject.SetActive(false);
    }

    //----------Public Async Functions----------\\

    /* ENTER
     * 1 - Switch the screen on and let it do whatever it needs on opening
     * 2 - Fade it in
     * 3 - Only once it is fully there, allow it to be clicked and put the cursor
     *     on its first button
     */
    public IEnumerator Enter()
    {
        // 1
        gameObject.SetActive(true);
        OnEnter();

        // 2
        yield return ScreenTransition.Fade(group, 0f, 1f, fadeDuration);

        // 3
        SetInteractable(true);
        Select();
    }

    /* EXIT
     * 1 - Stop it being clickable straight away, so nothing can be pressed on
     *     the way out
     * 2 - Fade it away
     * 3 - Let it do whatever it needs on closing
     * 4 - Switch it off, if we were asked to
     */
    public IEnumerator Exit(bool deactivate)
    {
        // 1
        SetInteractable(false);

        // 2
        yield return ScreenTransition.Fade(group, group.alpha, 0f, fadeDuration);

        // 3
        OnExit();

        // 4
        if (deactivate) gameObject.SetActive(false);
    }

    //----------Private Functions----------\\

    /* SELECT
     * 1 - Do nothing if this screen has no first button, or there is nothing
     *     driving the cursor
     * 2 - Clear the cursor and put it on this screen's first button. It is
     *     cleared first so it still moves even if that button was already the
     *     one selected
     */
    private void Select()
    {
        // 1
        if (firstSelected == null || EventSystem.current == null) return;

        // 2
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    /* SET INTERACTABLE
     * 1 - Turn this screen's buttons on or off
     * 2 - Turn clicks passing through it on or off with them
     */
    private void SetInteractable(bool on)
    {
        // 1
        group.interactable = on;

        // 2
        group.blocksRaycasts = on;
    }

    //----------Protected Functions----------\\

    /* ON ENTER
     * Filled in by a screen that needs to set itself up as it opens
     */
    protected virtual void OnEnter() { }

    /* ON EXIT
     * Filled in by a screen that needs to tidy up after itself as it closes
     */
    protected virtual void OnExit() { }

    /* ON BACK
     * What backing out of this screen means. By default it simply closes and
     * returns to whatever was underneath, but a screen with nothing behind it
     * can override this to do nothing at all
     */
    public virtual void OnBack() => Manager.Pop();
}
