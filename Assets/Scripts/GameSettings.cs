using System;
using UnityEngine;

/// <summary>
/// The GAME tab of the SETTINGS panel, remembered on the device (PlayerPrefs, like <see cref="TouchSettings"/>):
/// mirrors on / off, minimap on / off, the day / night cycle on / off, and how long a day and a night last.
/// <see cref="DayNightCycle"/> reads the cycle and the lengths; the mirrors and the minimap will read theirs when
/// they are built (PLAN: minimap, vehicle mirrors). <see cref="SettingsMenu"/> changes them.
/// </summary>
public static class GameSettings
{
    private const string MirrorsKey = "game.mirrors";
    private const string MinimapKey = "game.minimap";
    private const string CycleKey = "game.dayNight";
    private const string DayKey = "game.dayMinutes";
    private const string NightKey = "game.nightMinutes";

    /// <summary>Default lengths (owner, 2026-11-07): 10 minutes of day, 5 minutes of night.</summary>
    public const int DefaultDayMinutes = 10;
    public const int DefaultNightMinutes = 5;

    /// <summary>The lengths the - / + buttons step through, in minutes.</summary>
    public static readonly int[] LengthSteps = { 1, 2, 3, 4, 5, 6, 8, 10, 12, 15, 20, 25, 30, 45, 60 };

    /// <summary>Raised after any setting changes.</summary>
    public static event Action Changed;

    public static bool Mirrors
    {
        get => PlayerPrefs.GetInt(MirrorsKey, 1) == 1;
        set => SetBool(MirrorsKey, value);
    }

    public static bool Minimap
    {
        get => PlayerPrefs.GetInt(MinimapKey, 1) == 1;
        set => SetBool(MinimapKey, value);
    }

    /// <summary>Day and night take turns. Off = always day.</summary>
    public static bool DayNightCycle
    {
        get => PlayerPrefs.GetInt(CycleKey, 1) == 1;
        set => SetBool(CycleKey, value);
    }

    /// <summary>Real minutes from sunrise to sunset.</summary>
    public static int DayMinutes
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(DayKey, DefaultDayMinutes), LengthSteps[0], LengthSteps[LengthSteps.Length - 1]);
        set => SetInt(DayKey, value);
    }

    /// <summary>Real minutes from sunset to sunrise.</summary>
    public static int NightMinutes
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(NightKey, DefaultNightMinutes), LengthSteps[0], LengthSteps[LengthSteps.Length - 1]);
        set => SetInt(NightKey, value);
    }

    /// <summary>The next length up (direction 1) or down (-1) from <paramref name="minutes"/> in <see cref="LengthSteps"/>.</summary>
    public static int Step(int minutes, int direction)
    {
        if (direction > 0)
        {
            foreach (int m in LengthSteps) if (m > minutes) return m;
            return LengthSteps[LengthSteps.Length - 1];
        }
        for (int i = LengthSteps.Length - 1; i >= 0; i--) if (LengthSteps[i] < minutes) return LengthSteps[i];
        return LengthSteps[0];
    }

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    private static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, Mathf.Clamp(value, LengthSteps[0], LengthSteps[LengthSteps.Length - 1]));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
