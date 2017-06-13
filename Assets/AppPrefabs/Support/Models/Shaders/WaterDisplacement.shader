//Very fast shader that uses the Unity lighting model
//Compiles down to only performing the operations you're actually using
//Uses material property drawers rather than a custom editor for ease of maintainance

Shader "HoloToolkit/Water Displacement"
{
	Properties 
	{
		[Header(Main Color)]
		_Color ("Main Color", Color) = (0,1,1,1)
		[Space(20)]

		[Header(Rim Color)]
		_RimColor("Rim Color", Color) = (1,1,1,1)
		_RimPow("Rim Power", Float) = .7
		[Space(20)]
		
		
		[Header(Base (RGB))]
		_MainTex ("Base (RGB)", 2D) = "white" {}
		[Space(20)]

		[Header(DisplacementTexture)]
		_DispTex ("Displacement (RGB)", 2D) = "white" {}
		_DispAmount("Displacement Amount", Float) = .1
		_Offset("Offset", Vector) = (0,0,0,0)
		_colorHeightScaleFactor("Color Height Scale", Float) = 1
		[Space(20)]

		
		[Header(Blend State)]
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("SrcBlend", Float) = 1 //"One"
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("DestBlend", Float) = 0 //"Zero"
		[Space(20)]
		
		[Header(Other)]
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2 //"Back"
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4 //"LessEqual"
		[Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1.0 //"On"
		[Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorWriteMask ("ColorWriteMask", Float) = 15 //"All"
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		Blend [_SrcBlend] [_DstBlend]
		ZTest [_ZTest]
		ZWrite [_ZWrite]
		Cull [_Cull]
		ColorMask [_ColorWriteMask]
		LOD 300

		CGPROGRAM		
		//we only target the hololens (and the unity editor) so take advantage of shader model 5
		#pragma target 5.0
		#pragma only_renderers d3d11

		#pragma surface surf Lambert vertex:vert

		#include "HoloToolkitCommon.cginc"
        #include "WaterDisplacement.cginc"
        //#include "UnityCG.cginc"

		ENDCG  
	}
			Fallback "VertexLit"
}
