using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Reusable virtual joystick component.
/// Attach to a panel RectTransform. Outputs Horizontal and Vertical (-1 to 1).
/// Works with any number of simultaneous touch fingers.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Visual")]
    public RectTransform background;    // the outer ring
    public RectTransform handle;        // the inner thumb dot
    public Image backgroundImage;
    public Image handleImage;

    [Header("Settings")]
    public float handleRange    = 1f;   // how far handle can travel (0–1 relative to bg radius)
    public float deadZone       = 0.1f; // ignore tiny inputs
    public bool  snapOnRelease  = true; // return to center on finger lift
    public float returnSpeed    = 15f;  // speed of handle snapping back

    [Header("Appearance")]
    public Color normalColor    = new Color(1, 1, 1, 0.3f);
    public Color activeColor    = new Color(1, 1, 1, 0.6f);

    // Output values — read these from MobileUIController
    public Vector2 Direction { get; private set; }
    public float   Horizontal => Direction.x;
    public float   Vertical   => Direction.y;
    public bool    IsActive   { get; private set; }

    private RectTransform rectTransform;
    private Canvas canvas;
    private int    touchId = -1;
    private Vector2 centerPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas        = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        // Smooth return to center when released
        if (!IsActive && snapOnRelease && handle != null)
        {
            handle.anchoredPosition = Vector2.Lerp(
                handle.anchoredPosition, Vector2.zero, Time.deltaTime * returnSpeed);
        }
    }

    // ─── POINTER EVENTS ─────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        IsActive = true;
        touchId  = eventData.pointerId;
        centerPos = RectTransformUtility.WorldToScreenPoint(null, background.position);
        UpdateHandle(eventData.position);
        SetColors(activeColor);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != touchId) return;
        UpdateHandle(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != touchId) return;
        IsActive  = false;
        touchId   = -1;
        Direction = Vector2.zero;
        SetColors(normalColor);
    }

    // ─── HANDLE MOVEMENT ────────────────────────────────────────────────────

    void UpdateHandle(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, screenPos, canvas.worldCamera, out Vector2 localPos);

        float radius    = background.sizeDelta.x * 0.5f * handleRange;
        Vector2 clamped = Vector2.ClampMagnitude(localPos, radius);

        if (handle != null) handle.anchoredPosition = clamped;

        Vector2 raw = clamped / radius;
        Direction   = raw.magnitude < deadZone ? Vector2.zero : raw;
    }

    void SetColors(Color c)
    {
        if (backgroundImage != null) backgroundImage.color = c;
        if (handleImage     != null) handleImage.color     = c;
    }

    // For editor preview
    void OnValidate()
    {
        if (backgroundImage != null) backgroundImage.color = normalColor;
    }
}
