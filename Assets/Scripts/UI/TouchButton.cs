using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Touch button supporting tap, hold, and toggle modes.
/// Used for VTOL toggle, camera switch, boost, etc.
/// </summary>
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonMode { Tap, Hold, Toggle }

    [Header("Mode")]
    public ButtonMode mode = ButtonMode.Tap;

    [Header("Visual")]
    public Image  buttonImage;
    public TextMeshProUGUI label;
    public Color  normalColor  = new Color(1, 1, 1, 0.25f);
    public Color  pressedColor = new Color(1, 1, 1, 0.65f);
    public Color  toggleOnColor = new Color(0.3f, 0.9f, 1f, 0.7f);

    [Header("Feedback")]
    public float scaleOnPress = 0.9f;
    public float scaleSpeed   = 10f;

    // State
    public bool IsPressed     { get; private set; }
    public bool IsToggleOn    { get; private set; }
    public bool WasTappedThisFrame { get; private set; }

    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        UpdateVisuals();
    }

    void LateUpdate()
    {
        // Reset tap flag after one frame
        WasTappedThisFrame = false;

        // Animate scale
        Vector3 target = IsPressed
            ? originalScale * scaleOnPress
            : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;

        if (mode == ButtonMode.Toggle)
        {
            IsToggleOn = !IsToggleOn;
            WasTappedThisFrame = true;
        }
        else if (mode == ButtonMode.Tap)
        {
            WasTappedThisFrame = true;
        }

        UpdateVisuals();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (buttonImage == null) return;

        if (mode == ButtonMode.Toggle)
            buttonImage.color = IsToggleOn ? toggleOnColor : normalColor;
        else
            buttonImage.color = IsPressed ? pressedColor : normalColor;
    }
}
