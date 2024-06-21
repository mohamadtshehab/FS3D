Shader "Unlit/3DTextureShader"
{
    Properties
    {
        _VolumeTex ("Volume Texture", 3D) = "white" {}
        _Samples ("Number of Samples", Range(1, 100)) = 10 // Number of samples to take along the z-axis
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler3D _VolumeTex;
            float _Samples;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = half4(0, 0, 0, 0);
                // Loop to sample along the z-axis
                for (int j = 0; j < _Samples; ++j)
                {
                    float z = j / (_Samples - 1.0);
                    col += tex3D(_VolumeTex, float3(i.uv, z));
                }
                col /= _Samples; // Average the samples
                return col;
            }
            ENDCG
        }
    }
}
