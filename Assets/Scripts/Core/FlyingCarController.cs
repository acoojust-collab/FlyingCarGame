using UnityEngine;

/// <summary>
/// Realistic flying car controller using Rigidbody aerodynamics.
/// Supports both ground driving and airborne flight modes.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FlyingCarController : MonoBehaviour
{
    [Header("Engine")]
    public float maxThrust = 15000f;          // Newtons
    public float maxHoverThrust = 9810f;      // Enough to counteract gravity (mass * 9.81)
    public float engineSpoolSpeed = 2f;       // How fast thrust builds up
    public float fuelCapacity = 100f;
    public float fuelBurnRate = 0.5f;         // Per second at full thrust

    [Header("Aerodynamics")]
    public float liftCoefficient = 1.5f;
    public float dragCoefficient = 0.3f;
    public float lateralDragCoefficient = 2.0f;
    public float maxSpeed = 120f;             // m/s (~432 km/h)
    public float stallSpeed = 15f;            // m/s — below this, lift drops

    [Header("Control Surfaces")]
    public float pitchTorque = 800f;
    public float rollTorque = 600f;
    public float yawTorque = 400f;
    public float controlResponsiveness = 3f;

    [Header("Hover / VTOL")]
    public float hoverHeight = 3f;            // Target height when hovering
    public float hoverDamping = 5f;
    public bool vtolCapable = true;

    [Header("Ground Drive")]
    public float groundAcceleration = 50f;
    public float groundMaxSpeed = 30f;        // m/s
    public float groundTurnSpeed = 60f;       // degrees/sec
    public float groundDrag = 5f;

    [Header("References")]
    public Transform[] engines;               // Engine transform positions for VFX
    public LayerMask groundLayer;

    // --- Private State ---
    private Rigidbody rb;
    private float currentFuel;
    private float currentThrust;
    private bool isGrounded;
    private bool flightModeActive;
    private float currentSpeed;

    // Input (set by InputHandler)
    [HideInInspector] public float thrustInput;       // 0 to 1
    [HideInInspector] public float pitchInput;        // -1 to 1
    [HideInInspector] public float rollInput;         // -1 to 1
    [HideInInspector] public float yawInput;          // -1 to 1
    [HideInInspector] public bool vtolToggle;
    [HideInInspector] public float steerInput;        // ground steering

    // Public read-only state for HUD
    public float Fuel => currentFuel;
    public float Speed => currentSpeed;
    public float Altitude => transform.position.y;
    public bool IsFlying => flightModeActive;
    public bool IsGrounded => isGrounded;
    public float ThrustPercent => currentThrust / maxThrust;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1000f;    // kg — a real car-sized vehicle
        rb.drag = 0f;       // We handle drag manually
        rb.angularDrag = 2f;
        currentFuel = fuelCapacity;
        flightModeActive = false;
    }

    void Update()
    {
        CheckGrounded();
        currentSpeed = rb.velocity.magnitude;

        // Toggle flight mode
        if (vtolToggle && vtolCapable && currentFuel > 0f)
            flightModeActive = !flightModeActive;
    }

    void FixedUpdate()
    {
        if (currentFuel <= 0f)
        {
            ApplyEmergencyGlide();
            return;
        }

        if (flightModeActive)
            HandleFlight();
        else
            HandleGroundDrive();

        BurnFuel();
    }

    // ─── FLIGHT ─────────────────────────────────────────────────────────────

    void HandleFlight()
    {
        // Spool engine thrust
        float targetThrust = thrustInput * maxThrust;
        currentThrust = Mathf.Lerp(currentThrust, targetThrust, Time.fixedDeltaTime * engineSpoolSpeed);

        Vector3 thrustForce = transform.forward * currentThrust;
        rb.AddForce(thrustForce, ForceMode.Force);

        // Lift (dependent on forward speed — wing effect)
        float speedFactor = Mathf.Clamp01((currentSpeed - stallSpeed) / (maxSpeed - stallSpeed));
        float lift = liftCoefficient * rb.velocity.magnitude * speedFactor * rb.mass * 2f;
        rb.AddForce(Vector3.up * lift, ForceMode.Force);

        // VTOL hover assist when throttle is low and near hover height
        if (vtolCapable && thrustInput < 0.3f)
            ApplyHoverStabilization();

        // Aerodynamic drag (opposing velocity)
        Vector3 drag = -rb.velocity.normalized * dragCoefficient * rb.velocity.sqrMagnitude;
        rb.AddForce(drag, ForceMode.Force);

        // Lateral drag (resist sliding sideways)
        Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
        Vector3 lateralDrag = -transform.right * localVel.x * lateralDragCoefficient * 100f;
        rb.AddForce(lateralDrag, ForceMode.Force);

        // Control torques
        rb.AddTorque(transform.right  * pitchInput * pitchTorque, ForceMode.Force);
        rb.AddTorque(transform.forward * -rollInput * rollTorque, ForceMode.Force);
        rb.AddTorque(transform.up     * yawInput   * yawTorque,  ForceMode.Force);

        // Speed cap
        if (rb.velocity.magnitude > maxSpeed)
            rb.velocity = rb.velocity.normalized * maxSpeed;
    }

    void ApplyHoverStabilization()
    {
        float distToGround = GetGroundDistance();
        float heightError = hoverHeight - distToGround;
        float hoverForce = heightError * hoverDamping * rb.mass * 9.81f;
        hoverForce = Mathf.Clamp(hoverForce, -maxHoverThrust, maxHoverThrust);
        rb.AddForce(Vector3.up * hoverForce, ForceMode.Force);

        // Level out when no pitch/roll input
        if (Mathf.Abs(pitchInput) < 0.05f && Mathf.Abs(rollInput) < 0.05f)
        {
            Vector3 targetUp = Vector3.up;
            Vector3 currentUp = transform.up;
            Vector3 correction = Vector3.Cross(currentUp, targetUp);
            rb.AddTorque(correction * 300f, ForceMode.Force);
        }
    }

    void ApplyEmergencyGlide()
    {
        // Basic glide with no fuel: gravity + minimal lift from speed
        float lift = liftCoefficient * 0.3f * rb.velocity.magnitude;
        rb.AddForce(Vector3.up * lift, ForceMode.Force);
        Vector3 drag = -rb.velocity.normalized * dragCoefficient * 2f * rb.velocity.sqrMagnitude;
        rb.AddForce(drag, ForceMode.Force);
    }

    // ─── GROUND DRIVE ───────────────────────────────────────────────────────

    void HandleGroundDrive()
    {
        currentThrust = 0f;
        if (!isGrounded) return;

        // Forward/backward
        Vector3 driveForce = transform.forward * thrustInput * groundAcceleration * rb.mass;
        rb.AddForce(driveForce, ForceMode.Force);

        // Steering
        float turnAmount = steerInput * groundTurnSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));

        // Ground drag
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(-flatVel * groundDrag, ForceMode.Force);

        // Speed cap on ground
        if (flatVel.magnitude > groundMaxSpeed)
        {
            flatVel = flatVel.normalized * groundMaxSpeed;
            rb.velocity = new Vector3(flatVel.x, rb.velocity.y, flatVel.z);
        }
    }

    // ─── HELPERS ────────────────────────────────────────────────────────────

    void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.2f, groundLayer);
    }

    float GetGroundDistance()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f, groundLayer))
            return hit.distance;
        return 100f;
    }

    void BurnFuel()
    {
        if (flightModeActive && thrustInput > 0.01f)
            currentFuel -= fuelBurnRate * thrustInput * Time.fixedDeltaTime;
        currentFuel = Mathf.Max(0f, currentFuel);
    }
}
