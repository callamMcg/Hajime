using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    //----------Variables----------\\
    //Singleton
    public static InputReader Instance { get; private set; }

    //Actions other scripts subscribe to
    public event Action Pause;   // from gameplay
    public event Action Cancel;  // from ui

    //Control maps
    private GameControls controls;

    //----------Event Loop----------\\
    /*Awake
     * 1 - Establish singleton
     * 2 - Create control map and bind initial actions
     * 3 - Initialise within the ui map
     */
    private void Awake()
    {
        //1
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //2
        controls = new GameControls();
        controls.Gameplay.Pause.performed += OnPause;
        controls.UI.Cancel.performed += OnCancel;

        //3
        EnableUI(); 
    }

    /*On Destroy
     * 1 - IF not singleton just destory
     * 2 - Unbind actions and remove singleton reference
     */
    private void OnDestroy()
    {
        //1
        if (Instance != this) return;
        //2
        controls.Gameplay.Pause.performed -= OnPause;
        controls.UI.Cancel.performed -= OnCancel;
        controls.Disable();
        Instance = null;
    }

    //----------Action Map Separation----------\\
    /*Enable Gameplay
     * 1 - Enable gameplay and disable ui
     */
    public void EnableGameplay()
    {
        controls.UI.Disable();
        controls.Gameplay.Enable();
    }
    /*Enable UI
    * 1 - Enable ui and disable gameplay
    */
    public void EnableUI()
    {
        controls.Gameplay.Disable();
        controls.UI.Enable();
    }
    /*Disable All
     * 1 - Disable gameplay and ui
     */
    public void DisableAll()
    {
        controls.Gameplay.Disable();
        controls.UI.Disable();
    }

    //----------Input Handlers----------\\
    private void OnPause(InputAction.CallbackContext ctx) => Pause?.Invoke();
    private void OnCancel(InputAction.CallbackContext ctx) => Cancel?.Invoke();
}