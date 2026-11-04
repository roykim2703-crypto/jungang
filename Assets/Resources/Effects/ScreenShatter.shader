Shader "AugmentedSix/ScreenShatter"
{
    Properties { [PerRendererData] _MainTex ("Screenshot", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float4 color:COLOR; float4 uv:TEXCOORD0; };
            struct Output { float4 vertex:SV_POSITION; fixed4 color:COLOR; float3 uv:TEXCOORD0; };
            sampler2D _MainTex;
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv.xyz;return o; }
            fixed4 frag(Output i):SV_Target
            {
                fixed4 sample=tex2D(_MainTex,i.uv.xy);
                fixed gray=dot(sample.rgb,fixed3(.299,.587,.114));
                fixed3 tint=lerp(fixed3(gray,gray,gray),fixed3(1,1,1),i.uv.z);
                return fixed4(tint*i.color.rgb,i.color.a);
            }
            ENDCG
        }
    }
}
