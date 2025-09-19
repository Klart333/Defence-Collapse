Shader "DELTation/Inverted Hull Outline"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 0)
        _Scale ("Scale", Range(0, 10)) = 0.05
        _DepthOffsetFactor ("Offset Factor", Float) = 0
        _DepthOffsetUnits ("Offset Units", Float) = 0
        [Toggle(CLIP_SPACE)]
        _ClipSpace ("Clip Space", Float) = 1
        [Toggle(CUSTOM_NORMALS)]
        _CustomNormals ("Custom Normals (Tangent)", Float) = 0

        [Toggle(USE_DITHER)]
        _UseDither ("Use Dither", Float) = 1
        _DitherThreshold ("Dither Threshold", Float) = 2.5
    }
    SubShader
    {
        Cull Front ZWrite On ZTest LEqual
        Offset [_DepthOffsetFactor], [_DepthOffsetUnits]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_fragment USE_DITHER
            #pragma shader_feature_vertex CLIP_SPACE
            #pragma shader_feature_vertex CUSTOM_NORMALS
            #pragma shader_feature_vertex FALLBACK_TO_DEFAULT_NORMALS

            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float3 vertex : POSITION;
                #ifdef CUSTOM_NORMALS
                float3 normal : TEXCOORD7;
                #else
                float3 normal : NORMAL;
                #endif
            };

            struct v2f
            {
                float4 position_cs : SV_POSITION;
                float3 position_ws : TEXCOORD0;
                float fog_factor : FOG_FACTOR;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Scale;
            float _DitherThreshold;
            CBUFFER_END
            float4 _MousePos;
            int _UseMousePos;

            v2f vert (const appdata input)
            {
                v2f output;
                
                float3 worldPos = TransformObjectToWorld(input.vertex);

#ifdef CLIP_SPACE
                float4 vertex_hclip = TransformWorldToHClip(worldPos);
                float3 normal_hclip = TransformWorldToHClipDir(TransformObjectToWorldNormal(input.normal, true));
                output.position_cs = vertex_hclip + float4(normal_hclip * _Scale, 0);
#else
                output.position_cs = TransformObjectToHClip(input.vertex + normalize(input.normal) * _Scale);
#endif

                output.position_ws = worldPos;
                output.fog_factor = ComputeFogFactor(output.position_cs.z);

                return output;
            }

            float4 frag(const v2f input) : SV_Target
            {
#ifdef USE_DITHER
                if (_UseMousePos == 1)
                {
                    const float DITHER_THRESHOLDS[16] =
                    {
                        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                    };
                    
                    float dist = distance(_MousePos.xy, input.position_ws.xz);
                    float2 uv = input.position_cs.xy * _ScreenParams.xy;
                    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                    float threshold = (dist - _DitherThreshold) - DITHER_THRESHOLDS[index];
                    
                    // Clip based on dithering threshold
                    if (threshold <= 0)
                        discard;
                }
                
#endif

                float4 output = _Color;
                output.rgb = MixFog(output.rgb, input.fog_factor);
                return output;
            }
            ENDHLSL
        }
    }
}
