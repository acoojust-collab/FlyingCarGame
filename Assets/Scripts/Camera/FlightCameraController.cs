using UnityEngine;

/// <summary>
/// Smooth chase camera with cockpit mode toggle.
/// Automatically adjusts FOV and lag based on speed.
/// </summary>
public class FlightCameraController : MonoBehaviour
{
    public Transform target;

    [Header("Chase Camera")]
    public Vector3 chaseOffset = new Vector3(0f, 3f, -10f);
    public float followSpeed = 5f;
    public float rotationSpeed = 4f;

    [Header("Dynamic FOV")]
    public float baseFOV = 65f;
    public float maxFOV = 90f;
    public float fovSpeedFactor = 0.3f;         // how much speed inflates FOV

    [Header("Cockpit Mode")]
    public bool cockpitMode = false;
    public Vector3 cockpitOffset = new Vector3(0f, 0.5f, 0.5f);
    public KeyCode toggleKey = KeyCode.C;

    private Camera cam;
    private FlyingCarController carController;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target != null)
            carController = target.GetComponent<FlyingCarController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            cockpitMode = !cockpitMode;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (cockpitMode)
            UpdateCockpitCamera();
        else
            UpdateChaseCamera();

        UpdateFOV();
    }

    void UpdateChaseCamera()
    {
        Vector3 desiredPos = target.TransformPoint(chaseOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followSpeed);

        Quaternion desiredRot = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * rotationSpeed);
    }

    void UpdateCockpitCamera()
    {
        transform.position = target.TransformPoint(cockpitOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * 15f);
    }

    void UpdateFOV()
    {
        if (cam == null || carController == null) return;
        float targetFOV = baseFOV + carController.Speed * fovSpeedFactor;
        targetFOV = Mathf.Clamp(targetFOV, baseFOV, maxFOV);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 3f);
    }
}
