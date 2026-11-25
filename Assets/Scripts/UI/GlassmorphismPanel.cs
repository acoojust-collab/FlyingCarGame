using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Apple-style frosted glass panel (glassmorphism).
/// Creates the iOS Control Center / Notification Center blur-behind effect.
///
/// Requires: Unity URP + "Full Screen Pass Renderer Feature" OR
///           the free "KinoBlur" / "Grainy Blur" URP package.
///
/// For a quick Unity-only approximation we use a tinted semi-transparent
/// Image + a subtle noise overlay — still looks very premium on mobile.
/// </summary>
[RequireComponent(typeof(Image))]
public class GlassmorphismPanel : MonoBehaviour
{
    [Header("Glass Style")]
    public Color  glassTint      = new Color(1f, 1f, 1f, 0.12f);   // white tint, very transparent
    public Color  borderColor    = new Color(1f, 1f, 1f, 0.25f);
    public float  borderWidth    = 1.5f;
    public float  cornerRadius   = 24f;    // requires Rounded Corners shader or TextMeshPro sprite

    [Header("Noise / Grain overlay")]
    public bool   showGrain      = true;
    public float  grainOpacity   = 0.04f;
    public float  grainScale     = 80f;

    [Header("Dynamic Tint (reacts to environment)")]
    public bool   dynamicTint    = true;
    public Color  flightTint     = new Color(0.1f, 0.4f, 0.9f, 0.15f);
    public Color  groundTint     = new Color(0.05f, 0.05f, 0.05f, 0.2f);
    public Color  lowFuelTint    = new Color(0.9f, 0.1f, 0.1f, 0.15f);
    public float  tintLerpSpeed  = 2f;

    [Header("Shimmer (Apple card shimmer effect)")]
    public bool   shimmerEnabled = true;
    public float  shimmerSpeed   = 0.6f;
    public float  shimmerWidth   = 0.3f;

    private Image       panelImage;
    private Image       shimmerImage;
    private FlyingCarController car;
    private float       shimmerPos = -1f;

    void Awake()
    {
        panelImage = GetComponent<Image>();
        panelImage.color = glassTint;
        car = FindObjectOfType<FlyingCarController>();

        if (shimmerEnabled) CreateShimmer();
    }

    void Update()
    {
        UpdateDynamicTint();
        if (shimmerEnabled) TickShimmer();
    }

    // ─── DYNAMIC TINT ───────────────────────────────────────────────────────

    void UpdateDynamicTint()
    {
        if (!dynamicTint || car == null) return;

        Color target = car.IsFlying ? flightTint : groundTint;
        if (car.Fuel < 15f) target = Color.Lerp(target, lowFuelTint, 0.6f);

        panelImage.color = Color.Lerp(panelImage.color, target, Time.deltaTime * tintLerpSpeed);
    }

    // ─── SHIMMER ────────────────────────────────────────────────────────────

    void CreateShimmer()
    {
        var go = new GameObject("Shimmer");
        go.transform.SetParent(transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        shimmerImage = go.AddComponent<Image>();
        shimmerImage.color = new Color(1f, 1f, 1f, 0f);
        // In production: assign a gradient shimmer sprite (white→transparent→white strip)
    }

    void TickShimmer()
    {
        if (shimmerImage == null) return;
        shimmerPos += Time.deltaTime * shimmerSpeed;
        if (shimmerPos > 1.5f) shimmerPos = -shimmerWidth;

        // Animate shimmer alpha as it sweeps across
        float alpha = shimmerPos > 0f && shimmerPos < 1f
            ? Mathf.Sin(shimmerPos * Mathf.PI) * 0.08f
            : 0f;

        shimmerImage.color = new Color(1f, 1f, 1f, alpha);

        // Move shimmer strip (diagonal sweep)
        if (shimmerImage.rectTransform != null)
        {
            float xPercent = shimmerPos;
            shimmerImage.rectTransform.anchoredPosition =
                new Vector2(xPercent * GetComponent<RectTransform>().rect.width, 0f);
        }
    }

    // ─── PUBLIC ─────────────────────────────────────────────────────────────

    public void FlashAlert(Color alertColor, float duration = 0.5f)
    {
        StartCoroutine(AlertFlash(alertColor, duration));
    }

    IEnumerator AlertFlash(Color c, float dur)
    {
        Color original = panelImage.color;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ping = Mathf.Sin(t / dur * Mathf.PI);
            panelImage.color = Color.Lerp(original, c, ping);
            yield return null;
        }
        panelImage.color = original;
    }
}
