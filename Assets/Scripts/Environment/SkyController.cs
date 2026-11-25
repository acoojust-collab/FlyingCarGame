using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Dynamic sky controller using Polyhaven HDRI cubemaps.
/// Blends between day, sunset, and night HDRIs based on time-of-day.
/// Also drives URP fog and ambient light colour.
/// 
/// SETUP:
/// 1. Download HDRIs from polyhaven.com/hdris (search: sky, sunset, night)
/// 2. Import into Unity, set Texture Type = Cubemap
/// 3. Assign to dayHDRI, sunsetHDRI, nightHDRI below
/// </summary>
public class SkyController : MonoBehaviour
{
    [Header("Polyhaven HDRI Cubemaps")]
    public Cubemap dayHDRI;        // e.g. "kloppenheim_06" — clear blue sky
    public Cubemap sunsetHDRI;     // e.g. "evening_road_01" — golden hour
    public Cubemap nightHDRI;      // e.g. "moonlit_golf" — night with stars

    [Header("Time of Day")]
    [Range(0f, 24f)] public float timeOfDay = 10f;   // 0–24 hour clock
    public float daySpeed = 0.5f;                     // set 0 to freeze time
    public bool autoAdvanceTime = true;

    [Header("Sun")]
    public Light sunLight;
    public Gradient sunColorOverDay;        // set in Inspector
    public AnimationCurve sunIntensityCurve;

    [Header("Fog (URP)")]
    public bool enableFog = true;
    public Color dayFogColor    = new Color(0.8f, 0.9f, 1f);
    public Color nightFogColor  = new Color(0.05f, 0.05f, 0.15f);
    public float dayFogDensity    = 0.002f;
    public float nightFogDensity  = 0.005f;

    [Header("URP Volume")]
    public Volume postVolume;

    private float blendDayToSunset;
    private float blendSunsetToNight;

    void Update()
    {
        if (autoAdvanceTime)
            timeOfDay = (timeOfDay + Time.deltaTime * daySpeed / 60f) % 24f;

        UpdateSky();
        UpdateSun();
        UpdateFog();
    }

    void UpdateSky()
    {
        // 6–17 = day, 17–20 = sunset, 20–6 = night
        float t = timeOfDay;

        Cubemap from, to;
        float blend;

        if (t >= 6f && t < 17f)
        {
            from = dayHDRI; to = dayHDRI; blend = 0f;
        }
        else if (t >= 17f && t < 20f)
        {
            from = dayHDRI; to = sunsetHDRI;
            blend = Mathf.InverseLerp(17f, 20f, t);
        }
        else if (t >= 20f && t < 22f)
        {
            from = sunsetHDRI; to = nightHDRI;
            blend = Mathf.InverseLerp(20f, 22f, t);
        }
        else
        {
            from = nightHDRI; to = nightHDRI; blend = 0f;
        }

        // Apply to environment lighting
        if (from != null) RenderSettings.skybox?.SetTexture("_Tex", from);
        RenderSettings.ambientIntensity = Mathf.Lerp(GetDayAmbient(), GetNightAmbient(), blend);
    }

    float GetDayAmbient()   => timeOfDay > 6f && timeOfDay < 20f ? 1.2f : 0.3f;
    float GetNightAmbient() => 0.1f;

    void UpdateSun()
    {
        if (sunLight == null) return;

        // Sun angle: 0° at midnight, peaks at noon
        float sunAngle = (timeOfDay / 24f) * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 30f, 0f);

        if (sunColorOverDay != null)
            sunLight.color = sunColorOverDay.Evaluate(timeOfDay / 24f);

        if (sunIntensityCurve != null)
            sunLight.intensity = sunIntensityCurve.Evaluate(timeOfDay / 24f);

        // Disable sun at night
        sunLight.enabled = timeOfDay > 5.5f && timeOfDay < 20.5f;
    }

    void UpdateFog()
    {
        if (!enableFog) return;
        RenderSettings.fog = true;
        float nightBlend = Mathf.InverseLerp(18f, 22f, timeOfDay);
        RenderSettings.fogColor   = Color.Lerp(dayFogColor, nightFogColor, nightBlend);
        RenderSettings.fogDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, nightBlend);
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
    }

    // Public control for transitions or cutscenes
    public void SetTime(float hour) => timeOfDay = Mathf.Clamp(hour, 0f, 24f);
}
