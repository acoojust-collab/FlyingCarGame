using UnityEngine;

/// <summary>
/// Controls all engine VFX: exhaust particles, heat shimmer, boost trail,
/// and engine glow. Reacts to throttle, speed, and flight mode in real time.
/// </summary>
public class EngineVFXController : MonoBehaviour
{
    [Header("References")]
    public FlyingCarController car;

    [Header("Engine Exhaust (assign 1 per engine nozzle)")]
    public ParticleSystem[] exhaustParticles;
    public ParticleSystem[] afterburnerParticles;   // visible during heavy throttle

    [Header("Boost / Trail")]
    public TrailRenderer[] engineTrails;            // wingtip or nozzle trails
    public ParticleSystem boostBurst;               // one-shot burst on VTOL engage

    [Header("Hover Thrusters")]
    public ParticleSystem[] hoverThrusters;         // bottom-facing when hovering low
    public ParticleSystem groundDustEffect;         // dust kicked up near ground

    [Header("Speed Lines (Cockpit feel)")]
    public ParticleSystem speedLinesParticles;      // forward-facing streaks

    [Header("Heat Shimmer")]
    public ParticleSystem[] heatShimmerParticles;   // subtle heat distortion above engines

    [Header("Tuning")]
    public float trailMinThrottle = 0.4f;           // throttle threshold for trails
    public float afterburnerThreshold = 0.85f;      // throttle for afterburner kick
    public float speedLinesThreshold = 60f;         // km/h to show speed lines
    public float hoverDustHeight = 5f;              // altitude below which dust shows

    private bool wasFlying = false;
    private float prevThrottle = 0f;

    void Update()
    {
        if (car == null) return;

        float throttle = car.ThrustPercent;
        float speedKmh = car.Speed * 3.6f;
        bool flying    = car.IsFlying;
        float altitude = car.Altitude;

        UpdateExhaust(throttle, flying);
        UpdateAfterburner(throttle, flying);
        UpdateTrails(throttle, flying);
        UpdateHoverThrusters(flying, altitude);
        UpdateGroundDust(flying, altitude);
        UpdateSpeedLines(speedKmh, flying);
        UpdateHeatShimmer(throttle, flying);
        HandleModeTransition(flying);

        wasFlying    = flying;
        prevThrottle = throttle;
    }

    // ─── EXHAUST ────────────────────────────────────────────────────────────

    void UpdateExhaust(float throttle, bool flying)
    {
        foreach (var ps in exhaustParticles)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            var main     = ps.main;

            if (flying)
            {
                emission.enabled = true;
                // Rate scales 20–200 particles/s with throttle
                emission.rateOverTime = Mathf.Lerp(20f, 200f, throttle);
                // Particle size and speed increase with throttle
                main.startSpeed    = new ParticleSystem.MinMaxCurve(Mathf.Lerp(2f, 12f, throttle));
                main.startSize     = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0.1f, 0.4f, throttle));
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f);
                // Color: white-hot at high throttle, grey at idle
                main.startColor    = Color.Lerp(new Color(0.6f, 0.6f, 0.6f, 0.5f),
                                                new Color(1f, 0.95f, 0.8f, 0.8f), throttle);
            }
            else
            {
                emission.rateOverTime = 5f;   // gentle idle smoke on ground
                main.startSpeed = new ParticleSystem.MinMaxCurve(1f);
                main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            }
        }
    }

    // ─── AFTERBURNER ────────────────────────────────────────────────────────

    void UpdateAfterburner(float throttle, bool flying)
    {
        bool shouldBurn = flying && throttle >= afterburnerThreshold;
        foreach (var ps in afterburnerParticles)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            emission.enabled = shouldBurn;

            if (shouldBurn)
            {
                var main = ps.main;
                // Orange-blue plasma cone
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(0f, 0.5f, 1f, 0.9f),     // blue core
                    new Color(1f, 0.4f, 0f, 0.7f));     // orange outer
                main.startSpeed    = new ParticleSystem.MinMaxCurve(15f, 25f);
                main.startSize     = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                emission.rateOverTime = 300f;
            }
        }
    }

    // ─── TRAILS ─────────────────────────────────────────────────────────────

    void UpdateTrails(float throttle, bool flying)
    {
        bool trailActive = flying && throttle >= trailMinThrottle;
        foreach (var trail in engineTrails)
        {
            if (trail == null) continue;
            trail.emitting = trailActive;
            trail.time     = Mathf.Lerp(0.2f, 1.0f, throttle);
            trail.startWidth  = Mathf.Lerp(0.05f, 0.25f, throttle);
            // Fade color: bright white to transparent
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f),
                         new GradientColorKey(new Color(0.8f, 0.9f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.8f * throttle, 0f),
                         new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;
        }
    }

    // ─── HOVER THRUSTERS ────────────────────────────────────────────────────

    void UpdateHoverThrusters(bool flying, float altitude)
    {
        bool hovering = flying && altitude < hoverDustHeight * 3f;
        foreach (var ps in hoverThrusters)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            emission.enabled = hovering;
            if (hovering)
            {
                float intensity = 1f - Mathf.Clamp01(altitude / (hoverDustHeight * 3f));
                emission.rateOverTime = Mathf.Lerp(10f, 150f, intensity);
            }
        }
    }

    // ─── GROUND DUST ────────────────────────────────────────────────────────

    void UpdateGroundDust(bool flying, float altitude)
    {
        if (groundDustEffect == null) return;
        bool nearGround = flying && altitude < hoverDustHeight;
        var emission = groundDustEffect.emission;
        emission.enabled = nearGround;
        if (nearGround)
        {
            float dustAmount = 1f - (altitude / hoverDustHeight);
            emission.rateOverTime = dustAmount * 80f;
            var main = groundDustEffect.main;
            main.startColor = new Color(0.8f, 0.7f, 0.5f, dustAmount * 0.6f);
        }
    }

    // ─── SPEED LINES ────────────────────────────────────────────────────────

    void UpdateSpeedLines(float speedKmh, bool flying)
    {
        if (speedLinesParticles == null) return;
        bool shouldShow = flying && speedKmh > speedLinesThreshold;
        var emission = speedLinesParticles.emission;
        emission.enabled = shouldShow;
        if (shouldShow)
        {
            float intensity = Mathf.InverseLerp(speedLinesThreshold, 300f, speedKmh);
            emission.rateOverTime = Mathf.Lerp(10f, 100f, intensity);
            var main = speedLinesParticles.main;
            main.startSpeed    = new ParticleSystem.MinMaxCurve(Mathf.Lerp(5f, 30f, intensity));
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f);
            main.startColor    = new Color(1f, 1f, 1f, intensity * 0.4f);
        }
    }

    // ─── HEAT SHIMMER ───────────────────────────────────────────────────────

    void UpdateHeatShimmer(float throttle, bool flying)
    {
        foreach (var ps in heatShimmerParticles)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            emission.enabled = flying && throttle > 0.3f;
            emission.rateOverTime = throttle * 30f;
        }
    }

    // ─── MODE TRANSITION ────────────────────────────────────────────────────

    void HandleModeTransition(bool flying)
    {
        // Trigger burst particle when switching to flight mode
        if (flying && !wasFlying && boostBurst != null)
            boostBurst.Play();
    }
}
