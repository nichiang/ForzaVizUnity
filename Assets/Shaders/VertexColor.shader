Shader "Custom/VertexColorUnlit" {

	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 100
		Cull Off

		CGPROGRAM
#pragma surface surf Flat noambient novertexlights noforwardadd

		half4 LightingFlat(SurfaceOutput s, half3 lightDir, half atten) {
		half4 c;
		c.rgb = s.Albedo;
		return c;
	}

	fixed4 _Color;

	struct Input {
		float4 color : COLOR;
	};

	void surf(Input IN, inout SurfaceOutput o) {
		fixed4 c = _Color;
		o.Albedo = c.rgb * IN.color.rgb;
	}
	ENDCG
	}
		Fallback off

}