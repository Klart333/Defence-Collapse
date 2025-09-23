Shader "Custom/JumpFloodOutline"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
	    _OutlineColor ("Outline Color", Color) = (1,1,1,1)
	    _OutlineWidth ("Outline Width", float) = 2
		
		[HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
		[HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite Off
	    ZTest Always
	    
	    HLSLINCLUDE
        // just inside the precision of a R16G16_SNorm to keep encoded range 1.0 >= and > -1.0
        #define SNORM16_MAX_FLOAT_MINUS_EPSILON ((float)(32768-2) / (float)(32768-1))
        #define FLOOD_ENCODE_OFFSET float2(1.0, SNORM16_MAX_FLOAT_MINUS_EPSILON)
        #define FLOOD_ENCODE_SCALE float2(2.0, 1.0 + SNORM16_MAX_FLOAT_MINUS_EPSILON)

        #define FLOOD_NULL_POS -1.0
        #define FLOOD_NULL_POS_FLOAT2 float2(FLOOD_NULL_POS, FLOOD_NULL_POS)
        ENDHLSL

        // This pass is copied straight from Sprite-Unlit-Default
		Pass
		{
			Name "SPRITE RENDER"
		    Blend SrcAlpha OneMinusSrcAlpha
			Tags { "LightMode" = "Universal2D" }

			HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #if defined(DEBUG_DISPLAY)
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
            #endif

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            #pragma multi_compile _ DEBUG_DISPLAY

            struct attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                #if defined(DEBUG_DISPLAY)
                float3  positionWS  : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;
            float4 _Color;
            half4 _RendererColor;

            Varyings UnlitVertex(attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(v.positionOS);
                #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                #if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;

                InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
                InitializeInputData(i.uv, inputData);
                SETUP_DEBUG_DATA_2D(inputData, i.positionWS);

                if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
                #endif

                return mainTex;
            }
            ENDHLSL
		}
		
		// Pass 1 - Create alpha mask
		Pass 
		{
			Name "BUFFERFILL"
			Tags { "LightMode" = "PlayerCharacter" }
		    Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			// #pragma target 4.5

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct attributes
            {
                float3 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float2  uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

			TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;

            Varyings vert(attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);
            	o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                return mainTex.a;
            }
            ENDHLSL
		}
		
		Pass // 2
        {
            Name "JUMPFLOODINIT"
            Tags { "LightMode" = "PlayerCharacter" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma target 4.5

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                return o;
            }

            float2 frag (v2f i) : SV_Target {
                // integer pixel position
                int2 uvInt = i.pos.xy;

                // sample silhouette texture for sobel
                half3x3 values;
                UNITY_UNROLL
                for(int u=0; u<3; u++)
                {
                    UNITY_UNROLL
                    for(int v=0; v<3; v++)
                    {
                        uint2 sampleUV = clamp(uvInt + int2(u-1, v-1), int2(0,0), (int2)_MainTex_TexelSize.zw - 1);
                        values[u][v] = _MainTex.Load(int3(sampleUV, 0)).r;
                    }
                }

                // calculate output position for this pixel
                float2 outPos = i.pos.xy * abs(_MainTex_TexelSize.xy) * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET;

                // interior, return position
                if (values._m11 > 0.99)
                    return FLOOD_NULL_POS_FLOAT2;

                // exterior, return no position
                if (values._m11 < 0.01)
                    return FLOOD_NULL_POS_FLOAT2;

                // sobel to estimate edge direction
                float2 dir = -float2(
                    values[0][0] + values[0][1] * 2.0 + values[0][2] - values[2][0] - values[2][1] * 2.0 - values[2][2],
                    values[0][0] + values[1][0] * 2.0 + values[2][0] - values[0][2] - values[1][2] * 2.0 - values[2][2]
                    );

                // if dir length is small, this is either a sub pixel dot or line
                // no way to estimate sub pixel edge, so output position
                if (abs(dir.x) <= 0.005 && abs(dir.y) <= 0.005)
                    return outPos;

                // normalize direction
                dir = normalize(dir);

                // sub pixel offset
                float2 offset = dir * (1.0 - values._m11);

                // output encoded offset position
                return (i.pos.xy + offset) * abs(_MainTex_TexelSize.xy) * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET;
            }
            ENDHLSL
        }

        Pass // 3
        {
            Name "JUMPFLOOD_SINGLEAXIS"
            Tags { "LightMode" = "PlayerCharacter" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma target 4.5

            struct appdata
            {
                float3 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;
            int2 _AxisWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                return o;
            }

            half2 frag (v2f i) : SV_Target {
                // integer pixel position
                int2 uvInt = int2(i.pos.xy);

                // initialize best distance at infinity
                float bestDist = 1.#INF;
                float2 bestCoord;

                // jump samples
                // only one loop
                UNITY_UNROLL
                for(int u=-1; u<=1; u++)
                {
                    // calculate offset sample position
                    int2 offsetUV = uvInt + _AxisWidth * u;

                    // .Load() acts funny when sampling outside of bounds, so don't
                    offsetUV = clamp(offsetUV, int2(0,0), (int2)_MainTex_TexelSize.zw - 1);

                    // decode position from buffer
                    float2 offsetPos = (_MainTex.Load(int3(offsetUV, 0)).rg + FLOOD_ENCODE_OFFSET) * _MainTex_TexelSize.zw / FLOOD_ENCODE_SCALE;

                    // the offset from current position
                    float2 disp = i.pos.xy - offsetPos;

                    // square distance
                    float dist = dot(disp, disp);

                    // if offset position isn't a null position or is closer than the best
                    // set as the new best and store the position
                    if (offsetPos.x != -1.0 && dist < bestDist)
                    {
                        bestDist = dist;
                        bestCoord = offsetPos;
                    }
                }

                // if not valid best distance output null position, otherwise output encoded position
                return isinf(bestDist) ? FLOOD_NULL_POS_FLOAT2 : bestCoord * _MainTex_TexelSize.xy * FLOOD_ENCODE_SCALE - FLOOD_ENCODE_OFFSET;
            }
            ENDHLSL
        }

        Pass // 4
        {
            Name "JUMPFLOODOUTLINE"
            Tags { "LightMode" = "PlayerCharacter" }

//            Stencil {
//                Ref 1
//                ReadMask 1
//                WriteMask 1
//                Comp NotEqual
//                Pass Zero
//                Fail Zero
//            }

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma target 4.5

            struct appdata
            {
                float3 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            Texture2D _MainTex;
            Texture2D _OutlineMaskBuffer;

            half4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target {
                // integer pixel position
                int2 uvInt = int2(i.pos.xy);

                // load encoded position
                float2 encodedPos = _MainTex.Load(int3(uvInt, 0)).rg;

                // early out if null position
                if (encodedPos.y == -1)
                    return half4(0,0,0,0);

                // decode closest position
                float2 nearestPos = (encodedPos + FLOOD_ENCODE_OFFSET) * abs(_ScreenParams.xy) / FLOOD_ENCODE_SCALE;

                // current pixel position
                float2 currentPos = i.pos.xy;

                // distance in pixels to closest position
                half dist = length(nearestPos - currentPos);

                // calculate outline
                // + 1.0 is because encoded nearest position is half a pixel inset
                // not + 0.5 because we want the anti-aliased edge to be aligned between pixels
                // distance is already in pixels, so this is already perfectly anti-aliased!
                half outline = saturate(_OutlineWidth - dist + 1.0);

                // apply outline to alpha
                half4 col = _OutlineColor;
                col.a *= outline;
            	col.a *= 1-_OutlineMaskBuffer.Load(int3(uvInt, 0)).r;

                // profit!
                return col;
            }
            ENDHLSL
        }
	   
	}
}