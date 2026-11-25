using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Apple-style liquid UI animator.
/// Adds spring physics, bounce, blur-glass panels, smooth fade transitions,
/// and fluid scale/position animations to any UI element.
/// Inspired by iOS Human Interface Guidelines spring damping curves.
/// </summary>
public class LiquidUIAnimator : MonoBehaviour
{
    // ── Spring parameters (matches iOS UISpringTimingParameters) ─────────────
    [Header("Spring Physics")]
    [Range(0f, 1f)]
    public float damping     = 0.7f;    // 1 = no bounce, 0 = infinite bounce
    public float stiffness   = 300f;    // spring strength
    public float mass        = 1f;      // virtual mass

    // ── Entrance animation ───────────────────────────────────────────────────
    [Header("Entrance")]
    public bool  animateOnEnable  = true;
    public float entranceDelay    = 0f;
    public EntranceType entrance  = EntranceType.ScaleBounce;

    public enum EntranceType
    {
        ScaleBounce,    // scale 0.7 → 1.05 → 1.0 (iOS app open feel)
        SlideUp,        // slide from below + fade
        SlideDown,
        FadeBlur,       // fade in with blur dissolve
        Morph           // scale + fade combined
    }

    // ── Exit animation ───────────────────────────────────────────────────────
    [Header("Exit")]
    public ExitType exit         = ExitType.ScaleShrink;
    public float exitDuration    = 0.22f;

    public enum ExitType { ScaleShrink, SlideDown, FadeOut }

    // ── Idle breathing (subtle scale pulse like Apple Watch complications) ───
    [Header("Idle Breathing")]
    public bool  breathe         = false;
    public float breatheScale    = 1.02f;
    public float breatheSpeed    = 1.2f;

    // ── Haptic ───────────────────────────────────────────────────────────────
    [Header("Haptics")]
    public bool hapticOnShow     = true;
    public HapticType hapticType = HapticType.Light;

    public enum HapticType { Light, Medium, Heavy, Success, Warning }

