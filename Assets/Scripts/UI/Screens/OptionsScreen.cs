using UnityEngine;
using UnityEngine.UI;

public class OptionsScreen : MenuScreen
{
    [Header("Controls")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Toggle fullscreenToggle;

    private const string VolumeKey = "opt_master_volume";
    private const string SensKey = "opt_sensitivity";
    private const string FullscreenKey = "opt_fullscreen";

    private const float DefaultVolume = 0.8f;
    private const float DefaultSens = 1f;

    private void OnEnable()
    {
        masterVolumeSlider.onValueChanged.AddListener(ApplyVolume);
        fullscreenToggle.onValueChanged.AddListener(ApplyFullscreen);
    }

    private void OnDisable()
    {
        masterVolumeSlider.onValueChanged.RemoveListener(ApplyVolume);
        fullscreenToggle.onValueChanged.RemoveListener(ApplyFullscreen);
    }

    protected override void OnEnter()
    {
        masterVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
        sensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SensKey, DefaultSens));
        fullscreenToggle.SetIsOnWithoutNotify(
            PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1);

        ApplyVolume(masterVolumeSlider.value);
        ApplyFullscreen(fullscreenToggle.isOn);
    }

    protected override void OnExit()
    {
        PlayerPrefs.SetFloat(VolumeKey, masterVolumeSlider.value);
        PlayerPrefs.SetFloat(SensKey, sensitivitySlider.value);
        PlayerPrefs.SetInt(FullscreenKey, fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float v) => AudioListener.volume = v;
    private void ApplyFullscreen(bool on) => Screen.fullScreen = on;
}