Shader "Yorakeys/Built-In/Stylized_PirateShip/Sail"
{
    Properties
    {
        _BaseTexture("Base Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
        _Strength("Wind Strength", Range(0, 1)) = 0.5
        _Jitter("Jitter", Range(0.5, 1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _TearMask("Tear Mask", 2D) = "white" {}
        _IsTeared("Is Teared", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        sampler2D _BaseTexture;
        fixed4 _Color;
        float _Strength;
        float _Jitter;
        float _Metallic;
        float _Glossiness;

        sampler2D _TearMask;
        float _IsTeared;

        struct Input
        {
            float2 uv_BaseTexture;
            float2 uv_TearMask;
        };

        float hash(float2 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * (p.x + p.y));
        }

        float simpleNoise(float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);

            float a = hash(i);
            float b = hash(i + float2(1, 0));
            float c = hash(i + float2(0, 1));
            float d = hash(i + float2(1, 1));

            float2 u = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }

        void vert(inout appdata_full v)
        {
            float3 posOS = v.vertex.xyz;
            float time = _Time.y;
            time = _Strength * 5 * time;

            float2 centered = posOS.xz * 0.1;
            float radius = length(centered);
            float distortedRadius = pow(radius * 2.0, 2.0);
            float ripple = (1.0 - distortedRadius) * _Strength;

            float n = simpleNoise(centered * 10.0 + time * _Jitter);
            float offset = n * ripple;

            v.vertex.xyz += v.normal * offset;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float tearAlpha = tex2D(_TearMask, IN.uv_TearMask).a;
            clip(_IsTeared > 0.5 ? tearAlpha - 0.1 : 1);
            fixed4 col = tex2D(_BaseTexture, IN.uv_BaseTexture);
            o.Albedo = col.rgb * _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = col.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
