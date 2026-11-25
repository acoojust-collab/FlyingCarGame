using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central audio manager: engine RPM loop, afterburner, wind, coin SFX, music.
/// Uses AudioMixerGroups for master/SFX/music volume control.
/// Drop Kenney Interface Sounds + Freesound engine clips into the slots.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("References")]
    public FlyingCarController car;
    public AudioMixer masterMixer;          // Create in Unity: Audio → AudioMixer

    // ── Engine ───────────────────────────────────────────────────────────────
    [Header("Engine Sounds (Freesound CC0)")]
    public AudioClip engineIdleClip;        // freesound: "car engine idle"
    public AudioClip engineRevClip;         // freesound: "car engine rev loop"
    public AudioClip afterburnerClip;       // freesound: "jet afterburner"
    public AudioClip vtolToggleClip;        // freesound: "mechanical click"
    [Range(0f,1f)] public float engineVolume = 0.7f;

    // ── Wind ─────────────────────────────────────────────────────────────────
    [Header("Wind / Atmosphere (Freesound CC0)")]
    public AudioClip windClip;             // freesound: "wind loop"
    [Range(0f,1f)] public float windMaxVolume = 0.5f;
    public float windSpeedThreshold = 30f; // km/h before wind starts

    // ── UI & Coins ───────────────────────────────────────────────────────────
    [Header("UI & Coin Sounds (Kenney Interface Sounds)")]
    public AudioClip coinCollectClip;      // kenney: "impactBell_heavy"
    public AudioClip comboClip;            // kenney: "confirmation_001"
    public AudioClip warningBeepClip;      // kenney: "error_001"
    public AudioClip modeToggleClip;       // kenney: "switch_001"

    // ── Music ────────────────────────────────────────────────────────────────
    [Header("Background Music (OpenGameArt Synthwave)")]
    public AudioClip[] musicTracks;        // drag in your synthwave tracks
    public float musicFadeSpeed = 1.5f;
    [Range(0f,1f)] public float musicVolume = 0.4f;

    // ── Private audio sources ────────────────────────────────────────────────
    private AudioSource engineSource;
    private AudioSource afterburnerSource;
    private AudioSource windSource;
    private AudioSource musicSource;
    private AudioSource sfxSource;         // one-shot SFX

    private int    currentTrack = 0;
    private bool   wasFlying    = false;
    private bool   wasWarning   = false;
    private float  warnTimer    = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateAudioSources();
    }

    void Start()
    {
        if (car == null) car = FindObjectOfType<FlyingCarController>();
        PlayNextTrack();

        // Subscribe to coin events
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinsChanged += OnCoinCollected;
    }

    void OnDestroy()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinsChanged -= OnCoinCollected;
    }

    // ─── CREATE SOURCES ─────────────────────────────────────────────────────

    void CreateAudioSources()
    {
        engineSource      = CreateSource("Engine",      true,  engineVolume,  256);
        afterburnerSource = CreateSource("Afterburner", true,  engineVolume,  128);
        windSource        = CreateSource("Wind",        true,  0f,            128);
        musicSource       = CreateSource("Music",       true,  musicVolume,   0);
        sfxSource         = CreateSource("SFX",         false, 1f,            0);
    }

    AudioSource CreateSource(string name, bool loop, float volume, int priority)
    {
        var go = new GameObject($"AudioSource_{name}");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop           = loop;
        src.volume         = volume;
        src.priority       = priority;
        src.spatialBlend   = 0f;    // 2D audio
        src.playOnAwake    = false;
        return src;
    }

    // ─── UPDATE ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (car == null) return;

        float throttle = car.ThrustPercent;
        float speedKmh = car.Speed * 3.6f;
        bool  flying   = car.IsFlying;

        UpdateEngine(throttle, flying);
        UpdateWind(speedKmh, flying);
        UpdateMusic();
        HandleFlightToggle(flying);
        HandleLowFuelWarning();

        wasFlying = flying;
    }

    // ─── ENGINE ─────────────────────────────────────────────────────────────

    void UpdateEngine(float throttle, bool flying)
    {
        // Blend idle → rev based on throttle
        if (engineIdleClip != null && !engineSource.isPlaying)
        {
            engineSource.clip = engineIdleClip;
            engineSource.Play();
        }

        // Pitch: 0.6 at idle → 1.6 at full throttle
        engineSource.pitch  = Mathf.Lerp(0.6f, 1.6f, throttle);
        engineSource.volume = Mathf.Lerp(engineVolume * 0.4f, engineVolume, throttle);

        // Switch clip to rev when throttle is high
        if (engineRevClip != null && throttle > 0.3f && engineSource.clip != engineRevClip)
        {
            engineSource.clip = engineRevClip;
            engineSource.Play();
        }
        else if (engineIdleClip != null && throttle <= 0.3f && engineSource.clip != engineIdleClip)
        {
            engineSource.clip = engineIdleClip;
            engineSource.Play();
        }

        // Afterburner
        bool afterburning = flying && throttle > 0.85f;
        if (afterburnerClip != null)
        {
            if (afterburning && !afterburnerSource.isPlaying)
                afterburnerSource.PlayOneShot(afterburnerClip, 0.8f);
            else if (!afterburning)
                afterburnerSource.Stop();
        }
    }

    // ─── WIND ───────────────────────────────────────────────────────────────

    void UpdateWind(float speedKmh, bool flying)
    {
        if (windClip == null) return;
        if (!windSource.isPlaying)
        {
            windSource.clip = windClip;
            windSource.Play();
        }
        float t = Mathf.InverseLerp(windSpeedThreshold, 300f, speedKmh);
        windSource.volume = Mathf.Lerp(0f, windMaxVolume, t * (flying ? 1f : 0.3f));
        windSource.pitch  = Mathf.Lerp(0.8f, 1.3f, t);
    }

    // ─── MUSIC ──────────────────────────────────────────────────────────────

    void UpdateMusic()
    {
        if (musicTracks == null || musicTracks.Length == 0) return;
        if (!musicSource.isPlaying) PlayNextTrack();
    }

    void PlayNextTrack()
    {
        if (musicTracks == null || musicTracks.Length == 0) return;
        musicSource.clip   = musicTracks[currentTrack % musicTracks.Length];
        musicSource.volume = musicVolume;
        musicSource.Play();
        currentTrack++;
    }

    // ─── EVENTS ─────────────────────────────────────────────────────────────

    void HandleFlightToggle(bool flying)
    {
        if (flying != wasFlying && modeToggleClip != null)
            sfxSource.PlayOneShot(modeToggleClip, 0.8f);
    }

    void OnCoinCollected(int total)
    {
        if (coinCollectClip != null) sfxSource.PlayOneShot(coinCollectClip, 0.9f);
    }

    public void PlayCombo()
    {
        if (comboClip != null) sfxSource.PlayOneShot(comboClip, 1f);
    }

    void HandleLowFuelWarning()
    {
        if (car.Fuel < 15f && car.Fuel > 0f)
        {
            warnTimer += Time.deltaTime;
            if (warnTimer >= 2f)
            {
                warnTimer = 0f;
                if (warningBeepClip != null) sfxSource.PlayOneShot(warningBeepClip, 0.7f);
            }
        }
        else warnTimer = 0f;
    }

    // ─── PUBLIC CONTROLS ────────────────────────────────────────────────────

    public void SetMasterVolume(float v) => masterMixer?.SetFloat("MasterVol", Mathf.Log10(v) * 20f);
    public void SetMusicVolume(float v)  => musicSource.volume = v;
    public void SetSFXVolume(float v)    => sfxSource.volume   = v;
    public void PlaySFX(AudioClip clip, float vol = 1f) => sfxSource.PlayOneShot(clip, vol);
}
