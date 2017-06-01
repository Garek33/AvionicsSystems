// MOARdV/ColorNoise
//
// Textured noise shader for rentex
//----------------------------------------------------------------------------
// The MIT License (MIT)
//
// Copyright (c) 2016-2017 MOARdV
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
//----------------------------------------------------------------------------
Shader "MOARdV/ColorNoise"
{
	Properties
	{
		_MainTex ("Render Input", 2D) = "white" {}
		_AuxTex ("Secondary texture (noise)", 2D) = "white" {}
		_Gain ("_Gain", float) = 1.0
		_Blend ("_Blend", float) = 1.0
		_NoiseOffset ("_NoiseOffset", float) = 0.0
		_Opacity ("_Opacity", float) = 1.0
	}
	SubShader 
	{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha
		Pass
		{
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma target 3.0
				#include "UnityCG.cginc"

				UNITY_DECLARE_TEX2D(_MainTex);
				UNITY_DECLARE_TEX2D(_AuxTex);
				uniform float _Gain;
				uniform float _Blend;
				uniform float _NoiseOffset;
				uniform float _Opacity;

				float4 frag(v2f_img IN) : SV_TARGET
				{
					// Fetch color
					float2 uv = IN.uv;
					float4 color = UNITY_SAMPLE_TEX2D(_MainTex, uv);

					// Apply gain
					float gainBoost = max(0.0, _Gain - 1.0) * 0.15;
					color.r = saturate((color.r * _Gain) + gainBoost);
					color.g = saturate((color.g * _Gain) + gainBoost);
					color.b = saturate((color.b * _Gain) + gainBoost);
					
					// Fetch noise, including offset
					uv.y = frac(uv.y + _NoiseOffset);
					float4 noise = UNITY_SAMPLE_TEX2D(_AuxTex, uv);
					
					// Blend RGB
					color.r = lerp(noise.r, color.r, _Blend);
					color.g = lerp(noise.g, color.g, _Blend);
					color.b = lerp(noise.b, color.b, _Blend);
					color.a = saturate(_Opacity);
					
					return color;
				}
			ENDCG
		}
	}
}
