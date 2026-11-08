using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Kept (not a one-off tool): before every Android build, sets the Android version code from the version name in
/// Project Settings > Player > Version, so it is never forgotten (owner, 2026-11-08).
/// Code = major x 10000 + minor x 100 + patch, so 0.6.8 -> 608, 0.7.0 -> 700, 1.2.3 -> 10203. Minor and patch must
/// stay below 100. Google Play only takes a code higher than the last one uploaded; the old hand-set codes were the
/// version without dots (0.6.8 -> 68), and every code from this rule is higher than those.
/// </summary>
public class AndroidVersionCode : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;

        string name = PlayerSettings.bundleVersion;
        var parts = name.Split('.');
        int[] n = new int[3];
        for (int i = 0; i < 3; i++)
        {
            if (i < parts.Length && !int.TryParse(parts[i], out n[i]) || n[i] < 0 || (i > 0 && n[i] > 99))
                throw new BuildFailedException($"Android version code: the version \"{name}\" must look like 0.6.8 " +
                                               "(numbers, minor and patch below 100). Fix it in Project Settings > Player > Version.");
        }
        int code = n[0] * 10000 + n[1] * 100 + n[2];
        if (code < 1) throw new BuildFailedException($"Android version code: \"{name}\" gives {code}; it must be at least 1.");

        if (PlayerSettings.Android.bundleVersionCode != code)
        {
            Debug.Log($"Android version code: {PlayerSettings.Android.bundleVersionCode} -> {code} (from version {name}).");
            PlayerSettings.Android.bundleVersionCode = code;
            AssetDatabase.SaveAssets();
        }
    }
}
