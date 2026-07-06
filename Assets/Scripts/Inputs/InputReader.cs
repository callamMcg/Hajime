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
    public event Action Play;  // from ui
    public event Action FastForward;  // from ui
    public event Action SlowMotion;  // from ui
    public event Action Rewind;  // from ui

    //Input trackers
    private Vector2 move;
    public Vector2 Move => move;

    private Vector2 pull;
    public Vector2 Pull => pull;

    private Vector2 replayMove;
    public Vector2 ReplayMove => replayMove;

    private bool replayUp;
    public bool ReplayUp => replayUp;

    private bool replayDown;
    public bool ReplayDown => replayDown;

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

        controls.Gameplay.Move.performed += HandleMove;
        controls.Gameplay.Move.canceled += HandleMove;
        controls.Gameplay.Pull.performed += HandlePull;
        controls.Gameplay.Pull.canceled += HandlePull;

        controls.Replay.Move.performed += HandleReplayMove;
        controls.Replay.Move.canceled += HandleReplayMove;
        controls.Replay.Up.performed += HandleReplayUp;
        controls.Replay.Up.canceled += HandleReplayUp;
        controls.Replay.Down.performed += HandleReplayDown;
        controls.Replay.Down.canceled += HandleReplayDown;
        controls.Replay.Play.performed += OnPlay;
        controls.Replay.FastForward.performed += OnFastForward;
        controls.Replay.SlowMotion.performed += OnSlowMotion;
        controls.Replay.Rewind.performed += OnRewind;

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

        controls.Gameplay.Move.performed -= HandleMove;
        controls.Gameplay.Move.canceled -= HandleMove;
        controls.Gameplay.Pull.performed -= HandlePull;
        controls.Gameplay.Pull.canceled -= HandlePull;

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
        controls.Replay.Disable();
        controls.Gameplay.Enable();
    }
    /*Enable UI
    * 1 - Enable ui and disable gameplay
    */
    public void EnableUI()
    {
        controls.Gameplay.Disable();
        controls.Replay.Disable();
        controls.UI.Enable();

    }
    public void EnableReplay()
    {
        controls.Gameplay.Disable();
        controls.UI.Enable();
        controls.Replay.Enable();
        Debug.Log("Replay");
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
    //One Time
    private void OnPause(InputAction.CallbackContext ctx) => Pause?.Invoke();
    private void OnCancel(InputAction.CallbackContext ctx) => Cancel?.Invoke();
    private void OnPlay(InputAction.CallbackContext ctx) => Play?.Invoke();
    private void OnFastForward(InputAction.CallbackContext ctx) => FastForward?.Invoke();
    private void OnSlowMotion(InputAction.CallbackContext ctx) => SlowMotion?.Invoke();
    private void OnRewind(InputAction.CallbackContext ctx) => Rewind?.Invoke();

    //Constant
    private void HandleMove(InputAction.CallbackContext ctx) => move = ctx.ReadValue<Vector2>();
    private void HandlePull(InputAction.CallbackContext ctx) => pull = ctx.ReadValue<Vector2>();

    private void HandleReplayMove(InputAction.CallbackContext ctx) => replayMove = ctx.ReadValue<Vector2>();

    private void HandleReplayUp(InputAction.CallbackContext ctx) => replayUp = ctx.performed ;
    private void HandleReplayDown(InputAction.CallbackContext ctx) => replayDown = ctx.performed;


}