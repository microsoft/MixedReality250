
    UNITY_DECLARE_TEX2D(_MainTex);
	sampler2D _DispTex;
	float _DispAmount;
	float4 _Offset;
	float4 _Color;	
	float4 _RimColor;
	float _RimPow;
	float _colorHeightScaleFactor;


struct Input
{
    //will get compiled out if not touched
    float2 uv_MainTex;
	float fade;
	float3 viewDir;
	float4 vertColor;

};


void vert(inout appdata_full v, out Input o)
{
	UNITY_INITIALIZE_OUTPUT(Input, o);
		float d = tex2Dlod(_DispTex, float4(v.texcoord1.x + _Offset.x, v.texcoord1.y + _Offset.y, 0, 0)).r * _DispAmount;
		d *= v.color.a;
		v.vertex.z += v.normal.z * d;
		v.vertex.xy += v.normal.xy * (d * .25);
		//o.fade = ComputeNearPlaneFadeLinear(v.vertex);
		o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
		o.vertColor = v.color + d / _colorHeightScaleFactor * float4(1,1,1,1);
}

void surf(Input IN, inout SurfaceOutput o)
{
    float4 c;
	c = 1;
        c = UNITY_SAMPLE_TEX2D(_MainTex, IN.uv_MainTex);
        c *= _Color;
		half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
		o.Emission = _RimColor.rgb * pow(rim, _RimPow);
		c *= IN.vertColor;
    o.Albedo = c.rgb;	
		//o.Albedo.rgb *= IN.fade;	
	o.Alpha = c.a;
}