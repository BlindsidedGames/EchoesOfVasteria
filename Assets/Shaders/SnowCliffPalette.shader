Shader "Echoes/Snow Cliff Palette"
{
 Properties { [PerRendererData] _MainTex("Sprite Texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
 Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _Color;
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 float3 palette(float3 c){
 #ifndef UNITY_COLORSPACE_GAMMA
 c=GammaToLinearSpace(c);
 #endif
 return c;}
 float4 frag(v2f i):SV_Target {float4 c=tex2D(_MainTex,i.uv);
 // Swap only the authored grass colours. Rock outlines, shading and alpha stay intact.
 if(distance(c.rgb,palette(float3(62,137,72)/255.0))<0.015)c.rgb=palette(float3(148,243,244)/255.0);
 else if(distance(c.rgb,palette(float3(38,92,66)/255.0))<0.015)c.rgb=palette(float3(37,205,208)/255.0);
 else if(distance(c.rgb,palette(float3(38,84,44)/255.0))<0.015)c.rgb=palette(float3(29,103,104)/255.0);
 return c*i.color;}
 ENDHLSL
 }
 }
}
