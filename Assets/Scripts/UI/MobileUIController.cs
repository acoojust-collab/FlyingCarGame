using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Orchestrates the full mobile UI:
///   Left joystick  → throttle (vertical) + yaw/steer (horizontal)
///   Right joystick → pitch (vertical) + roll (horizontal)
///   VTOL button    → toggle flight mode
///   Camera button  → toggle chase / cockpit
///   Boost button   → hold for afterburner
///   Fuel bar + speed readout always visible
/// 
/// Auto-hides joysticks on PC builds; shown only on Android / touch devices.
/// </summary>
public class MobileUIController : MonoBehaviour
{
    [Header("Joysticks")]
    public VirtualJoystick leftJoystick;    // throttle + yaw
    public VirtualJoystick rightJoystick;   // pitch + roll

    [Header("Action Buttons")]
    public TouchButton vtolButton;          // toggle: DRIVE ↔ FLIGHT
    public TouchButton cameraButton;        // toggle: Chase ↔ Cockpit
    public TouchButton boostButton;         // hold for afterburner throttle boost

    [Header("Quick Info Strip (top of screen)")]
    public TextMeshProUGUI speedLabel;      // "248 km/h"
    public TextMeshProUGUI altLabel;        // "312 m"
    public Slider          fuelBar;
    public Image           fuelFill;

    [Header("Button Labels")]
    public TextMeshProUGUI vtolLabel;
    public TextMeshProUGUI camLabel;

    [Header("Layout")]
    public CanvasGroup mobileGroup;         // whole mobile UI group
    public float       joystickAlpha = 0.7f;

    [Header("References")]
    public FlyingCarController car;
    public FlightCameraController camController;

    // Internally resolved
    private bool isPCBuild;

    void Start()
    {
        if (car == null) car = FindObjectOfType<FlyingCarController>();
        if (camController == null) camController = FindObjectOfType<FlightCameraController>();

#if UNITY_EDITOR || UNITY_STANDALONE
        isPCBuild = true;
#else
        isPCBuild = false;
#endif

        // Hide mobile UI on PC
        if (mobileGroup != null)
        {
            mobileGroup.alpha          = isPCBuild ? 0f : joystickAlpha;
            mobileGroup.interactable   = !isPCBuild;
            mobileGroup.blocksRaycasts = !isPCBuild;
        }

        SetupButtonLabels();
    }

    void Update()
    {
        if (isPCBuild || car == null) return;

        ApplyJoystickInput();
        HandleButtons();
        UpdateInfoStrip();
    }

    // ─── JOYSTICK → CAR INPUT ────────────────────────────────────────────────

    void ApplyJoystickInput()
    {
        // Left joystick
        if (leftJoystick != null)
        {
            float throttle = Mathf.Max(0f, leftJoystick.Vertical);   // only forward throttle
            float yaw      = leftJoystick.Horizontal;

            car.thrustInput = throttle;
            car.yawInput    = yaw;
            car.steerInput  = yaw;   // ground steering uses same axis
        }

        // Right joystick
        if (rightJoystick != null)
        {
            car.pitchInput = rightJoystick.Vertical;
            car.rollInput  = rightJoystick.Horizontal;
        }

        // Boost: hold overrides throttle to max
        if (boostButton != null && boostButton.IsPressed)
            car.thrustInput = 1f;
    }

    // ─── BUTTON EVENTS ───────────────────────────────────────────────────────

    void HandleButtons()
    {
        // VTOL toggle — fires once per tap
        if (vtolButton != null)
        {
            car.vtolToggle = vtolButton.WasTappedThisFrame;
            if (vtolLabel != null)
                vtolLabel.text = car.IsFlying ? "🚗 LAND" : "✈ FLY";
        }

        // Camera toggle
        if (cameraButton != null && cameraButton.WasTappedThisFrame && camController != null)
        {
            camController.cockpitMode = !camController.cockpitMode;
            if (camLabel != null)
                camLabel.text = camController.cockpitMode ? "📷 CHASE" : "🪟 COCKPIT";
        }
    }

    // ─── INFO STRIP ──────────────────────────────────────────────────────────

    void UpdateInfoStrip()
    {
        if (speedLabel != null)
            speedLabel.text = $"{car.Speed * 3.6f:F0} km/h";

        if (altLabel != null)
            altLabel.text = $"{car.Altitude:F0} m";

        if (fuelBar != null)
            fuelBar.value = car.Fuel / 100f;

        if (fuelFill != null)
        {
            float pct = car.Fuel / 100f;
            fuelFill.color = pct > 0.4f ? Color.green :
                             pct > 0.15f ? Color.yellow : Color.red;
        }
    }

    // ─── LABELS ──────────────────────────────────────────────────────────────

    void SetupButtonLabels()
    {
        if (vtolLabel  != null) vtolLabel.text = "✈ FLY";
        if (camLabel   != null) camLabel.text  = "🪟 COCKPIT";
    }
}
