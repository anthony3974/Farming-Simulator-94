using UnityEngine;

/// <summary>
/// Keeps one real street light on, the one nearest the driven vehicle, so a vehicle is lit as it passes under a lamp and
/// the game only ever pays for one lamp's light. Every other lamp shows its <see cref="LightPool"/>. On the "Street Lamps"
/// group in GameScene (added by Tools > Street Lamp Builder).
/// </summary>
public class StreetLampLights : MonoBehaviour
{
    [Tooltip("Seconds the real light and the pool take to swap.")]
    public float fadeSeconds = 0.6f;
    [Tooltip("Another lamp has to be this many metres closer before the real light moves to it, so it can't flicker between two lamps.")]
    public float switchMargin = 5f;

    private LightPool[] lamps;
    private LightPool nearest;

    void Awake() => lamps = GetComponentsInChildren<LightPool>();

    void Update()
    {
        if (!NightLight.LitNow)
        {
            foreach (var lamp in lamps) lamp.Apply(false, 0f);
            nearest = null;
            return;
        }

        if (Vehicle.Active != null) nearest = FindNearest(Vehicle.Active.transform.position);

        float step = Time.deltaTime / Mathf.Max(0.01f, fadeSeconds);
        foreach (var lamp in lamps)
            lamp.Apply(true, Mathf.MoveTowards(Mathf.Max(0f, lamp.Real), lamp == nearest ? 1f : 0f, step));
    }

    private LightPool FindNearest(Vector3 from)
    {
        LightPool best = nearest;
        float bestDistance = nearest != null ? Distance(nearest, from) - switchMargin : float.MaxValue;
        foreach (var lamp in lamps)
        {
            float distance = Distance(lamp, from);
            if (distance < bestDistance)
            {
                best = lamp;
                bestDistance = distance;
            }
        }
        return best;
    }

    // along the ground, to the middle of the pool (under the lamp's head, not at its pole)
    private static float Distance(LightPool lamp, Vector3 from)
    {
        Vector3 to = lamp.transform.position;
        return Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));
    }
}
