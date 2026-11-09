using UnityEngine;

/// <summary>
/// The fake pool of light under one street lamp, and that lamp's real light. On the "Light Pool" child of the Street Lamp
/// prefab (made by Tools > Street Lamp Builder). <see cref="StreetLampLights"/> says which lamp is real: every other lamp
/// shows only the pool, and the real light and the pool cross-fade so the ground doesn't change brightness in a jump.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class LightPool : MonoBehaviour
{
    private static readonly int Fade = Shader.PropertyToID("_Fade");

    [Tooltip("This lamp's real light. On only while this is the lamp nearest the player.")]
    public Light realLight;

    /// <summary>How far the real light has taken over from the pool: 0 = only the pool, 1 = only the real light.</summary>
    public float Real { get; private set; } = -1f; // so the first Apply always takes effect

    private Renderer pool;
    private MaterialPropertyBlock block;
    private float fullIntensity;
    private bool night = true;

    void Awake()
    {
        pool = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        if (realLight != null) fullIntensity = realLight.intensity;
        Apply(false, 0f);
    }

    /// <summary>Shows the lamp: dark by day; by night, <paramref name="real"/> of the real light and the rest as the pool.</summary>
    public void Apply(bool isNight, float real)
    {
        if (isNight == night && Mathf.Approximately(real, Real)) return;
        night = isNight;
        Real = real;

        if (realLight != null)
        {
            realLight.enabled = isNight && real > 0f;
            realLight.intensity = fullIntensity * real;
        }

        pool.enabled = isNight && real < 1f;
        if (!pool.enabled) return;
        if (real <= 0f)
        {
            pool.SetPropertyBlock(null); // no block, so the plain pools can still be batched
            return;
        }
        block.SetFloat(Fade, 1f - real);
        pool.SetPropertyBlock(block);
    }
}
