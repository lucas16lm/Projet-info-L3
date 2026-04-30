Shader "Custom/TerrainTriplanar"
{
    Properties
    {
        [Header(Textures)]
        _GrassTex ("Grass Texture", 2D) = "white" {}
        _RockTex ("Rock Texture", 2D) = "white" {}
        _SandTex ("Sand Texture", 2D) = "white" {}
        
        [Header(Triplanar Settings)]
        _TexScale ("Texture Scale", Float) = 1.0
        // Contrôle la netteté de la transition entre les projections X/Y/Z.
        // Élevé = transition dure. Bas = transition douce.
        _BlendSharpness ("Blend Sharpness", Range(1, 10)) = 4.0 

        [Header(Sand Settings (Height))]
        _SandHeight ("Sand Max Height", Float) = 0.4
        _SandBlend ("Sand Blend Smoothness", Float) = 0.1

        [Header(Rock Settings (Slope))]
        _SlopeThreshold ("Slope Threshold", Range(0, 1)) = 0.8
        _SlopeBlend ("Slope Blend Smoothness", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            // Déclaration des Textures et Samplers
            TEXTURE2D(_GrassTex); SAMPLER(sampler_GrassTex);
            TEXTURE2D(_RockTex);  SAMPLER(sampler_RockTex);
            TEXTURE2D(_SandTex);  SAMPLER(sampler_SandTex);

            CBUFFER_START(UnityPerMaterial)
                float _TexScale;
                float _BlendSharpness;
                float _SandHeight;
                float _SandBlend;
                float _SlopeThreshold;
                float _SlopeBlend;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = normalInput.normalWS;
                return OUT;
            }

            // --- FONCTION UTILITAIRE : ÉCHANTILLONNAGE TRIPLANAR ---
            // Cette fonction lit une texture 3 fois et mélange les résultats.
            half3 SampleTriplanar(TEXTURE2D_PARAM(tex, samplerTex), float3 pWS, float3 nWS)
            {
                // 1. Calcul des coordonnées UV pour les 3 projections
                float2 uv_x = pWS.zy * _TexScale; // Projection sur l'axe X
                float2 uv_y = pWS.xz * _TexScale; // Projection sur l'axe Y (le plat)
                float2 uv_z = pWS.xy * _TexScale; // Projection sur l'axe Z

                // 2. Échantillonnage des 3 textures
                half3 col_x = SAMPLE_TEXTURE2D(tex, samplerTex, uv_x).rgb;
                half3 col_y = SAMPLE_TEXTURE2D(tex, samplerTex, uv_y).rgb;
                half3 col_z = SAMPLE_TEXTURE2D(tex, samplerTex, uv_z).rgb;

                // 3. Calcul des poids de mélange basés sur la normale
                // On utilise la valeur absolue car la normale peut être négative.
                float3 blending = abs(nWS);
                // On s'assure qu'on ne divise pas par zéro
                blending = max(blending, 0.00001); 
                
                // On élève à la puissance sharpness pour rendre la transition plus nette.
                blending = pow(blending, _BlendSharpness);
                
                // Normalisation : la somme des poids doit être égale à 1.
                float totalWeight = blending.x + blending.y + blending.z;
                blending /= totalWeight;

                // 4. Mélange final
                return col_x * blending.x + col_y * blending.y + col_z * blending.z;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                // On normalise la normale reçue pour avoir des calculs précis
                float3 normalWS = normalize(IN.normalWS);
                float3 positionWS = IN.positionWS;

                // --- 1. ÉCHANTILLONNAGE TRIPLANAR DES BIOMES ---
                // On n'utilise plus SAMPLE_TEXTURE2D mais SampleTriplanar
                half3 grassCol = SampleTriplanar(_GrassTex, sampler_GrassTex, positionWS, normalWS);
                half3 rockCol  = SampleTriplanar(_RockTex,  sampler_RockTex,  positionWS, normalWS);
                half3 sandCol  = SampleTriplanar(_SandTex,  sampler_SandTex,  positionWS, normalWS);

                // --- 2. LOGIQUE DES BIOMES (Identique à avant) ---
                // Hauteur (Sable vs Plaines)
                float heightMask = smoothstep(_SandHeight - _SandBlend, _SandHeight + _SandBlend, positionWS.y);
                half3 flatTerrainColor = lerp(sandCol, grassCol, heightMask);

                // Pente (Roche vs Terrain Plat)
                // normalWS.y vaut 1 sur le plat, 0 sur un mur vertical
                float slopeMask = smoothstep(_SlopeThreshold - _SlopeBlend, _SlopeThreshold + _SlopeBlend, normalWS.y);

                // --- 3. FUSION ET LUMIÈRE ---
                half3 albedo = lerp(rockCol, flatTerrainColor, slopeMask);

                // Calcul de la lumière principale (Directional Light)
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Ambiante basique (0.15)
                half3 finalColor = albedo.rgb * (mainLight.color * NdotL + 0.15);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}