using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime builder: creates the entire mobile HUD Canvas in code.
/// Call MobileCanvasBuilder.Build() from any Start() or editor tool.
/// Useful for prototyping before switching to designed UI prefabs.
///
/// Layout:
///   ┌──────────────────────────────────────────────┐
///   │  [⚡ speed]      [🛢 fuel bar]    [⬆ alt]    │  ← top strip
///   │                                              │
///   │                                              │
///   │  [L joystick]              [R joystick]      │  ← bottom
///   │          [✈FLY] [📷CAM] [🚀BOOST]           │
///   └──────────────────────────────────────────────┘
/// </summary>
public class MobileCanvasBuilder : MonoBehaviour
{
    [Header("Auto Build on Start")]
    public bool buildOnStart = true;
    public MobileUIController targetController;

    void Start()
    {
        if (buildOnStart) Build(targetController);
    }

    public static void Build(MobileUIController ctrl)
    {
        // ── Root Canvas ──────────────────────────────────────────────────────
        var canvasGO = new GameObject("MobileCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var cg = canvasGO.AddComponent<CanvasGroup>();
        if (ctrl != null) ctrl.mobileGroup = cg;

        // ── Top Info Strip ───────────────────────────────────────────────────
        var strip = CreatePanel(canvasGO.transform, "TopStrip",
            new Vector2(0, 1), new Vector2(1, 1),    // anchors: top full-width
            new Vector2(0, -60), new Vector2(0, 0),
            new Vector2(0, 60));
        strip.GetComponent<Image>().color = new Color(0, 0, 0, 0.35f);

        var speedTxt  = CreateText(strip.transform, "SpeedLabel",  "0 km/h",  24, TextAnchor.MiddleLeft,  new Vector2(20, 0));
        var altTxt    = CreateText(strip.transform, "AltLabel",    "0 m",     24, TextAnchor.MiddleRight, new Vector2(-20, 0));
        var fuelSlider = CreateSlider(strip.transform, "FuelBar");

        if (ctrl != null)
        {
            ctrl.speedLabel = speedTxt.GetComponent<TMPro.TextMeshProUGUI>();
            ctrl.altLabel   = altTxt.GetComponent<TMPro.TextMeshProUGUI>();
            ctrl.fuelBar    = fuelSlider.GetComponent<Slider>();
        }

        // ── Left Joystick ────────────────────────────────────────────────────
        var leftJoyGO  = CreateJoystickUI(canvasGO.transform, "LeftJoystick",
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(180, 180), new Vector2(170, -170));

        // ── Right Joystick ───────────────────────────────────────────────────
        var rightJoyGO = CreateJoystickUI(canvasGO.transform, "RightJoystick",
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(180, 180), new Vector2(-170, -170));

        if (ctrl != null)
        {
            ctrl.leftJoystick  = leftJoyGO.GetComponent<VirtualJoystick>();
            ctrl.rightJoystick = rightJoyGO.GetComponent<VirtualJoystick>();
        }

        // ── Bottom Center Buttons ────────────────────────────────────────────
        float btnY   = 100f;
        float btnSize = 90f;

        var vtolBtn  = CreateButton(canvasGO.transform, "VTOLBtn",  "✈ FLY",   new Vector2(0.5f, 0), new Vector2(-110, btnY), btnSize);
        var camBtn   = CreateButton(canvasGO.transform, "CamBtn",   "🪟 CAM",   new Vector2(0.5f, 0), new Vector2(0, btnY),    btnSize);
        var boostBtn = CreateButton(canvasGO.transform, "BoostBtn", "🚀 BOOST", new Vector2(0.5f, 0), new Vector2(110, btnY),  btnSize);

        if (ctrl != null)
        {
            ctrl.vtolButton   = vtolBtn.GetComponent<TouchButton>();
            ctrl.cameraButton = camBtn.GetComponent<TouchButton>();
            ctrl.boostButton  = boostBtn.GetComponent<TouchButton>();

            ctrl.vtolLabel = vtolBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            ctrl.camLabel  = camBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            // Set VTOL as toggle, boost as hold
            ctrl.vtolButton.mode   = TouchButton.ButtonMode.Toggle;
            ctrl.cameraButton.mode = TouchButton.ButtonMode.Toggle;
            ctrl.boostButton.mode  = TouchButton.ButtonMode.Hold;
        }

        Debug.Log("[MobileCanvasBuilder] Mobile UI built successfully.");
    }

    // ─── HELPERS ────────────────────────────────────────────────────────────

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Vector2 sizeDelta)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = offsetMin;
        rt.offsetMax  = offsetMax;
        rt.sizeDelta  = sizeDelta;
        go.AddComponent<Image>().color = Color.clear;
        return go;
    }

    static GameObject CreateJoystickUI(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPos)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.sizeDelta = size; rt.anchoredPosition = anchoredPos;

        // Background ring
        var bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.sizeDelta = size;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0.2f);

        // Handle dot
        var handle = new GameObject("Handle");
        handle.transform.SetParent(bg.transform, false);
        var hRt = handle.AddComponent<RectTransform>();
        hRt.sizeDelta = size * 0.4f;
        var hImg = handle.AddComponent<Image>();
        hImg.color = new Color(1, 1, 1, 0.5f);

        // Add joystick component
        var joystick = root.AddComponent<VirtualJoystick>();
        joystick.background       = bgRt;
        joystick.handle           = hRt;
        joystick.backgroundImage  = bgImg;
        joystick.handleImage      = hImg;

        return root;
    }

    static GameObject CreateButton(Transform parent, string name, string labelText,
        Vector2 anchor, Vector2 anchoredPos, float size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f, 0.3f);

        var btn = go.AddComponent<TouchButton>();
        btn.buttonImage = img;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txtRt = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        var tmp = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = labelText;
        tmp.fontSize  = 14;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        btn.label     = tmp;

        return go;
    }

    static GameObject CreateText(Transform parent, string name, string text, int size,
        TextAnchor anchor, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text     = text;
        tmp.fontSize = size;
        tmp.color    = Color.white;
        return go;
    }

    static GameObject CreateSlider(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.3f, 0.1f);
        rt.anchorMax = new Vector2(0.7f, 0.9f);
        rt.sizeDelta = Vector2.zero;
        go.AddComponent<Slider>();
        return go;
    }
}
