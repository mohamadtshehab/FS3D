using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlPanel : MonoBehaviour
{
    public TMP_InputField gridSizeInput;
    public Slider velocitySlider;
    public TMP_Text velocityValue;
    public Slider diffusionSlider;
    public TMP_Text diffusionValue;
    public Toggle rgbToggle;
    public TMP_Text toggleValue;

    private int gridSize;
    private float velocity;
    private float diffusion;
    private bool rgbModeEnabled;

    void Start()
    {
        // Initialize the UI elements
        velocitySlider.minValue = 0;
        velocitySlider.maxValue = 50;
        velocitySlider.onValueChanged.AddListener(OnVelocityChanged);
        velocityValue.text = velocitySlider.value.ToString("F2");

        diffusionSlider.minValue = 0;
        diffusionSlider.maxValue = 0.1f;
        diffusionSlider.onValueChanged.AddListener(OnDiffusionChanged);
        diffusionValue.text = diffusionSlider.value.ToString("F4");

        gridSizeInput.onValueChanged.AddListener(OnGridSizeChanged);
        if (int.TryParse(gridSizeInput.text, out gridSize))
        {
            // Valid initial value
        }
        else
        {
            gridSize = 0; // Or some default value
        }

        // Initialize the RGB toggle
        rgbToggle.onValueChanged.AddListener(OnRgbToggleChanged);
        toggleValue.text = rgbToggle.isOn.ToString();
        rgbModeEnabled = rgbToggle.isOn;
    }

    void OnVelocityChanged(float value)
    {
        velocity = value;
        velocityValue.text = value.ToString("F2");
    }

    void OnDiffusionChanged(float value)
    {
        diffusion = value;
        diffusionValue.text = value.ToString("F4");
    }

    void OnGridSizeChanged(string value)
    {
        if (int.TryParse(value, out gridSize))
        {
            // Valid input, gridSize is updated
        }
        else
        {
            Debug.LogWarning("Invalid grid size input");
        }
    }

    void OnRgbToggleChanged(bool value)
    {
        rgbModeEnabled = value;
        toggleValue.text = value.ToString();
    }

    public int GetGridSize()
    {
        return gridSize;
    }

    public float GetVelocity()
    {
        return velocity;
    }

    public float GetDiffusion()
    {
        return diffusion;
    }

    public bool GetRgbModeEnabled()
    {
        return rgbModeEnabled;
    }
}
