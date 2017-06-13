//Very fast shader that uses the Unity lighting model
//Compiles down to only performing the operations you're actually using
//Uses material property drawers rather than a custom editor for ease of maintainance

Shader "HoloToolkit/atmosphereEffect"
{
	Properties {
	_RimColor ("Rim Color", Color) = (1,1,1,1)
	_RimPower ("Rim Power", Range(0.5,8.0)) = 1.0
	_AlphPower ("Alpha Rim Power", Range(0.0,8.0)) = 1.5
	_AlphaMin ("Alpha Minimum", Range(0.0,1.0)) = 0.5

	}
	SubShader {
	Tags { "Queue" = "Transparent" }

	CGPROGRAM
	#pragma surface surf Lambert alpha
	struct Input {
	float3 worldRefl;
	float3 viewDir;
	INTERNAL_DATA
	};
	sampler2D _MainTex;
	float4 _RimColor;
	float _RimPower;
	float _AlphPower;
	float _AlphaMin;
	void surf (Input IN, inout SurfaceOutput o) {
	o.Albedo = _RimColor.rgb;
	half rim = 1.0 - saturate(dot (normalize(IN.viewDir), o.Normal));
	o.Emission = _RimColor.rgb;
	o.Alpha = (pow (rim, _AlphPower)*(1-_AlphaMin))+_AlphaMin ;
	}
	ENDCG
	}
	Fallback "VertexLit"
}

