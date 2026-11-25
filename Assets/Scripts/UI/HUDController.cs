using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD displaying speed, altitude, fuel, flight mode, and warning alerts.
/// Requires TextMeshPro and Unity UI.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("References")]
    public FlyingCarController car;

    [Header("Text Elements")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI altitudeText;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI warningText;

    [Header("Gauges")]
    public Slider fuelSlider;
    public Slider thrustSlider;
    public Image fuelFill;

    [Header("Colors")]
    public Color normalFuelColor = Color.green;
    public Color lowFuelColor    = Color.yellow;
    public Color criticalColor   = Color.red;

    void Update()
    {
        if (car == null) return;

        // Speed: convert m/s to km/h
        float kmh = car.Speed * 3.6f;
        speedText.text = $"{kmh:F0} km/h";

        // Altitude: meters above sea level
        altitudeText.text = $"{car.Altitude:F1} m";

        // Fuel
        float fuelPct = car.Fuel / 100f;
        fuelSlider.value = fuelPct;
        thrustSlider.value = car.ThrustPercent;

        if (fuelPct > 0.4f)
            fuelFill.color = normalFuelColor;
        else if (fuelPct > 0.15f)
            fuelFill.color = lowFuelColor;
        else
            fuelFill.color = criticalColor;

        // Mode
        modeText.text = car.IsFlying ? "✈ FLIGHT" : "🚗 DRIVE";
        modeText.color = car.IsFlying ? Color.cyan : Color.white;

        // Warnings
        string warning = "";
        if (car.Fuel <= 0f)
            warning = "⚠ FUEL EMPTY — GLIDING";
        else if (car.Fuel < 15f)
            warning = "⚠ LOW FUEL";
        else if (car.IsFlying && car.Speed < 15f && !car.IsGrounded)
            warning = "⚠ STALL SPEED";

        warningText.text = warning;
        warningText.gameObject.SetActive(!string.IsNullOrEmpty(warning));
    }
}
