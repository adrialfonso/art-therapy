using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class BrushSettingsObserver : MonoBehaviour
{
    // Events to notify subscribers of brush property changes
    public event Action<int> OnBrushSizeChanged;
    public event Action<Color> OnBrushColorChanged;
    public event Action<int> OnStrategyChanged;
    public event Action<bool> On3DModeChanged;
    public event Action OnSaveArtwork;
    public event Action OnLoadArtwork;
    public event Action OnNewArtwork;

    [Header("Brush Properties")]
    [SerializeField] private int brushSize = 50;
    [SerializeField] private Color brushColor = Color.red;
    [SerializeField, Range(1, 100)] private float brushBrightness = 50f;
    [SerializeField] private int strategyIndex = 0;

    [Header("UI Elements")]
    public Slider brushSizeSlider;
    public TMP_Text brushSizeText;
    public Slider colorSlider;
    public Image colorPreview;
    public Slider brightnessSlider;
    public TMP_Text brightnessText;

    private Color baseColor;

    private void Awake()
    {
        InitializeUIElements();
        UpdateUI();
    }

    // Update UI elements to reflect current brush settings
    private void UpdateUI()
    {
        if (brushSizeText != null) brushSizeText.text = brushSize.ToString();
        if (colorPreview != null) colorPreview.color = brushColor;
        if (brightnessText != null) brightnessText.text = Mathf.RoundToInt(brushBrightness).ToString();
    }

    // Update the brush color based on base color and brightness
    private void UpdateBrushColor()
    {
        Color finalColor;

        if (brushBrightness < 50)
        {
            float t = brushBrightness / 50f;
            finalColor = Color.Lerp(Color.white, baseColor, t);
        }
        else if (brushBrightness > 50)
        {
            float t = (brushBrightness - 50f) / 50f;
            finalColor = Color.Lerp(baseColor, Color.black, t);
        }
        else
        {
            finalColor = baseColor;
        }

        brushColor = finalColor;
        OnBrushColorChanged?.Invoke(finalColor);
        UpdateUI();
    }

    // Invoked when brush size slider value changes
    public void SetBrushSize(float newSize)
    {
        brushSize = Mathf.RoundToInt(newSize);
        OnBrushSizeChanged?.Invoke(brushSize);
        UpdateUI();
    }

    // Invoked when color slider value changes
    public void SetBrushHue(float hue)
    {
        baseColor = Color.HSVToRGB(hue, 1f, 1f);
        UpdateBrushColor();
    }

    // Invoked when brightness slider value changes
    public void SetBrightness(float brightness)
    {
        brushBrightness = brightness;
        UpdateBrushColor();
    }

    // Invoked to change the current drawing strategy
    public void SetStrategyIndex(int index)
    {
        strategyIndex = index;
        OnStrategyChanged?.Invoke(strategyIndex);
    }

    // Invoked to toggle 3D drawing mode
    public void Set3DMode(bool active)
    {
        On3DModeChanged?.Invoke(active);
    }

    // Invoked when Save Artwork button is pressed
    public void SaveArtwork()
    {
        OnSaveArtwork?.Invoke();
    }

    // Invoked when Load Artwork button is pressed
    public void LoadArtwork()
    {
        OnLoadArtwork?.Invoke();
    }

    // Invoked when New Artwork button is pressed
    public void NewArtwork()
    {
        OnNewArtwork?.Invoke();
    }

    // Initialize UI elements and their listeners
    private void InitializeUIElements()
    {
        if (brushSizeSlider != null)
        {
            brushSizeSlider.minValue = 1;
            brushSizeSlider.maxValue = 100;
            brushSizeSlider.value = brushSize;
            brushSizeSlider.onValueChanged.AddListener(SetBrushSize);
        }

        if (colorSlider != null)
        {
            colorSlider.onValueChanged.AddListener(SetBrushHue);
            baseColor = brushColor;
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 1;
            brightnessSlider.maxValue = 100;
            brightnessSlider.value = brushBrightness;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
    }

    // Public getters for brush properties
    public int BrushSize => brushSize;
    public Color BrushColor => brushColor;
    public float BrushBrightness => brushBrightness;
    public int StrategyIndex => strategyIndex;
}
