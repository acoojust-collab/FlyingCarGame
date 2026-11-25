using UnityEngine;

/// <summary>
/// Individual coin behaviour: rotation, hover bob, collection magnet pull,
/// particle burst on pickup, and sound.
/// Attach to any coin prefab with a SphereCollider (set Is Trigger = true).
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class Coin : MonoBehaviour
{
    [Header("Appearance")]
    public int value = 1;
    public float rotateSpeed = 90f;         // degrees/sec
    public float bobHeight   = 0.3f;        // meters up and down
    public float bobSpeed    = 2f;

    [Header("Magnet Pull")]
    public float magnetRadius  = 6f;        // distance at which coin starts flying to car
    public float magnetForce   = 12f;       // speed toward car

    [Header("VFX / SFX")]
    public ParticleSystem collectBurst;     // assign a small burst particle prefab
    public AudioClip collectSound;
    [Range(0f, 1f)] public float volume = 0.6f;

    [Header("Glow")]
    public Light coinLight;                 // optional point light child
    public float glowIntensity = 1.5f;

    private Vector3 startPos;
    private Transform playerTransform;
    private bool collected = false;
    private float bobOffset;

    void Start()
    {
        startPos  = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);  // stagger bob phase

        // Find player on start
        var car = FindObjectOfType<FlyingCarController>();
        if (car != null) playerTransform = car.transform;

        if (coinLight != null) coinLight.intensity = glowIntensity;
    }

    void Update()
    {
        if (collected) return;

        // Rotate
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // Bob
        float bob = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
        transform.position = startPos + Vector3.up * bob;

        // Magnet pull toward player
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < magnetRadius)
            {
                float pullStrength = Mathf.InverseLerp(magnetRadius, 0f, dist);
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    playerTransform.position,
                    magnetForce * pullStrength * Time.deltaTime);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;
        Collect();
    }

    void Collect()
    {
        // Notify coin manager
        CoinManager.Instance?.AddCoins(value);

        // VFX burst
        if (collectBurst != null)
        {
            var burst = Instantiate(collectBurst, transform.position, Quaternion.identity);
            Destroy(burst.gameObject, 2f);
        }

        // SFX
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position, volume);

        Destroy(gameObject);
    }
}
