using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnvironmentSettingsObserver : MonoBehaviour
{
    // Events to notify subscribers of environment property changes
    public event Action<float> OnDirectLightChanged;
    public event Action<float> OnAmbientLightChanged;

    [Header("Environment Properties")]
    [SerializeField, Range(0f, 5f)] private float directLightIntensity = 0.8f;
    [SerializeField, Range(0f, 5f)] private float ambientLightIntensity = 0.8f;

    [Header("UI Elements")]
    public Slider directLightSlider;
    public TMP_Text directLightText;
    public Slider ambientLightSlider;
    public TMP_Text ambientLightText;

    private void Awake()
    {
        InitializeUIElements();
        UpdateUI();
    }

    // Update UI elements to reflect current environment settings
    private void UpdateUI()
    {
        if (directLightText != null)
            directLightText.text = directLightIntensity.ToString("F2");

        if (ambientLightText != null)
            ambientLightText.text = ambientLightIntensity.ToString("F2");
    }

    // Invoked when the direct light intensity slider value changes
    public void SetDirectLightIntensity(float intensity)
    {
        directLightIntensity = intensity;
        OnDirectLightChanged?.Invoke(directLightIntensity);
        UpdateUI();
    }

    // Invoked when the ambient light intensity slider value changes
    public void SetAmbientLightIntensity(float intensity)
    {
        ambientLightIntensity = intensity;
        OnAmbientLightChanged?.Invoke(ambientLightIntensity);
        UpdateUI();
    }

    // Initialize UI elements and their listeners
    private void InitializeUIElements()
    {
        if (directLightSlider != null)
        {
            directLightSlider.minValue = 0f;
            directLightSlider.maxValue = 3f;
            directLightSlider.value = directLightIntensity;
            directLightSlider.onValueChanged.AddListener(SetDirectLightIntensity);
        }

        if (ambientLightSlider != null)
        {
            ambientLightSlider.minValue = 0f;
            ambientLightSlider.maxValue = 3f;
            ambientLightSlider.value = ambientLightIntensity;
            ambientLightSlider.onValueChanged.AddListener(SetAmbientLightIntensity);
        }
    }

    // Public getters for environment properties
    public float DirectLightIntensity => directLightIntensity;
    public float AmbientLightIntensity => ambientLightIntensity;
}
