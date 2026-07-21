using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ReplayScreen : MenuScreen
{
    [SerializeField] Camera combatCam;
    [SerializeField] Camera replayCam;
    [SerializeField] ReplayController controller;
    [SerializeField] private TMP_Text speedDisplay;

    protected override void Awake()
    {
        base.Awake();
        combatCam.enabled = true;
        replayCam.enabled = false;
    }

    protected override void OnEnter()
    {
        controller.BeginReplay();
        InputReader.Instance.EnableReplay();
        replayCam.enabled = true;
        combatCam.enabled = false;
        //InputReader.Instance.Play.performed += OnPlay;
        InputReader.Instance.VideoPlay += OnPlay;
        InputReader.Instance.FastForward += OnSpeedUp;
        InputReader.Instance.SlowMotion += OnSlowDown;
    }

    protected override void OnExit()
    {
        InputReader.Instance.VideoPlay -= OnPlay;
        controller.EndReplay();
        InputReader.Instance.EnableUI();
        combatCam.enabled = true;
        replayCam.enabled = false;
    }

    private void Update()
    {
        Vector2 move = InputReader.Instance.ReplayMove * 3;
        float up = 0;
        if (InputReader.Instance.ReplayUp)
            up = 1;
        else if (InputReader.Instance.ReplayDown)
            up = -1;

        Vector3 displacement = new Vector3(move.x, up, move.y);
        replayCam.transform.position += replayCam.transform.TransformDirection(displacement) * Time.unscaledDeltaTime;
    }

    private void OnPlay() 
    {
        controller.TogglePlay();
    }
    private void OnSpeedUp()
    {
        controller.FastForward();
        speedDisplay.text = controller.SpeedDisplay;
    }
    private void OnSlowDown()
    {
        controller.SlowDown();
        speedDisplay.text = controller.SpeedDisplay;
    }
}