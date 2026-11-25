using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Premium animated score popup — floating "+1 coin" text that springs
/// upward and fades out, like iOS game reward feedback.
/// Pool-based for performance on mobile.
/// </summary>
public class FloatingScorePopup : MonoBehaviour
{
    public static FloatingScorePopup Instance { get; private set; }

    [Header("Prefab Setup (auto-creates if null)")]
    public GameObject popupPrefab;      // TextMeshProUGUI on a RectTransform

    [Header("Animation")]
    public float riseDistance   = 80f;
    public float lifetime       = 1.1f;
    public float scaleIn        = 1.3f;   // initial overshoot scale

    [Header("Style")]
    public Color  coinColor     = new Color(1f, 0.85f, 0f);
    public Color  comboColor    = new Color(0.3f, 1f, 0.5f);
    public Color  dangerColor   = new Color(1f, 0.3f, 0.1f);
    public int    fontSize      = 32;

    private Canvas canvas;
    private const int poolSize = 10;
    private GameObject[] pool;
    private int poolIndex = 0;

    void Awake()
    {
        Instance = this;
        canvas = FindObjectOfType<Canvas>();
        InitPool();
    }

    void Start()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinsChanged += OnCoin;
    }

    void OnDestroy()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinsChanged -= OnCoin;
    }

    // ─── POOL ───────────────────────────────────────────────────────────────

    void InitPool()
    {
        pool = new GameObject[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = CreatePopupGO();
            pool[i].SetActive(false);
        }
    }

    GameObject CreatePopupGO()
    {
        var go = new GameObject("FloatPopup");
        go.transform.SetParent(canvas != null ? canvas.transform : transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 60f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        var cg = go.AddComponent<CanvasGroup>();
        return go;
    }

    GameObject GetFromPool()
    {
        var obj = pool[poolIndex % poolSize];
        poolIndex++;
        obj.SetActive(false);
        obj.SetActive(true);
        return obj;
    }

    // ─── PUBLIC SPAWN ────────────────────────────────────────────────────────

    public void Spawn(string text, Vector2 screenPos, Color color)
    {
        var go = GetFromPool();
        var rt = go.GetComponent<RectTransform>();
        var tmp = go.GetComponent<TextMeshProUGUI>();
        var cg  = go.GetComponent<CanvasGroup>();

        // Convert screen position to canvas local position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), screenPos,
            canvas.worldCamera, out Vector2 localPos);
        rt.anchoredPosition = localPos;

        tmp.text  = text;
        tmp.color = color;
        cg.alpha  = 1f;

        StartCoroutine(AnimatePopup(rt, cg, go));
        LiquidUIAnimator.TriggerHaptic(LiquidUIAnimator.HapticType.Light);
    }

    public void SpawnAtCenter(string text, Color color)
    {
        Spawn(text, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), color);
    }

    // ─── ANIMATION ──────────────────────────────────────────────────────────

    IEnumerator AnimatePopup(RectTransform rt, CanvasGroup cg, GameObject go)
    {
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos   = startPos + Vector2.up * riseDistance;

        float t = 0f;

        // Scale-in bounce
        rt.localScale = Vector3.one * scaleIn;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(Vector3.one * scaleIn, Vector3.one, t / 0.15f);
            yield return null;
        }
        rt.localScale = Vector3.one;

        // Rise + fade
        t = 0f;
        while (t < lifetime)
        {
            t += Time.deltaTime;
            float ease = 1f - Mathf.Pow(1f - t / lifetime, 2f);   // ease out quad
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            cg.alpha = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(lifetime * 0.5f, lifetime, t));
            yield return null;
        }

        go.SetActive(false);
    }

    // ─── EVENTS ─────────────────────────────────────────────────────────────

    void OnCoin(int total)
    {
        SpawnAtCenter("+1 🪙", coinColor);
    }

    public void ShowCombo(int combo)
    {
        SpawnAtCenter($"×{combo} COMBO! 🔥", comboColor);
    }
}
