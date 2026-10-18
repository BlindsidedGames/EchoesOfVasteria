Shader "Echoes/Character Composite"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Layer0("Body", 2D) = "black" {}
        _Layer1("Legs", 2D) = "black" {}
        _Layer2("Feet", 2D) = "black" {}
        _Layer3("Chest", 2D) = "black" {}
        _Layer4("Hands", 2D) = "black" {}
        _Layer5("Head", 2D) = "black" {}
        _Layer6("Accessory", 2D) = "black" {}
        _Layer7("Tool", 2D) = "black" {}
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
        TEXTURE2D(_Layer0); SAMPLER(sampler_Layer0);
        TEXTURE2D(_Layer1); SAMPLER(sampler_Layer1);
        TEXTURE2D(_Layer2); SAMPLER(sampler_Layer2);
        TEXTURE2D(_Layer3); SAMPLER(sampler_Layer3);
        TEXTURE2D(_Layer4); SAMPLER(sampler_Layer4);
        TEXTURE2D(_Layer5); SAMPLER(sampler_Layer5);
        TEXTURE2D(_Layer6); SAMPLER(sampler_Layer6);
        TEXTURE2D(_Layer7); SAMPLER(sampler_Layer7);
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float4 _LayerRect0, _LayerRect1, _LayerRect2, _LayerRect3;
            float4 _LayerRect4, _LayerRect5, _LayerRect6, _LayerRect7;
            float _ToolBehind;
        CBUFFER_END

        struct Attributes { COMMON_2D_NORMALS_INPUTS half4 color : COLOR; };
        struct Varyings {
            COMMON_2D_LIT_OUTPUTS
            half4 color : COLOR;
            half3 surfaceNormalWS : TEXCOORD4;
            half3 tangentWS : TEXCOORD5;
            half3 bitangentWS : TEXCOORD6;
        };
        Varyings CharacterVertex(Attributes input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
            SetUpSpriteInstanceProperties();
            input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
            Varyings output = (Varyings)0;
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            output.lightingUV = ComputeScreenPos(output.positionCS / output.positionCS.w).xy;
            output.color = input.color * _Color * unity_SpriteColor;
            output.surfaceNormalWS = TransformObjectToWorldDir(input.normal);
            output.tangentWS = TransformObjectToWorldDir(input.tangent.xyz);
            output.bitangentWS = cross(output.surfaceNormalWS, output.tangentWS) * input.tangent.w;
            return output;
        }
        half4 Layer(TEXTURE2D_PARAM(textureName, samplerName), float4 rect, float2 uv)
        {
            if (rect.z <= 0 || rect.w <= 0) return 0;
            return SAMPLE_TEXTURE2D(textureName, samplerName, rect.xy + uv * rect.zw);
        }
        half4 Over(half4 underneath, half4 top)
        {
            // Accumulate premultiplied colour before applying the character's overall tint.
            return half4(top.rgb * top.a + underneath.rgb * (1 - top.a), top.a + underneath.a * (1 - top.a));
        }
        half4 CharacterColour(float2 uv)
        {
            half4 tool = Layer(TEXTURE2D_ARGS(_Layer7, sampler_Layer7), _LayerRect7, uv);
            half4 result = _ToolBehind > .5 ? Over(0, tool) : 0;
            result = Over(result, Layer(TEXTURE2D_ARGS(_Layer0, sampler_Layer0), _LayerRect0, uv));
            result = Over(result, Layer(TEXTURE2D_ARGS(_Layer5, sampler_Layer5), _LayerRect5, uv));
            result = Over(result, Layer(TEXTURE2D_ARGS(_Layer1, sampler_Layer1), _LayerRect1, uv));
            result = Over(result, Layer(TEXTURE2D_ARGS(_Layer2, sampler_Layer2), _LayerRect2, uv));
            result = Over(result, Layer(TEXTURE2D_ARGS(_Layer3, sampler_Layer3), _LayerRect3, uv));
            result = Over(result, Layer(TEXTURE2D_ARGS(_Layer4, sampler_Layer4), _LayerRect4, uv));
            if (_ToolBehind <= .5) result = Over(result, tool);
            result = Over(result, Layer(TEXTURE2D_ARGS(_Layer6, sampler_Layer6), _LayerRect6, uv));
            result.rgb /= max(result.a, .00001);
            return result;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex CharacterVertex
            #pragma fragment CharacterLitFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            half4 CharacterLitFragment(Varyings input) : SV_Target
            {
                half4 colour = CharacterColour(input.uv) * input.color;
                SurfaceData2D surface;
                InputData2D lighting;
                InitializeSurfaceData(colour.rgb, colour.a, half4(1,1,1,1), half3(0,0,1), surface);
                InitializeInputData(input.uv, input.lightingUV, lighting);
                return CombinedShapeLightShared(surface, lighting);
            }
            ENDHLSL
        }
        Pass
        {
            Tags { "LightMode"="NormalsRendering" }
            HLSLPROGRAM
            #pragma vertex CharacterVertex
            #pragma fragment CharacterNormalsFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
            half4 CharacterNormalsFragment(Varyings input) : SV_Target
            {
                SetUpSpriteInstanceProperties();
                return NormalsRenderingShared(CharacterColour(input.uv) * input.color, half3(0,0,1), input.tangentWS, input.bitangentWS, input.surfaceNormalWS);
            }
            ENDHLSL
        }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex CharacterVertex
            #pragma fragment CharacterUnlitFragment
            #pragma multi_compile_instancing
            half4 CharacterUnlitFragment(Varyings input) : SV_Target { return CharacterColour(input.uv) * input.color; }
            ENDHLSL
        }
    }
}
