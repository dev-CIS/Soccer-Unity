// Bakes the game's red/blue mask textures (team crests, kit previews) into a
// plain colored texture that UI Toolkit can display. Mirrors the colour-mask
// maths of TeamLogoUIShader: red areas take the first colour, blue areas the second.
Shader "Hidden/CIS/MaskColorize" {
    Properties {
        _MainTex ("Mask", 2D) = "white" {}
        _FirstColor ("First Color", Color) = (1, 1, 1, 1)
        _SecondColor ("Second Color", Color) = (0, 0, 0, 1)
    }

    SubShader {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _FirstColor;
            fixed4 _SecondColor;

            fixed4 frag (v2f_img i) : SV_Target {
                float4 mask = tex2D(_MainTex, i.uv);

                float firstWeight = saturate(1 - distance(mask.rgb, float3(1, 0, 0)));
                float secondWeight = saturate(1 - distance(mask.rgb, float3(0, 0, 1)));

                float3 color = _FirstColor.rgb * firstWeight + _SecondColor.rgb * secondWeight;

                return float4(color, mask.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
