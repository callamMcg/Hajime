using System;
using UnityEngine;

public class ReplayScreen : MenuScreen
{
    [SerializeField] Camera combatCam;
    [SerializeField] Camera replayCam;
    protected override void Awake()
    {
        base.Awake();
        combatCam.enabled = true;
        replayCam.enabled = false;
    }
    protected override void OnEnter()
    {
        Debug.Log("Enter");
        InputReader.Instance.EnableReplay();
        replayCam.enabled = true;
        combatCam.enabled = false;

    }
    protected override void OnExit()
    {
        InputReader.Instance.EnableUI();
        combatCam.enabled = true;
        replayCam.enabled = false;
    }

    private void Update()
    {
        Vector2 move = InputReader.Instance.ReplayMove;
        float up = 0;
        if (InputReader.Instance.ReplayUp)
            up = 1;
        else if (InputReader.Instance.ReplayDown)
            up = -1;

        Vector3 displacement = new Vector3(move.x, up, move.y);
        replayCam.transform.position += replayCam.transform.TransformDirection(displacement) * Time.unscaledDeltaTime;
    }
}
