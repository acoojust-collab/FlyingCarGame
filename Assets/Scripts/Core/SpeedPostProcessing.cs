using UnityEngine;

/// <summary>
/// Screen-space motion blur and chromatic aberration driver.
/// Requires Unity URP with Post Processing enabled on the camera.
/// Uses UnityEngine.Rendering.Universal for URP Volume components.
/// </summary>
public class SpeedPostProcessing : MonoBehaviour
{
    [Header("References")]
    public FlyingCarController car;

    [Header("Motion Blur")]
    public float maxShutterAngle = 270f;         // 0–360
    public float motionBlurSpeedThreshold = 50f; // km/h

    [Header("Chromatic Aberration")]
    public float maxChromaticIntensity = 0.5f;
    public float chromaticSpeedThreshold = 150f; // km/h

    [Header("Vignette")]
    public float maxVignetteIntensity = 0.5f;
    public float vignetteSpeedThreshold = 100f;

    [Header("Lens Distortion (G-force)")]
    public float maxLensDistortion = -0.4f;      // negative = barrel
    public float gForceThreshold   = 2f;

    // URP Volume components (auto-fetched)
    private UnityEngine.Rendering.Universal.MotionBlur motionBlur;
    private UnityEngine.Rendering.Universal.ChromaticAberration chromatic;
    private UnityEngine.Rendering.Universal.Vignette vignette;
    private UnityEngine.Rendering.Universal.LensDistortion lensDistortion;
    private UnityEngine.Rendering.Volume volume;

    private Vector3 prevVelocity;
    private float gForce;

    void Start()
    {
        volume = GetComponent<UnityEngine.Rendering.Volume>();
        if (volume == null) return;

        volume.profile.TryGet(out motionBlur);
        volume.profile.TryGet(out chromatic);
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out lensDistortion);

        prevVelocity = Vector3.zero;
    }

    void Update()
    {
        if (car == null) return;

        float speedKmh = car.Speed * 3.6f;

        // G-force estimate from velocity change
        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 accel = (rb.velocity - prevVelocity) / Time.deltaTime;
            gForce = accel.magnitude / 9.81f;
            prevVelocity = rb.velocity;
        }

        UpdateMotionBlur(speedKmh);
        UpdateChromatic(speedKmh);
        UpdateVignette(speedKmh);
        UpdateLensDistortion(gForce);
    }

    void UpdateMotionBlur(float speedKmh)
    {
        if (motionBlur == null) return;
        float t = Mathf.InverseLerp(0f, motionBlurSpeedThreshold * 3f, speedKmh);
        motionBlur.shutterAngle.Override(Mathf.Lerp(0f, maxShutterAngle, t));
        motionBlur.active = speedKmh > motionBlurSpeedThreshold;
    }

    void UpdateChromatic(float speedKmh)
    {
        if (chromatic == null) return;
        float t = Mathf.InverseLerp(chromaticSpeedThreshold, chromaticSpeedThreshold * 2f, speedKmh);
        chromatic.intensity.Override(Mathf.Lerp(0f, maxChromaticIntensity, t));
    }

    void UpdateVignette(float speedKmh)
    {
        if (vignette == null) return;
        float t = Mathf.InverseLerp(vignetteSpeedThreshold, vignetteSpeedThreshold * 2f, speedKmh);
        vignette.intensity.Override(Mathf.Lerp(0.2f, maxVignetteIntensity, t));
    }

    void UpdateLensDistortion(float g)
    {
        if (lensDistortion == null) return;
        float t = Mathf.InverseLerp(gForceThreshold, gForceThreshold * 3f, g);
        lensDistortion.intensity.Override(Mathf.Lerp(0f, maxLensDistortion, t));
    }
}