    // ── Private ───────────────────────────────────────────────────────────────
    private RectTransform rt;
    private CanvasGroup   cg;
    private Vector3       originalScale;
    private Vector2       originalPos;
    private Coroutine     currentAnim;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        originalScale = rt.localScale;
        originalPos   = rt.anchoredPosition;
    }

    void OnEnable()
    {
        if (animateOnEnable)
            Show(entranceDelay);
    }

    void Update()
    {
        if (breathe)
        {
            float s = 1f + Mathf.Sin(Time.time * breatheSpeed * Mathf.PI) * (breatheScale - 1f);
            rt.localScale = originalScale * s;
        }
    }

    // ─── PUBLIC API ─────────────────────────────────────────────────────────

    public void Show(float delay = 0f)
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(PlayEntrance(delay));
        if (hapticOnShow) TriggerHaptic(hapticType);
    }

    public void Hide()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(PlayExit());
    }

    public void Pulse()
    {
        StartCoroutine(PulseRoutine());
    }

    // ─── ENTRANCE ROUTINES ──────────────────────────────────────────────────

    IEnumerator PlayEntrance(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        switch (entrance)
        {
            case EntranceType.ScaleBounce:  yield return SpringScale(Vector3.zero, originalScale); break;
            case EntranceType.SlideUp:      yield return SlideIn(Vector2.down * 80f); break;
            case EntranceType.SlideDown:    yield return SlideIn(Vector2.up * 80f); break;
            case EntranceType.FadeBlur:     yield return FadeIn(); break;
            case EntranceType.Morph:        yield return MorphIn(); break;
        }
    }

    // iOS-style spring scale using damped oscillation
    IEnumerator SpringScale(Vector3 from, Vector3 to)
    {
        cg.alpha = 1f;
        float t  = 0f;
        float omega = Mathf.Sqrt(stiffness / mass);
        float zeta  = damping;

        while (t < 2f)   // spring settles well within 2s
        {
            t += Time.deltaTime;
            float envelope = Mathf.Exp(-zeta * omega * t);
            float osc      = Mathf.Cos(omega * Mathf.Sqrt(1f - zeta * zeta) * t);
            float value    = 1f - envelope * osc;        // 0→1 with overshoot
            rt.localScale  = Vector3.LerpUnclamped(from, to, value);

            if (Mathf.Abs(value - 1f) < 0.001f && t > 0.3f) break;
            yield return null;
        }
        rt.localScale = to;
    }

    IEnumerator SlideIn(Vector2 offset)
    {
        Vector2 startPos = originalPos + offset;
        rt.anchoredPosition = startPos;
        cg.alpha = 0f;

        float t = 0f, dur = 0.4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ease = EaseOutExpo(t / dur);
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, originalPos, ease);
            cg.alpha = Mathf.Lerp(0f, 1f, t / dur * 2f);
            yield return null;
        }
        rt.anchoredPosition = originalPos;
        cg.alpha = 1f;
    }

    IEnumerator FadeIn()
    {
        rt.localScale = originalScale * 0.95f;
        cg.alpha = 0f;
        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ease = EaseOutCubic(t / dur);
            cg.alpha = ease;
            rt.localScale = Vector3.Lerp(originalScale * 0.95f, originalScale, ease);
            yield return null;
        }
        cg.alpha = 1f;
        rt.localScale = originalScale;
    }

    IEnumerator MorphIn()
    {
        yield return SpringScale(originalScale * 0.6f, originalScale);
    }

    // ─── EXIT ROUTINES ──────────────────────────────────────────────────────

    IEnumerator PlayExit()
    {
        float t = 0f;
        switch (exit)
        {
            case ExitType.ScaleShrink:
                while (t < exitDuration)
                {
                    t += Time.deltaTime;
                    float ease = EaseInCubic(t / exitDuration);
                    rt.localScale = Vector3.Lerp(originalScale, originalScale * 0.7f, ease);
                    cg.alpha = Mathf.Lerp(1f, 0f, ease);
                    yield return null;
                }
                break;
            case ExitType.FadeOut:
                while (t < exitDuration)
                {
                    t += Time.deltaTime;
                    cg.alpha = Mathf.Lerp(1f, 0f, t / exitDuration);
                    yield return null;
                }
                break;
            case ExitType.SlideDown:
                Vector2 target = originalPos - Vector2.up * 80f;
                while (t < exitDuration)
                {
                    t += Time.deltaTime;
                    float ease = EaseInCubic(t / exitDuration);
                    rt.anchoredPosition = Vector2.Lerp(originalPos, target, ease);
                    cg.alpha = Mathf.Lerp(1f, 0f, ease);
                    yield return null;
                }
                break;
        }
        gameObject.SetActive(false);
    }

    // ─── PULSE ──────────────────────────────────────────────────────────────

    IEnumerator PulseRoutine()
    {
        float t = 0f, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ping = Mathf.Sin(t / dur * Mathf.PI);
            rt.localScale = originalScale * (1f + ping * 0.08f);
            yield return null;
        }
        rt.localScale = originalScale;
    }

    // ─── HAPTICS ────────────────────────────────────────────────────────────

    public static void TriggerHaptic(HapticType type)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        long ms = type switch {
            HapticType.Light   => 20,
            HapticType.Medium  => 40,
            HapticType.Heavy   => 80,
            HapticType.Success => 30,
            HapticType.Warning => 50,
            _ => 20
        };
        Handheld.Vibrate();   // Unity's built-in; replace with Android native plugin for custom durations
#elif UNITY_IOS && !UNITY_EDITOR
        // iOS Taptic Engine via iOS Native plugin (e.g. "Unity-iPhone-Haptics")
        // iOSHapticFeedback.Instance?.Trigger(type == HapticType.Heavy ? iOSHapticFeedback.iOSFeedbackType.ImpactHeavy
        //     : iOSHapticFeedback.iOSFeedbackType.ImpactLight);
#endif
    }

    // ─── EASING ─────────────────────────────────────────────────────────────

    static float EaseOutExpo(float t)  => t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
    static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    static float EaseInCubic(float t)  => t * t * t;
}
