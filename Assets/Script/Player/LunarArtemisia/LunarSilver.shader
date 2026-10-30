Shader "Custom/LunarSilverSprite"
{
 Properties
 {
  [PerRendererData] _MainTex("Sprite Texture",2D)="white"{}
  _Color("Tint",Color)=(.87,.94,1,1)
  _LuminanceFloor("Silver shadow lift",Range(0,1))=0
  [MaterialToggle] PixelSnap("Pixel snap",Float)=0
  [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
  [PerRendererData] _AlphaTex("External Alpha",2D)="white"{}
  [PerRendererData] _EnableExternalAlpha("Enable External Alpha",Float)=0
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "PreviewType"="Plane"}
  Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex SpriteVert
   #pragma fragment SilverFrag
   #pragma target 2.0
   #pragma multi_compile_instancing
   #pragma multi_compile _ PIXELSNAP_ON
   #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
   #include "UnitySprites.cginc"
   fixed _LuminanceFloor;
   fixed4 SilverFrag(v2f IN):SV_Target
   {
    fixed4 c=SampleSpriteTexture(IN.texcoord);
    fixed gray=dot(c.rgb,fixed3(.299,.587,.114));
    gray=lerp(_LuminanceFloor,1,gray);
    c.rgb=gray.xxx;
    c*=IN.color;
    c.rgb*=c.a;
    return c;
   }
   ENDCG
  }
 }
}
