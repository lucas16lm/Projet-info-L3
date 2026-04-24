Shader "Yorakeys/Built-In/Stylized_PirateShip/PirateShip"
{
    Properties
    {
        _MainTex ("Base Albedo", 2D) = "white" {}

        _PrimaryMask ("Primary Color Mask", 2D) = "white" {}
        _SecondaryMask ("Secondary Color Mask", 2D) = "white" {}
        _EmissionMask ("Emission Mask", 2D) = "black" {}

        _SwapPrimary ("Primary Color", Color) = (0.9, 0.2, 0.2, 1)
        _SwapSecondary ("Secondary Color", Color) = (0.2, 0.4, 0.9, 1)

        _DefaultWoodA ("Primary Wood", Color) = (0.4, 0.2, 0.1, 1)
        _SwapWoodA ("Swap Wood A", Color) = (0.6, 0.3, 0.1, 1)

        _DefaultWoodB ("Secondary Wood", Color) = (0.5, 0.3, 0.2, 1)
        _SwapWoodB ("Swap Wood B", Color) = (0.7, 0.4, 0.2, 1)

        _DefaultIron ("Iron", Color) = (0.2, 0.2, 0.25, 1)
        _SwapIron ("Swap Iron", Color) = (0.5, 0.5, 0.6, 1)

        _DefaultDetail ("Detail", Color) = (0.7, 0.7, 0.7, 1)
        _SwapDetail ("Swap Detail", Color) = (1, 0.8, 0.3, 1)

        _DefaultRope ("Rope", Color) = (0.6, 0.4, 0.2, 1)
        _SwapRope ("Swap Rope", Color) = (0.8, 0.6, 0.3, 1)

        _DefaultSail ("Sail", Color) = (0.8, 0.8, 0.8, 1)
        _SwapSail ("Swap Sail", Color) = (1, 1, 0.7, 1)

        _DefaultLamp ("Default Lamp", Color) = (1, 0.8, 0.6, 1)
        _SwapLamp ("Lamp Color", Color) = (1, 1, 0.2, 1)

        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 1
        _Tolerance ("Color Match Tolerance", Float) = 0.08

        _Metallic ("Metallic", Range(0,1)) = 0.3
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex, _PrimaryMask, _SecondaryMask, _EmissionMask;

        float4 _SwapPrimary, _SwapSecondary;
        float _PrimaryTintStrength, _SecondaryTintStrength;

        float4 _DefaultWoodA, _SwapWoodA;
        float4 _DefaultWoodB, _SwapWoodB;
        float4 _DefaultIron, _SwapIron;
        float4 _DefaultDetail, _SwapDetail;
        float4 _DefaultRope, _SwapRope;
        float4 _DefaultSail, _SwapSail;

        float4 _DefaultLamp, _SwapLamp;

        float _EmissionIntensity;
        float _Tolerance;
        float _Metallic;
        float _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_PrimaryMask;
            float2 uv_SecondaryMask;
            float2 uv_EmissionMask;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 texColor = tex2D(_MainTex, IN.uv_MainTex);
            float3 col = texColor.rgb;

            float primaryMask = tex2D(_PrimaryMask, IN.uv_PrimaryMask);
            float secondaryMask = tex2D(_SecondaryMask, IN.uv_SecondaryMask);
            float3 emissionMask = tex2D(_EmissionMask, IN.uv_EmissionMask).rgb;

            // Direct color match swaps (non-mask)
            if (distance(col, _DefaultWoodA.rgb) < _Tolerance)
                col = _SwapWoodA.rgb;
            else if (distance(col, _DefaultWoodB.rgb) < _Tolerance)
                col = _SwapWoodB.rgb;
            else if (distance(col, _DefaultIron.rgb) < _Tolerance)
                col = _SwapIron.rgb;
            else if (distance(col, _DefaultDetail.rgb) < _Tolerance)
                col = _SwapDetail.rgb;
            else if (distance(col, _DefaultRope.rgb) < _Tolerance)
                col = _SwapRope.rgb;
            else if (distance(col, _DefaultSail.rgb) < _Tolerance)
                col = _SwapSail.rgb;

            float3 emission = 0;
            if (any(emissionMask > 0.01) && distance(texColor.rgb, _DefaultLamp.rgb) < _Tolerance)
                emission = _SwapLamp.rgb * emissionMask;

            col.rgb = lerp(col.rgb, col.rgb * _SwapPrimary.rgb, primaryMask.r);
            col.rgb = lerp(col.rgb, col.rgb * _SwapSecondary.rgb, secondaryMask.r);

            o.Albedo = col;
            o.Alpha = texColor.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = emission * _EmissionIntensity;
        }
        ENDCG
    }

    FallBack "Standard"
}
