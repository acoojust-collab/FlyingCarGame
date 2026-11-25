using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Premium notification system — Apple Dynamic Island style.
/// Shows toast alerts for: coin milestones, low fuel, altitude records,
/// combo streaks, flight mode changes.
/// Appears at top-center, animates in with spring bounce, auto-dismisses.
/// </summary>
public class DynamicNotification : MonoBehaviour
{
    public static DynamicNotification Instance { get; private set; }

    [Header("Island Panel")]
    public RectTransform islandRoot;        // assign the top-center pill panel
    public Image         islandBG;
    public TextMeshProUGUI iconText;        // emoji icon
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public CanvasGroup    canvasGroup;

    [Header("Sizing")]
    public Vector2 collapsedSize  = new Vector2(120f, 36f);   // pill shape at rest
    public Vector2 expandedSize   = new Vector2(320f, 72f);   // expanded notification
    public float   expandDuration = 0.35f;
    public float   displayTime    = 2.5f;
    public float   collapseDuration = 0.25f;

    [Header("Style")]
    public Color bgColor         = new Color(0.08f, 0.08f, 0.08f, 0.95f);
    public Color successColor    = new Color(0.1f,  0.8f,  0.3f,  0.95f);
    public Color warningColor    = new Color(1f,    0.6f,  0f,    0.95f);
    public Color dangerColor     = new Color(0.9f,  0.1f,  0.1f,  0.95f);
    public Color infoColor       = new Color(0.1f,  0.5f,  1f,    0.95f);

    public enum NotifType { Info, Success, Warning, Danger }

    private Queue<(string icon, string title, string sub, NotifType type)> queue
        = new Queue<(string, string, string, NotifType)>();
    private bool isShowing = false;

    void Awake()
    {
        Instance = this;
        if (islandRoot != null)
        {
            islandRoot.sizeDelta = collapsedSize;
            if (canvasGroup != null) canvasGroup.alpha = 0.4f;
        }
    }

    void Start()
    {
        // Subscribe to game events
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinsChanged += OnCoinsChanged;
    }

    void OnDestroy()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinsChanged -= OnCoinsChanged;
    }

    // ─── PUBLIC PUSH ────────────────────────────────────────────────────────

    public void Push(string icon, string title, string subtitle = "", NotifType type = NotifType.Info)
    {
        queue.Enqueue((icon, title, subtitle, type));
        if (!isShowing) StartCoroutine(ShowNext());
    }

    // ─── QUEUE PROCESSOR ────────────────────────────────────────────────────

    IEnumerator ShowNext()
    {
        while (queue.Count > 0)
        {
            isShowing = true;
            var (icon, title, sub, type) = queue.Dequeue();
            yield return ShowNotification(icon, title, sub, type);
            yield return new WaitForSeconds(0.15f); // gap between notifs
        }
        isShowing = false;
        yield return CollapseIsland();
    }

    IEnumerator ShowNotification(string icon, string title, string sub, NotifType type)
    {
        // Set content
        if (iconText     != null) iconText.text     = icon;
        if (titleText    != null) titleText.text     = title;
        if (subtitleText != null) subtitleText.text  = sub;

        Color bgTarget = type switch
        {
            NotifType.Success => successColor,
            NotifType.Warning => warningColor,
            NotifType.Danger  => dangerColor,
            _                 => bgColor
        };

        // Expand animation (spring)
        yield return AnimateIsland(collapsedSize, expandedSize, bgTarget, expandDuration);
        LiquidUIAnimator.TriggerHaptic(type == NotifType.Warning || type == NotifType.Danger
            ? LiquidUIAnimator.HapticType.Medium
            : LiquidUIAnimator.HapticType.Light);

        // Hold
        yield return new WaitForSeconds(displayTime);

        // Collapse
        yield return AnimateIsland(expandedSize, collapsedSize, bgColor, collapseDuration);
    }

    IEnumerator AnimateIsland(Vector2 fromSize, Vector2 toSize, Color toColor, float dur)
    {
        if (islandRoot == null) yield break;

        Color fromColor = islandBG != null ? islandBG.color : bgColor;
        float fromAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        float toAlpha   = toSize == expandedSize ? 1f : 0.5f;

        bool expanding  = toSize.x > fromSize.x;
        Vector2 startSz = islandRoot.sizeDelta;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ease = expanding ? EaseOutBack(t / dur) : EaseInCubic(t / dur);

            islandRoot.sizeDelta = Vector2.LerpUnclamped(startSz, toSize, ease);
            if (islandBG     != null) islandBG.color   = Color.Lerp(fromColor, toColor, t / dur);
            if (canvasGroup  != null) canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t / dur);

            // Show text only when expanded enough
            bool showText = islandRoot.sizeDelta.x > collapsedSize.x * 1.5f;
            if (titleText    != null) titleText.gameObject.SetActive(showText);
            if (subtitleText != null) subtitleText.gameObject.SetActive(showText);

            yield return null;
        }
        islandRoot.sizeDelta = toSize;
    }

    IEnumerator CollapseIsland()
    {
        if (islandRoot == null) yield break;
        float t = 0f, dur = 0.3f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0.5f, 0.3f, t / dur);
            yield return null;
        }
    }

    // ─── GAME EVENTS ────────────────────────────────────────────────────────

    void OnCoinsChanged(int total)
    {
        if (total > 0 && total % 10 == 0)
            Push("🪙", $"{total} Coins!", "Keep flying!", NotifType.Success);
    }

    public void NotifyFlightMode(bool isFlying) =>
        Push(isFlying ? "✈️" : "🚗", isFlying ? "Flight Mode" : "Drive Mode",
             isFlying ? "Engines engaged" : "Landed safely", NotifType.Info);

    public void NotifyLowFuel(float pct) =>
        Push("⛽", "Low Fuel", $"{pct:F0}% remaining", NotifType.Warning);

    public void NotifyCombo(int combo) =>
        Push("🔥", $"×{combo} Combo!", "Incredible!", NotifType.Success);

    public void NotifyStall() =>
        Push("⚠️", "Stall Warning", "Increase throttle!", NotifType.Danger);

    // ─── EASING ─────────────────────────────────────────────────────────────

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
    static float EaseInCubic(float t) => t * t * t;
}
