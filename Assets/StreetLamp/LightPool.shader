// The fake pool of light under a street lamp (PLAN NEXT UP 1): a soft warm circle added on top of whatever is on the
// ground. Unlit, so it costs one simple draw. To change how bright or warm the pools are, change Colour on
// Assets/StreetLamp/Light Pool.mat.
Shader "Street Lamp/Light Pool"
{
    Properties
    {
        _Color ("Colour at the centre", Color) = (0.42, 0.37, 0.28, 1)
        _Fade ("Fade", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed _Fade;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 p : TEXCOORD0; // -1 to 1 across the circle
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.p = v.texcoord.xy * 2.0 - 1.0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float f = saturate(1.0 - dot(i.p, i.p));
                f = f * f * f; // bright under the lamp, gone at the edge, about like the real light
                return fixed4(_Color.rgb * f * _Fade, 1.0);
            }
            ENDCG
        }
    }
}
