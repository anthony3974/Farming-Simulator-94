using System;
using UnityEngine;

/// <summary>How the touch controls steer: the &lt; &gt; buttons, the steering wheel or the slider.</summary>
public enum SteeringMode { Buttons, Wheel, Slider }

/// <summary>How the touch controls drive: the GAS and BRAKE buttons or the pedal slider (up gas, down brake).</summary>
public enum PedalMode { Buttons, Slider }

/// <summary>
/// The touch control choices from the SETTINGS panel, remembered on the phone (PlayerPrefs).
/// <see cref="TouchControls"/> shows the chosen steering, pedals and button shape; <see cref="SettingsMenu"/> changes them.
/// </summary>
public static class TouchSettings
{
    private const string SteeringKey = "touch.steering";
    private const string RoundedKey = "touch.rounded";
    private const string PedalsKey = "touch.pedals";

    /// <summary>Raised after any setting changes.</summary>
    public static event Action Changed;

    public static SteeringMode Steering
    {
        get => (SteeringMode)Mathf.Clamp(PlayerPrefs.GetInt(SteeringKey, (int)SteeringMode.Buttons), 0, 2);
        set { PlayerPrefs.SetInt(SteeringKey, (int)value); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    public static PedalMode Pedals
    {
        get => (PedalMode)Mathf.Clamp(PlayerPrefs.GetInt(PedalsKey, (int)PedalMode.Buttons), 0, 1);
        set { PlayerPrefs.SetInt(PedalsKey, (int)value); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    /// <summary>Rounded touch buttons instead of square ones.</summary>
    public static bool Rounded
    {
        get => PlayerPrefs.GetInt(RoundedKey, 0) == 1;
        set { PlayerPrefs.SetInt(RoundedKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
    }
}
