using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off tool: Tools > Vehicle Ground Fix. The tractor floated when driven (owner, 2026-11-08): a CharacterController
/// rests Skin Width (0.08 m) above the ground, and the tractor's controller bottom was also 5 cm below its origin, so it
/// stood 11 cm higher in Play than where it was placed and the tread no longer touched the ground. This moves the
/// controller's Center Y on the Tractor, Van and Harvester prefabs so that, resting on flat ground, the bottom of the
/// tires' rubber sinks <see cref="Sink"/> into the ground and the tread lugs are in the ground, like a parked vehicle
/// (owner, 2026-11-08: "I want the tractor lugs in the ground like it is for the van and harvester"). Only Center Y
/// changes. Safe to run again.
/// It also removes the van's two mud flaps (owner, 2026-11-08), first, so nothing else is measured.
/// Delete after running (it stays in git history).
/// </summary>
public static class VehicleGroundFix
{
    private const float Sink = 0.01f; // metres the tire rubber goes into the ground (the lugs go deeper), so it looks planted
    private static readonly string[] Prefabs =
        { "Assets/Prefabs/Tractor.prefab", "Assets/Prefabs/Van.prefab", "Assets/Prefabs/Harvester.prefab" };

    [MenuItem("Tools/Vehicle Ground Fix")]
    public static void Run()
    {
        var log = new System.Text.StringBuilder("Vehicle Ground Fix:\n");
        foreach (var path in Prefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!root.TryGetComponent(out CharacterController cc)) { log.AppendLine($"  {path}: no CharacterController, skipped"); continue; }

                // the van's mud flaps go (and out of the paint list, so it keeps no empty slots)
                var vehicle = root.GetComponent<Vehicle>();
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null || !t.name.StartsWith("Mud Flap")) continue;
                    if (vehicle != null) vehicle.paintRenderers.RemoveAll(r => r == null || r.transform.IsChildOf(t));
                    log.AppendLine($"  {System.IO.Path.GetFileNameWithoutExtension(path)}: removed {t.name}");
                    Object.DestroyImmediate(t.gameObject);
                }

                // bottom of the tires' rubber (not the lugs), in the root's own space
                float lowest = float.MaxValue;
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    if (!r.gameObject.activeInHierarchy || !r.name.EndsWith("Tire")) continue;
                    if (!r.TryGetComponent(out MeshFilter mf) || mf.sharedMesh == null) continue;
                    foreach (var v in mf.sharedMesh.vertices)
                        lowest = Mathf.Min(lowest, root.transform.InverseTransformPoint(r.transform.TransformPoint(v)).y);
                }
                if (lowest == float.MaxValue) { log.AppendLine($"  {path}: no tires found, skipped"); continue; }

                // resting on flat ground the origin is at (skin - controller bottom); we want lowest + origin = -Sink
                float half = Mathf.Max(cc.height / 2f, cc.radius);
                float bottomBefore = cc.center.y - half;
                float bottom = cc.skinWidth + lowest + Sink;
                var c = cc.center;
                float oldY = c.y;
                c.y = bottom + half;
                cc.center = c;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.AppendLine($"  {System.IO.Path.GetFileNameWithoutExtension(path)}: tire bottom {lowest:0.000} m, Center Y {oldY:0.000} -> {c.y:0.000}; " +
                               $"rests at y {cc.skinWidth - bottomBefore:0.000} before, {cc.skinWidth - bottom:0.000} now");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log(log + "Press Play and drive: the tires should sit on the ground with the lugs in it. Then delete Assets/Editor/VehicleGroundFix.cs " +
                  "and VehicleGroundCheck.cs.");
    }
}
