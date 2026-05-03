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

        // =========================================================
        // PASSE PRINCIPALE : Rendu des couleurs, lumière, et brouillard
        // =========================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // --- MOTS-CLÉS URP POUR OMBRES ET BROUILLARD ---
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            // -----------------------------------------------

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
                float  fogFactor  : TEXCOORD2; // Ajout pour stocker la profondeur du brouillard
            };

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
                
                // Calcul du facteur de brouillard basé sur la profondeur de la caméra
                OUT.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return OUT;
            }

            half3 SampleTriplanar(TEXTURE2D_PARAM(tex, samplerTex), float3 pWS, float3 nWS)
            {
                float2 uv_x = pWS.zy * _TexScale;
                float2 uv_y = pWS.xz * _TexScale;
                float2 uv_z = pWS.xy * _TexScale;

                half3 col_x = SAMPLE_TEXTURE2D(tex, samplerTex, uv_x).rgb;
                half3 col_y = SAMPLE_TEXTURE2D(tex, samplerTex, uv_y).rgb;
                half3 col_z = SAMPLE_TEXTURE2D(tex, samplerTex, uv_z).rgb;

                float3 blending = abs(nWS);
                blending = max(blending, 0.00001); 
                blending = pow(blending, _BlendSharpness);
                
                float totalWeight = blending.x + blending.y + blending.z;
                blending /= totalWeight;

                return col_x * blending.x + col_y * blending.y + col_z * blending.z;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 positionWS = IN.positionWS;

                // 1. ÉCHANTILLONNAGE TRIPLANAR
                half3 grassCol = SampleTriplanar(_GrassTex, sampler_GrassTex, positionWS, normalWS);
                half3 rockCol  = SampleTriplanar(_RockTex,  sampler_RockTex,  positionWS, normalWS);
                half3 sandCol  = SampleTriplanar(_SandTex,  sampler_SandTex,  positionWS, normalWS);

                // 2. LOGIQUE DES BIOMES
                float heightMask = smoothstep(_SandHeight - _SandBlend, _SandHeight + _SandBlend, positionWS.y);
                half3 flatTerrainColor = lerp(sandCol, grassCol, heightMask);

                float slopeMask = smoothstep(_SlopeThreshold - _SlopeBlend, _SlopeThreshold + _SlopeBlend, normalWS.y);
                half3 albedo = lerp(rockCol, flatTerrainColor, slopeMask);

                // 3. LUMIÈRE ET OMBRES
                // On récupère les coordonnées de l'ombre pour ce pixel
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                
                // On passe le shadowCoord à GetMainLight pour qu'elle calcule l'atténuation
                Light mainLight = GetMainLight(shadowCoord);
                
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Éclairage ambiant dynamique (remplace le + 0.15) via les Spherical Harmonics
                half3 ambientColor = SampleSH(normalWS) * albedo;
                
                // Calcul de la lumière principale multipliée par l'atténuation de l'ombre (mainLight.shadowAttenuation)
                half3 diffuseColor = albedo.rgb * mainLight.color * NdotL * mainLight.shadowAttenuation;
                
                half3 finalColor = diffuseColor + ambientColor;

                // 4. BROUILLARD
                // On mixe la couleur finale avec la couleur du brouillard global de Unity
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // =========================================================
        // PASSE SHADOW CASTER : Permet au terrain de projeter des ombres
        // =========================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // --- CORRECTION : Déclaration des variables de lumière ---
            // Ces variables sont remplies automatiquement par Unity lors de cette passe.
            float4 _LightPosition;
            float3 _LightDirection;
            // ---------------------------------------------------------

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition.xyz - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float3 positionWS_Biased = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                float4 positionCS = TransformWorldToHClip(positionWS_Biased);

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // =========================================================
        // PASSE DEPTH NORMALS : Indispensable pour l'écume et l'intersection de l'eau
        // =========================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0; // On a besoin de transmettre la normale
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // On convertit la normale de l'objet vers l'espace global (World Space)
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // URP attend simplement la normale en World Space dans la texture DepthNormals
                float3 normalWS = normalize(IN.normalWS);
                
                // On retourne la normale dans les canaux RGB, l'Alpha reste à 0
                return half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}