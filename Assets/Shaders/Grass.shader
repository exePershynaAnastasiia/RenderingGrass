
Shader "Custom/ProceduralGrass"
{
    // ShaderLab (no really HLSL) properties block
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {} // Texture containing grass image
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1) // Color multiplier
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.4 // Which transparent pixels are discarded

        _WindStrength ("Wind Strength", Range(0, 1)) = 0.15 // How far grass moves
        _WindSpeed ("Wind Speed", Range(0, 10)) = 1.5 // How quickly grass sways
        _WindFrequency ("Wind Frequency", Range(0, 5)) = 1 // How frequently wind waves repeat across field
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0) // Direction in which grass moves

        //player-grass interactions
        _InteractionRadius ("Interaction Radius", Range(0, 3)) = 4.0
        _InteractionStrength ("Interaction Strength", Range(0, 2)) = 0.5

        _GrassColorA ("Grass Color A", Color) = (0.25, 0.45, 0.12, 1)
        _GrassColorB ("Grass Color B", Color) = (0.65, 0.7, 0.25, 1)
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
            //#pragma instancing_options assumeuniformscaling // Assumes grass blades are uniformly scaled => not anymore

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

                // Player interaction settings
                float _InteractionRadius;
                float _InteractionStrength;

                // Grass color variations
                float4 _GrassColorA;
                float4 _GrassColorB;
            CBUFFER_END

            // Player position and forward direction in World Space
            float4 _PlayerPositionWS;
            float4 _PlayerForwardWS;

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

                // Calculate how much the grass bends based on wind strength
                float bendAmount = abs(wave) * _WindStrength;

                // Apply horizontal displacement and slightly lower the grass tip
                float3 displacement = float3(
                    direction.x * wave * _WindStrength * heightFactor,
                    -bendAmount * bendAmount * heightFactor * 0.5,
                    direction.y * wave * _WindStrength * heightFactor
                );

                // Apply wind displacement
                positionWS += displacement;

                // Calculate direction from player to grass cluster
                float2 playerToGrass = centerWS.xz - _PlayerPositionWS.xz;

                // Calculate distance to the grass cluster
                float distanceToPlayer = length(playerToGrass);

                // Normalize the player's forward direction
                float2 forward = _PlayerForwardWS.xz;
                forward /= max(length(forward), 0.0001);

                // Calculate the player's right direction
                float2 right = float2(-forward.y, forward.x);

                // Project grass position onto player's forward and right axes
                float forwardDistance = dot(playerToGrass, forward);
                float sideDistance = dot(playerToGrass, right);

                // Calculate interaction influence based on distance
                float radius = max(_InteractionRadius, 0.0001);

                float distanceInfluence = 1.0 - smoothstep(
                    0.0,
                    radius,
                    distanceToPlayer
                );

                // Apply stronger interaction to grass in front of the player
                float frontInfluence = smoothstep(
                    -0.3,
                    0.8,
                    forwardDistance + 1.0
                );

                // Calculate final interaction strength
                float interaction = distanceInfluence * frontInfluence;

                // Bend grass forward in the player's direction
                float2 forwardBend =
                    forward * interaction * _InteractionStrength;

                // Push grass sideways to create a path
                float sideSign = sideDistance >= 0.0 ? 1.0 : -1.0;

                float2 sidewaysBend =
                    right
                    * sideSign
                    * interaction
                    * _InteractionStrength
                    * 0.6;

                // Combine forward and sideways bending
                float2 playerDisplacement =
                    (forwardBend + sidewaysBend) * heightFactor;

                // Apply horizontal displacement
                positionWS.xz += playerDisplacement;

                // Slightly lower grass tips when bending
                positionWS.y -= length(playerDisplacement) * 0.2;


                ////////////////////////////////////
                //    DECORARIVE VARIATIONS
                ///////////////////////////////////





                ///////////////////////////////////

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

                // Generate stable variation based on grass cluster position
                float randomValue = Hash21(floor(input.positionWS.xz));

                // Blend between two grass colors
                float3 grassTint = lerp(
                    _GrassColorA.rgb,
                    _GrassColorB.rgb,
                    randomValue
                );

                half3 finalColor =
                    texColor.rgb
                    * grassTint
                    * mainLight.color
                    * lighting;
                return half4(finalColor, 1);
            }

            ENDHLSL
        }
    }
}
