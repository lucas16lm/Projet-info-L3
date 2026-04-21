Shader "Yorakeys/Built-In/Stylized_PirateShip/Flag"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _WindSpeed("Wind Speed", Float) = 1
        _WaveLength("Wave Length", Float) = 1
        _WindScale("Wind Scale", Float) = 0.2
        _Direction("Wind Direction", Vector) = (1, 0, 0, 0)
        _Metallic("Metallic", Range(0,1)) = 0
        _Glossiness("Smoothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        fixed4 _Color;
        float _WindSpeed;
        float _WaveLength;
        float _WindScale;
        float4 _Direction; // X, Y used

        float _Metallic;
        float _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void vert(inout appdata_full v)
        {
            float3 objPos = v.vertex.xyz;
            float2 pos2D = objPos.xy;
            float2 windDir = normalize(_Direction.xy);

            float phase = dot(pos2D, windDir);
            float timeWave = _Time.y * _WindSpeed;
            float wave = sin(phase * _WaveLength + timeWave);

            float offset = wave * _WindScale;
            float3 offsetDir = float3(-windDir.y, windDir.x, 0); // perpendicular for visual twist

            v.vertex.xyz += offset * offsetDir;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
