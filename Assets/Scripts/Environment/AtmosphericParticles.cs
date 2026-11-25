using UnityEngine;

/// <summary>
/// Atmospheric ambient particles: floating dust motes at low altitude,
/// rain, and cloud wisps at high altitude. All driven by player height.
/// </summary>
public class AtmosphericParticles : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Low Altitude — Dust & Debris")]
    public ParticleSystem ambientDust;          // floating dust motes at street level
    public float dustMaxAltitude = 50f;

    [Header("Mid Altitude — Rain")]
    public ParticleSystem rain;
    public ParticleSystem rainSplashes;         // impact splashes on the car body
    public float rainMinAltitude = 10f;
    public float rainMaxAltitude = 800f;
    [Range(0f, 1f)] public float rainIntensity = 0.5f;

    [Header("High Altitude — Cloud Wisps")]
    public ParticleSystem cloudWisps;
    public float cloudMinAltitude = 500f;
    public float cloudMaxAltitude = 2000f;

    [Header("Following")]
    public float followSpeed = 10f;

    private bool rainEnabled = true;

    void Update()
    {
        if (player == null) return;

        // Keep particle systems centered on player
        transform.position = Vector3.Lerp(transform.position, player.position, Time.deltaTime * followSpeed);

        float alt = player.position.y;

        UpdateDust(alt);
        UpdateRain(alt);
        UpdateClouds(alt);
    }

    void UpdateDust(float alt)
    {
        if (ambientDust == null) return;
        var emission = ambientDust.emission;
        float t = 1f - Mathf.Clamp01(alt / dustMaxAltitude);
        emission.rateOverTime = t * 40f;
        emission.enabled = t > 0.01f;
    }

    void UpdateRain(float alt)
    {
        if (rain == null) return;
        bool inRainZone = rainEnabled && alt >= rainMinAltitude && alt <= rainMaxAltitude;
        var emission = rain.emission;
        emission.enabled = inRainZone;
        if (inRainZone)
        {
            float altFade = 1f - Mathf.Abs(alt - (rainMinAltitude + rainMaxAltitude) * 0.5f)
                                 / ((rainMaxAltitude - rainMinAltitude) * 0.5f);
            emission.rateOverTime = rainIntensity * altFade * 500f;

            // Rain speed scales with player speed (streaking effect)
        }

        if (rainSplashes != null)
        {
            var sEmission = rainSplashes.emission;
            sEmission.enabled = inRainZone;
            sEmission.rateOverTime = rainIntensity * 20f;
        }
    }

    void UpdateClouds(float alt)
    {
        if (cloudWisps == null) return;
        bool inCloudZone = alt >= cloudMinAltitude && alt <= cloudMaxAltitude;
        var emission = cloudWisps.emission;
        float t = Mathf.InverseLerp(cloudMinAltitude, cloudMinAltitude + 200f, alt)
                * (1f - Mathf.InverseLerp(cloudMaxAltitude - 200f, cloudMaxAltitude, alt));
        emission.enabled = inCloudZone;
        emission.rateOverTime = t * 15f;

        if (inCloudZone)
        {
            var main = cloudWisps.main;
            main.startColor = new Color(1f, 1f, 1f, t * 0.5f);
            main.startSize  = new ParticleSystem.MinMaxCurve(5f, 20f);
        }
    }

    // Call from game manager to toggle rain
    public void SetRain(bool enabled, float intensity = 0.5f)
    {
        rainEnabled = enabled;
        rainIntensity = intensity;
    }
}
