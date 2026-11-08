using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off check (changes nothing): Tools > Debug: Vehicle Ground Check, in Play mode. For the vehicle you drive it
/// logs what its CharacterController is resting on and how far each tire is above the ground under it, to find why
/// the tractor floats when driven (PLAN.md, "Tractor floats when driving"). Delete after use (git history).
/// </summary>
public static class VehicleGroundCheck
{
    [MenuItem("Tools/Debug: Vehicle Ground Check")]
    public static void Run()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Vehicle Ground Check: press Play and drive a bit first."); return; }
        var v = Vehicle.Active;
        if (v == null) { Debug.LogWarning("Vehicle Ground Check: no vehicle is being driven."); return; }

        var cc = v.Controller;
        var t = v.transform;
        float height = Mathf.Max(cc.height, 2f * cc.radius) * t.lossyScale.y;
        float radius = cc.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);
        Vector3 centre = t.TransformPoint(cc.center);
        Vector3 bottomSphere = centre + Vector3.down * (height / 2f - radius);
        var log = new System.Text.StringBuilder();
        log.AppendLine($"Vehicle Ground Check: {v.name} at y {t.position.y:0.000}, grounded {cc.isGrounded}, " +
                       $"controller bottom y {bottomSphere.y - radius:0.000} (radius {radius:0.00}, skin {cc.skinWidth:0.00})");

        // what the controller's bottom ball rests on
        var hits = Physics.SphereCastAll(bottomSphere + Vector3.up * 0.5f, radius * 0.98f, Vector3.down, 1.5f,
                                         ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider == cc || h.collider.transform.IsChildOf(t)) continue;
            log.AppendLine($"  controller touches '{Path(h.collider.transform)}' ({h.collider.GetType().Name}) at " +
                           $"{h.point.x:0.00}, {h.point.y:0.000}, {h.point.z:0.00}, {Vector3.Distance(new Vector3(h.point.x, 0, h.point.z), new Vector3(t.position.x, 0, t.position.z)):0.00} m from the origin");
        }

        // gap under each tire
        foreach (var r in v.GetComponentsInChildren<Renderer>())
        {
            if (!r.name.EndsWith("Tire") || !r.gameObject.activeInHierarchy) continue;
            var b = r.bounds;
            if (Physics.Raycast(new Vector3(b.center.x, b.max.y + 1f, b.center.z), Vector3.down, out var hit, 20f, ~0,
                                QueryTriggerInteraction.Ignore))
            {
                // skip our own parts
                var all = Physics.RaycastAll(new Vector3(b.center.x, b.max.y + 1f, b.center.z), Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(all, (a, c) => a.distance.CompareTo(c.distance));
                foreach (var a in all) { if (!a.collider.transform.IsChildOf(t)) { hit = a; break; } }
                log.AppendLine($"  {r.name}: bottom y {b.min.y:0.000}, ground '{Path(hit.collider.transform)}' y {hit.point.y:0.000}, gap {b.min.y - hit.point.y:0.000} m");
            }
        }
        Debug.Log(log.ToString());
    }

    private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
}
