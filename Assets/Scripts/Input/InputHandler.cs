using UnityEngine;
using UnityEngine.InputSystem;  // Requires Input System package

/// <summary>
/// Unified input handler for PC (Keyboard/Gamepad) and Android (Touch).
/// Feeds normalized values into FlyingCarController.
/// </summary>
public class InputHandler : MonoBehaviour
{
    [Header("References")]
    public FlyingCarController car;

    [Header("Mobile Touch Settings")]
    public float touchSensitivity = 1.5f;
    public RectTransform leftJoystickZone;   // Assign UI joystick zones
    public RectTransform rightJoystickZone;

    private bool vtolToggleThisFrame;
    private Vector2 leftStick, rightStick;

    void Update()
    {
        vtolToggleThisFrame = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        HandleTouchInput();
#else
        HandleKeyboardInput();
        HandleGamepadInput();
#endif

        ApplyToController();
    }

    // ─── KEYBOARD ───────────────────────────────────────────────────────────

    void HandleKeyboardInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Throttle: W / S   or  Up / Down
        float throttle = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttle = 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttle = -0.5f;

        // Pitch: I/K    Roll: Q/E    Yaw: A/D
        float pitch = 0f, roll = 0f, yaw = 0f;
        if (kb.iKey.isPressed) pitch = 1f;
        if (kb.kKey.isPressed) pitch = -1f;
        if (kb.qKey.isPressed) roll = -1f;
        if (kb.eKey.isPressed) roll = 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  yaw = -1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) yaw = 1f;

        leftStick  = new Vector2(yaw, throttle);
        rightStick = new Vector2(roll, pitch);

        // VTOL toggle: Space
        if (kb.spaceKey.wasPressedThisFrame)
            vtolToggleThisFrame = true;
    }

    // ─── GAMEPAD ────────────────────────────────────────────────────────────

    void HandleGamepadInput()
    {
        var gp = Gamepad.current;
        if (gp == null) return;

        // Left stick: yaw + throttle,  Right stick: roll + pitch
        leftStick  = gp.leftStick.ReadValue();
        rightStick = gp.rightStick.ReadValue();

        // South button (A/Cross): toggle VTOL
        if (gp.buttonSouth.wasPressedThisFrame)
            vtolToggleThisFrame = true;
    }

    // ─── TOUCH (Android) ────────────────────────────────────────────────────

    void HandleTouchInput()
    {
        // Simple dual-joystick touch: left side = throttle/yaw, right side = pitch/roll
        leftStick  = Vector2.zero;
        rightStick = Vector2.zero;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.None) continue;

            Vector2 pos = touch.position.ReadValue();
            Vector2 delta = touch.delta.ReadValue() * touchSensitivity * Time.deltaTime;

            bool isLeft = pos.x < Screen.width * 0.5f;
            if (isLeft)
                leftStick += delta;
            else
                rightStick += delta;
        }

        leftStick  = Vector2.ClampMagnitude(leftStick, 1f);
        rightStick = Vector2.ClampMagnitude(rightStick, 1f);

        // Double-tap anywhere to toggle VTOL
        if (Touchscreen.current.primaryTouch.tapCount.ReadValue() >= 2)
            vtolToggleThisFrame = true;
    }

    // ─── APPLY ──────────────────────────────────────────────────────────────

    void ApplyToController()
    {
        car.thrustInput = Mathf.Clamp01(leftStick.y);
        car.yawInput    = leftStick.x;
        car.rollInput   = rightStick.x;
        car.pitchInput  = rightStick.y;
        car.steerInput  = leftStick.x;  // reused for ground steering
        car.vtolToggle  = vtolToggleThisFrame;
    }
}
