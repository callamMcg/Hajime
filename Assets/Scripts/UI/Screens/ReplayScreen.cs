using TMPro;
using UnityEngine;

/// <summary>
/// The screen that plays the match back.
/// Opening it swaps the match camera for a free one the player can fly around
/// with, hands the controls over to the replay set, and starts the playback.
/// Closing it puts all three back exactly as they were.
/// The free camera is flown in its own local directions, so pushing forward
/// always means forward from wherever it is currently pointing.
/// It runs on unscaled time throughout, because the match is frozen while the
/// replay is up and anything measured in match time would simply never move.
/// </summary>
public class ReplayScreen : MenuScreen
{
    //----------Variables----------\\

    // The camera the match is normally watched through
    [SerializeField] Camera combatCam;

    // The free camera the replay is watched through
    [SerializeField] Camera replayCam;

    // The controller that actually drives the playback
    [SerializeField] ReplayController controller;

    // Where the current playback speed is shown
    [SerializeField] private TMP_Text speedDisplay;

    //----------Event Loop----------\\

    /* AWAKE
     * 1 - Set up as normal
     * 2 - Make sure the match camera is the one live and the replay camera is
     *     not, so the replay one cannot be left on from the editor
     */
    protected override void Awake()
    {
        base.Awake();
        combatCam.enabled = true;
        replayCam.enabled = false;
    }

    /* UPDATE
     * 1 - Read which way the player is pushing the free camera
     * 2 - Read whether they are also taking it up or down
     * 3 - Move it in its own local directions, so forward always means the way
     *     it is currently pointing, counting in unscaled time since the match is
     *     frozen
     */
    private void Update()
    {
        // 1
        Vector2 move = InputReader.Instance.ReplayMove * 3;

        // 2
        float up = 0;
        if (InputReader.Instance.ReplayUp)
            up = 1;
        else if (InputReader.Instance.ReplayDown)
            up = -1;

        // 3
        Vector3 displacement = new Vector3(move.x, up, move.y);
        replayCam.transform.position += replayCam.transform.TransformDirection(displacement) * Time.unscaledDeltaTime;
    }

    //----------Override Functions----------\\

    /* ON ENTER
     * 1 - Start the playback from the beginning of what was recorded
     * 2 - Hand the controls over to the replay set, so the buttons now scrub
     *     rather than fight
     * 3 - Swap the match camera out for the free one
     * 4 - Start listening for play, fast forward and slow motion
     */
    protected override void OnEnter()
    {
        controller.BeginReplay();
        InputReader.Instance.EnableReplay();

        replayCam.enabled = true;
        combatCam.enabled = false;

        InputReader.Instance.VideoPlay += OnPlay;
        InputReader.Instance.FastForward += OnSpeedUp;
        InputReader.Instance.SlowMotion += OnSlowDown;
    }

    /* ON EXIT
     * 1 - Stop listening for the playback buttons
     * 2 - End the playback, which puts both judokas back exactly as the match
     *     left them
     * 3 - Hand the controls back to the menu
     * 4 - Swap the match camera back in
     */
    protected override void OnExit()
    {
        InputReader.Instance.VideoPlay -= OnPlay;

        controller.EndReplay();
        InputReader.Instance.EnableUI();

        combatCam.enabled = true;
        replayCam.enabled = false;
    }

    //----------Private Functions----------\\

    /* ON PLAY
     * 1 - Start or stop the playback
     */
    private void OnPlay()
    {
        controller.TogglePlay();
    }

    /* ON SPEED UP
     * 1 - Step the playback speed up
     * 2 - Show the new speed
     */
    private void OnSpeedUp()
    {
        controller.FastForward();
        speedDisplay.text = controller.SpeedDisplay;
    }

    /* ON SLOW DOWN
     * 1 - Step the playback speed down
     * 2 - Show the new speed
     */
    private void OnSlowDown()
    {
        controller.SlowDown();
        speedDisplay.text = controller.SpeedDisplay;
    }
}
