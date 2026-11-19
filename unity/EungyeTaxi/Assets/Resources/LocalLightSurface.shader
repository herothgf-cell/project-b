Shader "Taxi/Local Light Surface" {
 Properties {
  _MainTex ("Opacity", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  ZWrite Off
  Cull Off
  Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   fixed4 _Color;
   struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
   struct Varying { float4 clipPosition : SV_POSITION; float2 uv : TEXCOORD0; };
   Varying vert(Input input) {
    Varying result;
    result.clipPosition = UnityObjectToClipPos(input.vertex);
    result.uv = input.uv;
    return result;
   }
   fixed4 frag(Varying input) : SV_Target {
    return tex2D(_MainTex,input.uv)*_Color;
   }
   ENDCG
  }
 }
}
