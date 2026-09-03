using Google.XR.Cardboard;
using UnityEngine.InputSystem;

public static class InputSystemBridge
{
    public static bool FirePressed()
    {
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool touchPressed = Touchscreen.current != null &&
                            Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        return mousePressed || touchPressed;
    }

    public static bool RestartPressed()
    {
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool touchPressed = Touchscreen.current != null &&
                            Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool cardboardPressed = UnityEngine.Application.isMobilePlatform &&
                                Api.IsTriggerPressed;
        return keyboardPressed || touchPressed || cardboardPressed;
    }
}
