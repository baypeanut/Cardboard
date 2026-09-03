using Google.XR.Cardboard;
using UnityEngine;

public class CardboardStartup : MonoBehaviour
{
    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (!Application.isMobilePlatform)
        {
            return;
        }

        if (!Api.HasDeviceParams())
        {
            Api.ScanDeviceParams();
        }
    }

    private void Update()
    {
        if (!Application.isMobilePlatform)
        {
            return;
        }

        if (Api.IsGearButtonPressed)
        {
            Api.ScanDeviceParams();
        }

        if (Api.IsCloseButtonPressed)
        {
            Application.Quit();
        }

        if (Api.IsTriggerHeldPressed)
        {
            Api.Recenter();
        }

        if (Api.HasNewDeviceParams())
        {
            Api.ReloadDeviceParams();
        }

        Api.UpdateScreenParams();
    }
}
