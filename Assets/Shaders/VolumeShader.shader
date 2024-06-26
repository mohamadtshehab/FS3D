Shader "Custom/VolumeShader"
{
    Properties
    {
        _VolumeTex ("Volume Texture", 3D) = "white" {}
        _Slice ("Slice", Range(0, 63)) = 0.5
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler3D _VolumeTex;
            float _Slice;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.texcoord.z = _Slice;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float threshold = 0;
                float3 density = tex3D(_VolumeTex, i.texcoord).rgb;

                if (density.r < threshold || density.g  < threshold || density.b < threshold){
                    discard;
                    }
                return fixed4(density.r, density.r, density.r, density.r);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
