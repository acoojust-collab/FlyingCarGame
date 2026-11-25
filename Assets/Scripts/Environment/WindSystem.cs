using UnityEngine;

/// <summary>
/// Simulates realistic wind that affects the flying car's Rigidbody.
/// Wind direction shifts gradually; turbulence adds randomness at altitude.
/// </summary>
public class WindSystem : MonoBehaviour
{
    [Header("Base Wind")]
    public float windSpeed = 5f;              // m/s base
    public Vector3 windDirection = Vector3.right;
    public float directionChangeSpeed = 0.1f; // how fast wind direction drifts

    [Header("Turbulence")]
    public float turbulenceStrength = 2f;
    public float turbulenceFrequency = 0.5f;
    public float altitudeTurbulenceMultiplier = 0.02f; // more turbulence at height

    [Header("Gusts")]
    public float gustInterval = 10f;
    public float gustStrength = 20f;
    public float gustDuration = 1.5f;

    private Rigidbody carRb;
    private float gustTimer;
    private float gustActiveTimer;
    private bool isGusting;
    private Vector3 currentGustDir;
    private float noiseOffsetX, noiseOffsetZ;

    public static WindSystem Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        noiseOffsetX = Random.Range(0f, 1000f);
        noiseOffsetZ = Random.Range(0f, 1000f);
    }

    public void RegisterCar(Rigidbody rb) => carRb = rb;

    void FixedUpdate()
    {
        if (carRb == null) return;

        // Drift wind direction slowly
        windDirection = Vector3.Slerp(windDirection,
            new Vector3(Mathf.Sin(Time.time * directionChangeSpeed), 0,
                        Mathf.Cos(Time.time * directionChangeSpeed)), Time.fixedDeltaTime * 0.5f).normalized;

        float altitude = carRb.position.y;

        // Base wind force
        Vector3 windForce = windDirection * windSpeed * carRb.mass * 0.5f;

        // Turbulence using Perlin noise
        float t = Time.time * turbulenceFrequency;
        float turbX = (Mathf.PerlinNoise(t, noiseOffsetX) - 0.5f) * 2f;
        float turbZ = (Mathf.PerlinNoise(noiseOffsetZ, t) - 0.5f) * 2f;
        float altFactor = 1f + altitude * altitudeTurbulenceMultiplier;
        Vector3 turbulence = new Vector3(turbX, 0f, turbZ) * turbulenceStrength * altFactor * carRb.mass;

        // Gusts
        Vector3 gustForce = Vector3.zero;
        gustTimer += Time.fixedDeltaTime;
        if (gustTimer >= gustInterval && !isGusting)
        {
            isGusting = true;
            gustTimer = 0f;
            gustActiveTimer = 0f;
            currentGustDir = Random.onUnitSphere;
            currentGustDir.y = Mathf.Abs(currentGustDir.y) * 0.3f; // mostly horizontal
        }
        if (isGusting)
        {
            gustActiveTimer += Time.fixedDeltaTime;
            float gustFade = 1f - (gustActiveTimer / gustDuration);
            gustForce = currentGustDir * gustStrength * gustFade * carRb.mass;
            if (gustActiveTimer >= gustDuration) isGusting = false;
        }

        carRb.AddForce(windForce + turbulence + gustForce, ForceMode.Force);
    }

    public Vector3 GetCurrentWind() => windDirection * windSpeed;
}
