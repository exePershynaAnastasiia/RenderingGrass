
Shader "Custom/ProceduralGrass"
{
    // ShaderLab (no really HLSL) properties block
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {} // Texture containing grass image
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1) // Color multiplier
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.4 // Which transparent pixels are discarded

        _WindStrength ("Wind Strength", Range(0, 1)) = 0.2 // How far grass moves
        _WindSpeed ("Wind Speed", Range(0, 10)) = 2 // How quickly grass sways
        _WindFrequency ("Wind Frequency", Range(0, 5)) = 1 // How frequently wind waves repeat across field
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0) // Direction in which grass moves
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "GrassForward"

            Tags { "LightMode" = "UniversalForward" }

            Cull Off // Renders both sides of the grass planes
            ZWrite On // Enables writing to the depth buffer

            HLSLPROGRAM

            #pragma vertex vert // Specifies vert() as the vertex shader
            #pragma fragment frag // Specifies frag() as the fragment shader
            #pragma multi_compile_instancing // Enables GPU instancing for grass blades
            #pragma instancing_options assumeuniformscaling // Assumes grass blades are uniformly scaled

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Includes core functions and macros for URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // Includes lighting functions and macros for URP

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Constant buffer for material properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;

                float _WindStrength;
                float _WindSpeed;
                float _WindFrequency;
                float4 _WindDirection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION; // vertex position in Object Space
                float3 normalOS : NORMAL; // vertex normal in Object Space
                float2 uv : TEXCOORD0; // UV coordinates for texture mapping

                UNITY_VERTEX_INPUT_INSTANCE_ID // Macro for GPU instancing
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION; // Clip space position for rasterization
                float2 uv : TEXCOORD0; // UV coordinates passed to the fragment shader
                float3 normalWS : TEXCOORD1; // vertex normal in World Space
                float3 positionWS : TEXCOORD2; // vertex position in World Space

                UNITY_VERTEX_INPUT_INSTANCE_ID // Macro for GPU instancing
            };

            // Hash21() generates a deterministic pseudo-random value between 0 and 1 based on a 2D input
            float Hash21(float2 p)
            {
                return frac(
                    sin(dot(p, float2(127.1, 311.7)))
                    * 43758.5453
                );
            }

            // vert() is the vertex shader function where animation happens
            Varyings vert(Attributes input)
            {
                Varyings output; // Output structure to pass data to the fragment shader

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output); 

                float3 positionWS =
                    TransformObjectToWorld(input.positionOS.xyz);

                float3 centerWS =
                    TransformObjectToWorld(float3(0, 0, 0)); // Center of the grass object in World Space

                float randomPhase =
                    Hash21(centerWS.xz) * 6.2831853;

                // Wind calculations
                float wave =
                    sin(
                        _Time.y * _WindSpeed
                        + dot(centerWS.xz, float2(0.7, 0.5))
                        * _WindFrequency
                        + randomPhase
                    );

                float heightFactor =
                    input.uv.y * input.uv.y; // Makes the upper vertices move more than the lower ones

                float2 direction = _WindDirection.xz; // Extracts the horizontal components of the wind direction
                direction /= max(length(direction), 0.0001);

                float3 displacement = float3(
                    direction.x,
                    0,
                    direction.y
                ) * wave * _WindStrength * heightFactor;

                positionWS += displacement;

                output.positionWS = positionWS;
                output.positionCS =
                    TransformWorldToHClip(positionWS);

                output.normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv
                );

                clip(texColor.a - _Cutoff);

                Light mainLight = GetMainLight();

                float3 normalWS = normalize(input.normalWS);

                float NdotL = saturate(
                    dot(normalWS, mainLight.direction) // Calculates how directly the surface faces the light
                );

                float lighting = 0.45 + NdotL * 0.55; // Ambient light contribution + directional light contribution

                float colorVariation =
                    lerp(
                        0.85,
                        1.15,
                        Hash21(floor(input.positionWS.xz)) // Generates a pseudo-random value based on the vertex position
                    );

                half3 finalColor =
                    texColor.rgb
                    * _BaseColor.rgb
                    * mainLight.color
                    * lighting
                    * colorVariation;

                return half4(finalColor, 1);
            }

            ENDHLSL
        }
    }
}
