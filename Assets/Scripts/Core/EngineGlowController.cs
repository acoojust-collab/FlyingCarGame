using UnityEngine;

/// <summary>
/// Engine Glow Light Controller.
/// Pulses point lights on engine nozzles based on throttle and RPM simulation.
/// Works best with URP real-time lights (set Render Pipeline to URP).
/// </summary>
public class EngineGlowController : MonoBehaviour
{
    [Header("References")]
    public FlyingCarController car;

    [Header("Engine Lights (assign one Light per nozzle)")]
    public Light[] engineLights;
    public Light[] afterburnerLights;

    [Header("Normal Engine")]
    public Color idleColor       = new Color(0.5f, 0.6f, 1f);    // cool blue
    public Color thrustColor     = new Color(1f, 0.7f, 0.2f);    // warm orange
    public float idleIntensity   = 0.5f;
    public float thrustIntensity = 8f;
    public float idleRange       = 1f;
    public float thrustRange     = 4f;

    [Header("Afterburner")]
    public Color afterburnerColor = new Color(0.2f, 0.6f, 1f);   // blue plasma
    public float afterburnerIntensity = 20f;
    public float afterburnerRange     = 6f;
    public float afterburnerThreshold = 0.85f;

    [Header("Flicker")]
    public float flickerSpeed    = 15f;    // Hz
    public float flickerAmount   = 0.1f;  // intensity variance

    private float flickerTime;

    void Update()
    {
        if (car == null) return;

        float throttle = car.ThrustPercent;
        bool  flying   = car.IsFlying;
        bool  afterburning = flying && throttle >= afterburnerThreshold;

        flickerTime += Time.deltaTime * flickerSpeed;
        float flicker = 1f + Mathf.Sin(flickerTime) * flickerAmount
                           + Mathf.Sin(flickerTime * 2.3f) * flickerAmount * 0.5f;

        // ── Normal engine lights ─────────────────────────────────────────
        foreach (var light in engineLights)
        {
            if (light == null) continue;
            light.color     = Color.Lerp(idleColor, thrustColor, throttle);
            light.intensity = Mathf.Lerp(idleIntensity, thrustIntensity, throttle) * flicker;
            light.range     = Mathf.Lerp(idleRange, thrustRange, throttle);
            light.enabled   = flying || throttle > 0f;
        }

        // ── Afterburner lights ───────────────────────────────────────────
        foreach (var light in afterburnerLights)
        {
            if (light == null) continue;
            light.enabled   = afterburning;
            light.color     = afterburnerColor;
            light.intensity = afterburnerIntensity * flicker;
            light.range     = afterburnerRange;
        }
    }
}
