using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Full game HUD: coin counter with pop animation, score, high score,
/// combo multiplier, flight mode badge, and warning alerts.
/// Subscribe to CoinManager events for real-time coin updates.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Coin Counter")]
    public TextMeshProUGUI coinText;            // "x 42"
    public TextMeshProUGUI highScoreText;       // "BEST: 100"
    public Image coinIcon;                      // spinning coin icon (optional)
    public RectTransform coinCounterRoot;       // root to punch-scale on collect

    [Header("Combo Multiplier")]
    public TextMeshProUGUI comboText;           // "x3 COMBO!"
    public RectTransform comboRoot;
    public float comboWindow = 3f;             // seconds to chain collections
    public int   comboMax    = 10;

    [Header("Flight Mode Badge")]
    public GameObject flightBadge;             // "✈ FLIGHT" badge panel
    public GameObject driveBadge;              // "🚗 DRIVE" badge panel
    public TextMeshProUGUI modeText;

    [Header("Warnings")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;
    public float warningFlashSpeed = 2f;

    [Header("Fuel Bar")]
    public Slider fuelSlider;
    public Image  fuelFill;
    public TextMeshProUGUI fuelText;

    [Header("References")]
    public FlyingCarController car;
    public CoinManager coinManager;

    // ── Private state ────────────────────────────────────────────────────────
    private int    combo         = 1;
    private float  comboTimer    = 0f;
    private bool   comboActive   = false;
    private float  coinIconAngle = 0f;

    void OnEnable()
    {
        if (coinManager == null) coinManager = CoinManager.Instance;
        if (coinManager != null) coinManager.OnCoinsChanged += HandleCoinsChanged;
    }

    void OnDisable()
    {
        if (coinManager != null) coinManager.OnCoinsChanged -= HandleCoinsChanged;
    }

    void Start()
    {
        UpdateCoinDisplay(coinManager != null ? coinManager.totalCoins : 0);
        UpdateHighScore();
    }

    void Update()
    {
        if (car == null) return;

        UpdateFlightBadge();
        UpdateFuelBar();
        UpdateWarnings();
        UpdateComboTimer();
        SpinCoinIcon();
    }

    // ─── COINS ──────────────────────────────────────────────────────────────

    void HandleCoinsChanged(int total)
    {
        UpdateCoinDisplay(total);
        UpdateHighScore();
        TriggerCombo();
        StartCoroutine(PunchScale(coinCounterRoot, 1.3f, 0.15f));
    }

    void UpdateCoinDisplay(int total)
    {
        if (coinText != null) coinText.text = $"× {total}";
    }

    void UpdateHighScore()
    {
        if (highScoreText != null && coinManager != null)
            highScoreText.text = $"BEST: {coinManager.highScore}";
    }

    void SpinCoinIcon()
    {
        if (coinIcon == null) return;
        coinIconAngle -= 60f * Time.deltaTime;
        coinIcon.rectTransform.localEulerAngles = new Vector3(0f, coinIconAngle, 0f);
    }

    // ─── COMBO ──────────────────────────────────────────────────────────────

    void TriggerCombo()
    {
        if (comboActive)
            combo = Mathf.Min(combo + 1, comboMax);
        else
            combo = 1;

        comboTimer  = comboWindow;
        comboActive = true;

        if (comboRoot != null) comboRoot.gameObject.SetActive(combo > 1);
        if (comboText  != null) comboText.text = combo > 1 ? $"× {combo} COMBO!" : "";
        if (combo > 1) StartCoroutine(PunchScale(comboRoot, 1.4f, 0.2f));
    }

    void UpdateComboTimer()
    {
        if (!comboActive) return;
        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f)
        {
            comboActive = false;
            combo       = 1;
            if (comboRoot != null) comboRoot.gameObject.SetActive(false);
        }
    }

    // ─── FLIGHT BADGE ───────────────────────────────────────────────────────

    void UpdateFlightBadge()
    {
        bool flying = car.IsFlying;
        if (flightBadge != null) flightBadge.SetActive(flying);
        if (driveBadge  != null) driveBadge.SetActive(!flying);
        if (modeText    != null) modeText.text = flying ? "✈  FLIGHT MODE" : "🚗  DRIVE MODE";
    }

    // ─── FUEL ───────────────────────────────────────────────────────────────

    void UpdateFuelBar()
    {
        if (fuelSlider == null) return;
        float pct = car.Fuel / 100f;
        fuelSlider.value = pct;
        if (fuelText != null) fuelText.text = $"{car.Fuel:F0}%";
        if (fuelFill != null)
        {
            if (pct > 0.4f)      fuelFill.color = Color.green;
            else if (pct > 0.15f) fuelFill.color = Color.yellow;
            else                   fuelFill.color = Color.red;
        }
    }

    // ─── WARNINGS ───────────────────────────────────────────────────────────

    void UpdateWarnings()
    {
        string msg = "";
        if (car.Fuel <= 0f)                             msg = "⚠  FUEL EMPTY";
        else if (car.Fuel < 15f)                        msg = "⚠  LOW FUEL";
        else if (car.IsFlying && car.Speed < 15f && !car.IsGrounded) msg = "⚠  STALL";

        bool show = !string.IsNullOrEmpty(msg);
        if (warningPanel != null) warningPanel.SetActive(show);
        if (warningText  != null)
        {
            warningText.text = msg;
            float alpha = (Mathf.Sin(Time.time * warningFlashSpeed * Mathf.PI) + 1f) * 0.5f;
            warningText.color = new Color(1f, 0.3f, 0.1f, alpha);
        }
    }

    // ─── PUNCH SCALE ────────────────────────────────────────────────────────

    IEnumerator PunchScale(RectTransform target, float scale, float duration)
    {
        if (target == null) yield break;
        Vector3 original = target.localScale;
        float   half     = duration * 0.5f;
        float   t        = 0f;

        while (t < half)
        {
            target.localScale = Vector3.Lerp(original, original * scale, t / half);
            t += Time.deltaTime;
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            target.localScale = Vector3.Lerp(original * scale, original, t / half);
            t += Time.deltaTime;
            yield return null;
        }
        target.localScale = original;
    }
}
