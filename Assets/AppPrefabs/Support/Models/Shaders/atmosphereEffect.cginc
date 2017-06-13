#if _USEMAINTEX_ON
    UNITY_DECLARE_TEX2D(_MainTex);
#endif

#if _USECOLOR_ON		
    float4 _Color;
#endif

#if _USERIMCOLOR_ON		
	float4 _RimColor;
	float _RimPow;
#endif


struct Input
{
    //will get compiled out if not touched
    float2 uv_MainTex;


	#if _NEAR_PLANE_FADE_ON
		float fade;
	#endif

    #if _USERIMCOLOR_ON
		float3 viewDir;
	#endif

	#if _USEVERTEXCOLOR_ON
		float4 vertColor;
	#endif
};


void vert(inout appdata_full v, out Input o)
{
	UNITY_INITIALIZE_OUTPUT(Input, o);
	
	#if _NEAR_PLANE_FADE_ON
		o.fade = ComputeNearPlaneFadeLinear(v.vertex);
	#endif

    #if _USERIMCOLOR_ON
		o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
	#endif

	#if _USEVERTEXCOLOR_ON
		o.vertColor = v.color;
	#endif
}

void surf(Input IN, inout SurfaceOutput o)
{
    float4 c;

    #if _USEMAINTEX_ON
        c = UNITY_SAMPLE_TEX2D(_MainTex, IN.uv_MainTex);
    #else
        c = 1;
    #endif

    #if _USECOLOR_ON
        c *= _Color;
    #endif

	#if _USERIMCOLOR_ON
		o.Emission = _RimColor.rgb;
		half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
		o.Alpha = pow(rim, _RimPow);
		c.r = pow(rim, _RimPow);
	#endif

	#if _USEVERTEXCOLOR_ON
		c *= IN.vertColor;
	#endif

    o.Albedo = c.r;
	
	#if _NEAR_PLANE_FADE_ON
		o.Albedo.rgb *= IN.fade;
	#endif
			

}
