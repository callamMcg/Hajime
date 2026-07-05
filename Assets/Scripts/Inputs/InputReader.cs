using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    public static InputReader Instance { get; private set; }

    // Semantic events other systems subscribe to.
    public event Action Pause;   // from the Gameplay map
    public event Action Cancel;  // from the UI map (Back / Resume)

    private GameControls controls;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        controls = new GameControls();
        controls.Gameplay.Pause.performed += OnPause;
        controls.UI.Cancel.performed += OnCancel;

        EnableUI(); // we boot into the Main Menu scene
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        controls.Gameplay.Pause.performed -= OnPause;
        controls.UI.Cancel.performed -= OnCancel;
        controls.Disable();
        Instance = null;
    }

    // --- Action-map separation ---
    public void EnableGameplay()
    {
        controls.UI.Disable();
        controls.Gameplay.Enable();
    }
     
    public void EnableUI()
    {
        controls.Gameplay.Disable();
        controls.UI.Enable();
    }

    public void DisableAll()
    {
        controls.Gameplay.Disable();
        controls.UI.Disable();
    }

    // --- Callbacks -> events ---
    private void OnPause(InputAction.CallbackContext _) => Pause?.Invoke();
    private void OnCancel(InputAction.CallbackContext _) => Cancel?.Invoke();
}