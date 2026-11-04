using UnityEngine;

/// <summary>
/// Tiles a road's texture by its real size in metres, so it never looks stretched.
/// A material only has one tiling value, but the roads are long thin planes of different shapes
/// (TopRoad is about 38 x 1100 m along Z, SideRoad about 950 x 38 m along X) that share one material.
/// One tiling cannot fit both, and on a 1100 m plane 30 tiles along the length means 36 m per tile.
/// This script works out the tiling for each road from its size and the texture's shape and sets it
/// on that road only (a MaterialPropertyBlock), so the shared material is not changed.
/// Re-sizing the road in the editor updates it straight away.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class RoadTiling : MonoBehaviour
{
    [Tooltip("How many metres one copy of the texture covers across the road. Smaller = finer grain. "
           + "The length of a tile follows from the texture's shape, so it is never stretched.")]
    [Min(0.1f)] public float metresPerTile = 6f;

    [Tooltip("Shift the texture (in tiles) so roads that meet do not line up their patterns.")]
    public Vector2 offset;

    private static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");
    private MaterialPropertyBlock block;
    private Vector3 lastScale;
    private float lastMetres;

    private void OnEnable() => Apply();
    private void OnValidate() => Apply();

    private void OnDisable()
    {
        // give the road its material's own tiling back
        var r = GetComponent<Renderer>();
        if (r != null) r.SetPropertyBlock(null);
    }

#if UNITY_EDITOR
    // only needed while editing (roads do not change size in play), so the build pays nothing per frame
    private void Update()
    {
        if (Application.isPlaying) return;
        if (transform.lossyScale != lastScale || !Mathf.Approximately(metresPerTile, lastMetres)) Apply();
    }
#endif

    public void Apply()
    {
        var r = GetComponent<Renderer>();
        var mf = GetComponent<MeshFilter>();
        if (r == null || mf == null || mf.sharedMesh == null || r.sharedMaterial == null) return;

        lastScale = transform.lossyScale;
        lastMetres = metresPerTile;

        // size of the road surface in metres. A Plane is flat in Y (UV u = X, v = Z), a Quad is flat in Z (u = X, v = Y).
        Vector3 size = Vector3.Scale(mf.sharedMesh.bounds.size, transform.lossyScale);
        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        bool flatInY = size.y <= size.z;
        float across = size.x;
        float along = flatInY ? size.z : size.y;

        // keep the texture's own shape: a 1500 x 1730 photo covers 6 m by 6.9 m, not 6 by 36
        float aspect = 1f; // height / width
        var tex = r.sharedMaterial.mainTexture;
        if (tex != null && tex.width > 0) aspect = (float)tex.height / tex.width;

        float tileW = metresPerTile;
        float tileH = metresPerTile * aspect;
        var st = new Vector4(across / tileW, along / tileH, offset.x, offset.y);

        block ??= new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetVector(MainTexST, st);
        r.SetPropertyBlock(block);
    }
}
