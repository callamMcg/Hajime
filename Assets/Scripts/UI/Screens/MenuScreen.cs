using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public abstract class MenuScreen : MonoBehaviour
{
    //----------Variables----------\\
    //The first button
    [SerializeField] private GameObject firstSelected;

    //How long it takes to fade in
    [SerializeField] private float fadeDuration = 0.2f;
    
    //The canvas group component
    private CanvasGroup group;
    
    //The manager script
    protected MenuManager Manager { get; private set; }
    public void Bind(MenuManager manager) => Manager = manager;

    //----------Event Loop----------\\
    /*Awake
     * 1 - Get the group and set alpha to 0
     * 2 - Make it so it cannot be interacted with
     * 3 - Hide it (it must be active when the scene opens)
     */
    protected virtual void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0;
        SetInteractable(false);
        gameObject.SetActive(false); 
    }

    //----------Public Async Functions----------\\
    /*Enter
     * 1 - Set active and call specific screen function
     * 2 - Wait for the fade in
     * 3 - Make it interactable and select the first button
     */
    public IEnumerator Enter()
    {
        //1
        gameObject.SetActive(true);
        OnEnter();
        //2
        yield return ScreenTransition.Fade(group, 0f, 1f, fadeDuration);
        //3
        SetInteractable(true);
        Select();
    }

    /*Exit
     * 1 - Make it none interactable
     * 2 - Wait for the fade out
     * 3 - Call the specific exit function
     * 4 - If deactivating, set active to false
     */
    public IEnumerator Exit(bool deactivate)
    {
        SetInteractable(false);
        yield return ScreenTransition.Fade(group, group.alpha, 0f, fadeDuration);
        OnExit();
        if (deactivate) gameObject.SetActive(false);
    }

    //----------Private Functions----------\\
    /*Select
     * 1 - Bug catch
     * 2 - Set the current button as first selected
     */
    private void Select()
    {
        //1
        if (firstSelected == null || EventSystem.current == null) return;
        //2
        EventSystem.current.SetSelectedGameObject(null);     
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    /*Set Interactable
     * 1 - Set the group interactable
     * 2 - Set the groups ray blocking
     */
    private void SetInteractable(bool on)
    {
        //1
        group.interactable = on;
        //2
        group.blocksRaycasts = on;
    }

    //----------Protected Functions----------\\
    protected virtual void OnEnter() { }
    protected virtual void OnExit() { }
    public virtual void OnBack() => Manager.Pop(); 
}