Shader "PersonaCards/UI Brightness"
{
    Properties { [PerRendererData] _MainTex("Sprite Texture", 2D)="white" {} _Brightness("Brightness", Float)=1 }
    SubShader
    {
        Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend DstColor Zero
        ColorMask RGB
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION; };
            float _Brightness;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);return o;}
            float4 frag(v2f i):SV_Target{return float4(_Brightness,_Brightness,_Brightness,1);}
            ENDCG
        }
    }
}
