using UnityEngine;
using UnityEngine.UI;

public class OptionsScreen : MenuScreen
{
    //----------Variables----------\\
    //References
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Toggle fullscreenToggle;

    //Setting Keys
    private const string VolumeKey = "opt_master_volume";
    private const string SensKey = "opt_sensitivity";
    private const string FullscreenKey = "opt_fullscreen";

    //Default Values
    private const float DefaultVolume = 1;
    private const float DefaultSens = 1;

    //----------Event Loop----------\\
    //Add listeners to sliders
    private void OnEnable()
    {
        masterVolumeSlider.onValueChanged.AddListener(ApplyVolume);
        fullscreenToggle.onValueChanged.AddListener(ApplyFullscreen);
    }
    
    //Remove Listeners from sliders
    private void OnDisable()
    {
        masterVolumeSlider.onValueChanged.RemoveListener(ApplyVolume);
        fullscreenToggle.onValueChanged.RemoveListener(ApplyFullscreen);
    }

    //----------Protected Functions----------\\
    /*On Enter
     * 1 - Set values to default
     */
    protected override void OnEnter()
    {
        masterVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
        sensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SensKey, DefaultSens));
        fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1);
    }

    /*On Exit
     * 1 - Set values
     * 2 - Save
     */
    protected override void OnExit()
    {
        PlayerPrefs.SetFloat(VolumeKey, masterVolumeSlider.value);
        PlayerPrefs.SetFloat(SensKey, sensitivitySlider.value);
        PlayerPrefs.SetInt(FullscreenKey, fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    //----------Private Functions----------\\
    private void ApplyVolume(float v) => AudioListener.volume = v;
    private void ApplyFullscreen(bool on) => Screen.fullScreen = on;
}