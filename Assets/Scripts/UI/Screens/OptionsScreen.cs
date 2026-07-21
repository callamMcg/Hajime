using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The settings screen.
/// Settings are read back out of storage each time it opens and written back as
/// it closes, so they survive between sessions.
/// Volume and fullscreen are applied the instant they are changed rather than on
/// closing, so the player can hear and see what they are doing while dragging the
/// slider. Saving still waits until the screen closes, so storage is not written
/// on every frame of a drag.
/// </summary>
public class OptionsScreen : MenuScreen
{
    //----------Variables----------\\

    // The slider controlling overall volume
    [SerializeField] private Slider masterVolumeSlider;

    // The slider controlling look sensitivity
    [SerializeField] private Slider sensitivitySlider;

    // The switch controlling fullscreen
    [SerializeField] private Toggle fullscreenToggle;

    // The names each setting is stored under
    private const string VolumeKey = "opt_master_volume";
    private const string SensKey = "opt_sensitivity";
    private const string FullscreenKey = "opt_fullscreen";

    // What each setting falls back to if it has never been saved
    private const float DefaultVolume = 1;
    private const float DefaultSens = 1;

    //----------Event Loop----------\\

    /* ON ENABLE
     * 1 - Start listening to the controls that need to take effect immediately,
     *     so the player can hear the volume and see the fullscreen change as
     *     they move them
     */
    private void OnEnable()
    {
        masterVolumeSlider.onValueChanged.AddListener(ApplyVolume);
        fullscreenToggle.onValueChanged.AddListener(ApplyFullscreen);
    }

    /* ON DISABLE
     * 1 - Stop listening, so this is not still reacting once the screen is gone
     */
    private void OnDisable()
    {
        masterVolumeSlider.onValueChanged.RemoveListener(ApplyVolume);
        fullscreenToggle.onValueChanged.RemoveListener(ApplyFullscreen);
    }

    //----------Protected Functions----------\\

    /* ON ENTER
     * 1 - Read each setting back out of storage and show it on its control,
     *     falling back to a default if it was never saved
     * The controls are set without announcing it, so filling them in does not
     * count as the player changing them
     */
    protected override void OnEnter()
    {
        masterVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
        sensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SensKey, DefaultSens));
        fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1);
    }

    /* ON EXIT
     * 1 - Write every setting back to storage
     * 2 - Flush it to disk, so it survives the game being closed
     * Saving waits until the screen closes rather than happening on every change,
     * so dragging a slider does not write to storage on every frame
     */
    protected override void OnExit()
    {
        // 1
        PlayerPrefs.SetFloat(VolumeKey, masterVolumeSlider.value);
        PlayerPrefs.SetFloat(SensKey, sensitivitySlider.value);
        PlayerPrefs.SetInt(FullscreenKey, fullscreenToggle.isOn ? 1 : 0);

        // 2
        PlayerPrefs.Save();
    }

    //----------Private Functions----------\\

    /* APPLY VOLUME
     * 1 - Set the game's volume the moment the slider moves
     */
    private void ApplyVolume(float v) => AudioListener.volume = v;

    /* APPLY FULLSCREEN
     * 1 - Switch fullscreen the moment the toggle is flipped
     */
    private void ApplyFullscreen(bool on) => Screen.fullScreen = on;
}
