Shader "DELTation/Inverted Hull Outline"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 0)
        _Scale ("Scale", Range(0, 0.1)) = 0.05
        _DepthOffsetFactor ("Offset Factor", Float) = 0
        _DepthOffsetUnits ("Offset Units", Float) = 0
        [Toggle(CLIP_SPACE)]
        _ClipSpace ("Clip Space", Float) = 1
        [Toggle(CUSTOM_NORMALS)]
        _CustomNormals ("Custom Normals (Tangent)", Float) = 0
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
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma target 4.5
            #pragma shader_feature_vertex CLIP_SPACE
            #pragma shader_feature_vertex CUSTOM_NORMALS
            #pragma shader_feature_vertex FALLBACK_TO_DEFAULT_NORMALS

            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            
            struct appdata
            {
                float3 vertex : POSITION;
                #ifdef CUSTOM_NORMALS
                float3 normal : TEXCOORD7;
                #else
                float3 normal : NORMAL;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position_cs : SV_POSITION;
                float3 position_ws : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Per-material + per-instance data
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Scale;
            float _DitherThreshold;
            CBUFFER_END
            
#ifdef DOTS_INSTANCING_ON
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _Color)
                UNITY_DOTS_INSTANCED_PROP(float, _Scale)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
            #define _Color UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _Color)
            #define _Scale UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Scale)
#endif
            
            v2f vert(const appdata input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                v2f output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 worldPos = TransformObjectToWorld(input.vertex);

            #ifdef CLIP_SPACE
                float4 vertex_hclip = TransformWorldToHClip(worldPos);
                float3 normal_hclip = TransformWorldToHClipDir(TransformObjectToWorldNormal(input.normal, true));
                output.position_cs = vertex_hclip + float4(normal_hclip * _Scale, 0);
            #else
                output.position_cs = TransformObjectToHClip(input.vertex + normalize(input.normal) * _Scale);
            #endif

                output.position_ws = worldPos;
                
                return output;
            }

            float4 frag(const v2f input) : SV_Target
            {
                return _Color; 
            }
            ENDHLSL
        }
    }
}
