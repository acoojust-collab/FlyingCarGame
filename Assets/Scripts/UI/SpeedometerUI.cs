using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Analog-style speedometer dial rendered via UI Image (filled radial).
/// Pair with a needle RectTransform that rotates from minAngle to maxAngle.
/// Also handles altitude ribbon and G-force meter.
/// </summary>
public class SpeedometerUI : MonoBehaviour
{
    [Header("References")]
    public FlyingCarController car;

    // ── Speedometer ──────────────────────────────────────────────────────────
    [Header("Speedometer Dial")]
    public RectTransform needleTransform;       // needle image pivot at center
    public float needleMinAngle = 220f;         // degrees at 0 km/h (clockwise from up)
    public float needleMaxAngle = -40f;         // degrees at maxSpeed
    public float maxDisplaySpeed = 500f;        // km/h shown on dial
    public float needleSmoothSpeed = 5f;

    public TextMeshProUGUI digitalSpeedText;    // "342 km/h" digital readout below dial
    public TextMeshProUGUI speedUnitText;       // "KM/H" label
    public Image dialRingFill;                  // optional colored ring fill (radial 360)
    public Gradient speedColorGradient;         // green → yellow → red

    // ── Altitude ─────────────────────────────────────────────────────────────
    [Header("Altitude Gauge")]
    public RectTransform altNeedle;
    public float altNeedleMinAngle = 220f;
    public float altNeedleMaxAngle = -40f;
    public float maxDisplayAlt = 3000f;         // meters
    public TextMeshProUGUI altitudeText;        // "1240 m" digital

    // ── G-Force ──────────────────────────────────────────────────────────────
    [Header("G-Force Meter")]
    public Slider gForceSlider;
    public TextMeshProUGUI gForceText;
    public Image gForceFill;
    public float maxDisplayG = 6f;

    // ── Private ───────────────────────────────────────────────────────────────
    private float currentNeedleAngle;
    private float currentAltAngle;
    private Vector3 prevVelocity;
    private float smoothGForce;

    void Start()
    {
        currentNeedleAngle = needleMinAngle;
        currentAltAngle    = altNeedleMinAngle;
    }

    void Update()
    {
        if (car == null) return;

        float speedKmh = car.Speed * 3.6f;
        float altitude = car.Altitude;

        UpdateSpeedometer(speedKmh);
        UpdateAltimeter(altitude);
        UpdateGForce();
    }

    // ─── SPEEDOMETER ────────────────────────────────────────────────────────

    void UpdateSpeedometer(float speedKmh)
    {
        float t = Mathf.Clamp01(speedKmh / maxDisplaySpeed);
        float targetAngle = Mathf.Lerp(needleMinAngle, needleMaxAngle, t);
        currentNeedleAngle = Mathf.Lerp(currentNeedleAngle, targetAngle, Time.deltaTime * needleSmoothSpeed);

        if (needleTransform != null)
            needleTransform.localEulerAngles = new Vector3(0f, 0f, currentNeedleAngle);

        if (digitalSpeedText != null)
            digitalSpeedText.text = Mathf.RoundToInt(speedKmh).ToString();

        // Color the dial ring
        if (dialRingFill != null)
        {
            dialRingFill.fillAmount = t;
            if (speedColorGradient != null)
                dialRingFill.color = speedColorGradient.Evaluate(t);
        }
    }

    // ─── ALTIMETER ──────────────────────────────────────────────────────────

    void UpdateAltimeter(float altitude)
    {
        float t = Mathf.Clamp01(altitude / maxDisplayAlt);
        float targetAngle = Mathf.Lerp(altNeedleMinAngle, altNeedleMaxAngle, t);
        currentAltAngle = Mathf.Lerp(currentAltAngle, targetAngle, Time.deltaTime * needleSmoothSpeed);

        if (altNeedle != null)
            altNeedle.localEulerAngles = new Vector3(0f, 0f, currentAltAngle);

        if (altitudeText != null)
            altitudeText.text = $"{altitude:F0} m";
    }

    // ─── G-FORCE ────────────────────────────────────────────────────────────

    void UpdateGForce()
    {
        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 accel = (rb.velocity - prevVelocity) / Time.deltaTime;
        float rawG    = accel.magnitude / 9.81f;
        smoothGForce  = Mathf.Lerp(smoothGForce, rawG, Time.deltaTime * 4f);
        prevVelocity  = rb.velocity;

        float t = Mathf.Clamp01(smoothGForce / maxDisplayG);

        if (gForceSlider != null) gForceSlider.value = t;
        if (gForceText   != null) gForceText.text = $"{smoothGForce:F1} G";

        if (gForceFill != null)
        {
            if (t < 0.5f)      gForceFill.color = Color.green;
            else if (t < 0.8f) gForceFill.color = Color.yellow;
            else               gForceFill.color = Color.red;
        }
    }
}
