Shader "UI/RadialGradientShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("MainTexture", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 center : TEXCOORD1;
                float3 params : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 center : TEXCOORD1;
                float3 params : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Angle;
            fixed4 SampleSpriteTexture (float2 uv)
			{
				fixed4 color = tex2D (_MainTex, uv);

#if UNITY_TEXTURE_ALPHASPLIT_ALLOWED
				if (_AlphaSplitEnabled)
					color.a = tex2D (_AlphaTex, uv).r;
#endif //UNITY_TEXTURE_ALPHASPLIT_ALLOWED

				return color;
			}
            v2f vert (appdata v)
            {
                const float PI = 3.14159;
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float s = sin (2 * PI * (-v.params.z) /360);
                float c = cos (2 * PI * (-v.params.z) /360);
                float2x2 rotationMatrix = float2x2( c, -s, s, c);
                rotationMatrix *=0.5;
                rotationMatrix +=0.5;
                rotationMatrix = rotationMatrix * 2-1;
                o.uv.xy = mul (o.uv.xy - v.center.xy, rotationMatrix );

                o.params = v.params;
                o.center = v.center;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float x =  i.uv.x;
                float y =  i.uv.y;
                float r1 = i.params.x / 2;
                float r2 = i.params.y / 2;
                float2 uv = sqrt(x * x / r1 + y * y / r2);
                fixed4 col = SampleSpriteTexture (uv) * i.color; 
                return col;
            }
            ENDCG
        }
    }
}
