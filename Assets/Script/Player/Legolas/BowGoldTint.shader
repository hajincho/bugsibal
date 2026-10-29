Shader "bugsibal/Bow Gold Tint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HDR] _TintColor ("Gold Hue", Color) = (1,0.78,0.1,1)
        _SrcBlend ("Source Blend", Float) = 5
        _DstBlend ("Destination Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend [_SrcBlend] [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            float4 _TintColor;
            fixed4 _RendererColor;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _RendererColor;
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                half4 texel = tex2D(_MainTex, i.uv) * i.color;
                half brightness = max(texel.r, max(texel.g, texel.b));
                half low = min(texel.r, min(texel.g, texel.b));
                half saturation = (brightness - low) / max(brightness, 0.0001h);
                half highlight = (1.0h - saturation) * (1.0h - saturation) * 0.35h;
                // Replace blue/cyan hues with gold while preserving value, alpha and pale highlights.
                half3 hue = lerp(_TintColor.rgb * brightness, brightness.xxx, highlight);
                return half4(hue, texel.a * _TintColor.a);
            }
            ENDCG
        }
    }
}
