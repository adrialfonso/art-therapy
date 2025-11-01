using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class BrushSettings : MonoBehaviour
{
    public event Action<int> OnBrushSizeChanged;
    public event Action<Color> OnBrushColorChanged;
    public event Action<int> OnStrategyChanged;

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
        // Initialize UI elements
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

        UpdateUI();
    }

    // Update UI elements to reflect current brush settings
    private void UpdateUI()
    {
        if (brushSizeText != null) brushSizeText.text = brushSize.ToString();
        if (colorPreview != null) colorPreview.color = brushColor;
        if (brightnessText != null) brightnessText.text = Mathf.RoundToInt(brushBrightness).ToString();
    }

    public void SetBrushSize(float newSize)
    {
        brushSize = Mathf.RoundToInt(newSize);
        OnBrushSizeChanged?.Invoke(brushSize);
        UpdateUI();
    }

    public void SetBrushHue(float hue)
    {
        baseColor = Color.HSVToRGB(hue, 1f, 1f);
        UpdateBrushColor();
    }

    public void SetBrightness(float brightness)
    {
        brushBrightness = brightness;
        UpdateBrushColor();
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

    public void SetStrategyIndex(int index)
    {
        strategyIndex = index;
        OnStrategyChanged?.Invoke(strategyIndex);
    }

    // Public getters for brush properties
    public int BrushSize => brushSize;
    public Color BrushColor => brushColor;
    public float BrushBrightness => brushBrightness;
    public int StrategyIndex => strategyIndex;
}
