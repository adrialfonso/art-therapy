using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnvironmentSettingsObserver : MonoBehaviour
{
    // Events to notify subscribers of environment property changes
    public event Action<float> OnSkyExposureChanged;
    public event Action<float> OnSkyRotationChanged;
    public event Action<float> OnAmbientVolumeChanged;

    [Header("Environment Properties")]
    [SerializeField] private float skyExposure = 1.0f;
    [SerializeField] private float skyRotation = 1.0f;
    [SerializeField] private float ambientVolume = 0.1f;

    [Header("UI Elements")]
    public Slider skyExposureSlider;
    public TMP_Text skyExposureText;
    public Slider skyRotationSlider;
    public TMP_Text skyRotationText;
    public Slider ambientVolumeSlider;
    public TMP_Text ambientVolumeText;

    private void Awake()
    {
        InitializeUIElements();
        UpdateUI();
    }

    // Update UI elements to reflect current environment settings
    private void UpdateUI()
    {
        if (skyExposureText != null)
            skyExposureText.text = skyExposure.ToString("F2");

        if (skyRotationText != null)
            skyRotationText.text = skyRotation.ToString("F2");
    }

    // Invoked when the sky exposure slider value changes
    public void SetSkyExposure(float exposure)
    {
        skyExposure = exposure;
        OnSkyExposureChanged?.Invoke(skyExposure);
        UpdateUI();
    }

    // Invoked when the sky rotation slider value changes
    public void SetSkyRotation(float rotation)
    {
        skyRotation = rotation;
        OnSkyRotationChanged?.Invoke(skyRotation);
        UpdateUI();
    }

    // Invoked when the ambient volume slider value changes
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = volume;
        OnAmbientVolumeChanged?.Invoke(volume);

        if (ambientVolumeText != null)
            ambientVolumeText.text = volume.ToString("F2");
    }

    // Initialize UI elements and their listeners
    private void InitializeUIElements()
    {
        if (skyExposureSlider != null)
        {
            skyExposureSlider.value = skyExposure;
            skyExposureSlider.onValueChanged.AddListener(SetSkyExposure);
        }

        if (skyRotationSlider != null)
        {
            skyRotationSlider.value = skyRotation;
            skyRotationSlider.onValueChanged.AddListener(SetSkyRotation);
        }

        if (ambientVolumeSlider != null)
        {
            ambientVolumeSlider.value = ambientVolume;
            ambientVolumeSlider.onValueChanged.AddListener(SetAmbientVolume);
        }
    }

    // Public getters for environment properties
    public float SkyExposure => skyExposure;
    public float SkyRotation => skyRotation;
    public float AmbientVolume => ambientVolume;
}
