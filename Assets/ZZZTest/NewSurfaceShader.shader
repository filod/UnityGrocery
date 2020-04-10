// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/NewSurfaceShader"
{
	Properties
	{
		_Color("Color Tint", Color) = (1, 1, 1, 1)
		_MainTex("Main Tex", 2D) = "white" {}
		_Specular("Specular", Color) = (1, 1, 1, 1)
		_Gloss("Gloss", Range(8.0, 256)) = 20
	}
		SubShader
	{
			Tags {
				"RenderType" = "Opaque"
				"RenderPipeline" = "UniversalPipeline"
			}
			Pass {
					CGPROGRAM

					#pragma vertex vert
					#pragma fragment frag

			#include "Lighting.cginc"

			fixed4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Specular;
			float _Gloss;

					struct a2v {
							float4 vertex : POSITION;
							float3 normal : NORMAL;
							float4 texcoord : TEXCOORD0;
					};

					struct v2f {
						float4 pos : SV_POSITION;
						float3 worldNormal : TEXCOORD0;
						float3 worldPos : TEXCOORD1;
						float2 uv : TEXCOORD2;
					};

					v2f vert(a2v v) {
						v2f o;
						// Transform the vertex from object space to projection space
						o.pos = UnityObjectToClipPos(v.vertex);

						// Transform the normal from object space to world space
						o.worldNormal = mul(v.normal, (float3x3)unity_WorldToObject);
						// Transform the vertex from object spacet to world space
						o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

						o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

						return o;
					}

					fixed4 frag(v2f i) : SV_Target{
						fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;

						fixed3 worldNormal = normalize(i.worldNormal);
						fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz);

						fixed3 albedo = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
						fixed3 diffuse = _LightColor0.rgb * albedo * (0.5 * (dot(worldNormal, worldLightDir)) + 0.5);
						fixed3 reflectDir = normalize(reflect(-worldLightDir, worldNormal));
						fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);

						fixed3 halfDir = normalize(worldLightDir + viewDir);
						fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(saturate(dot(reflectDir, halfDir)), _Gloss);


						fixed3 color = ambient + diffuse + specular;

						return fixed4(color, 1.0);
					}

					ENDCG
			}
		//Pass {
		//	// Pass for other pixel lights
		//	Tags { "LightMode" = "ForwardAdd" }

		//	Blend One One

		//	CGPROGRAM

		//	// Apparently need to add this declaration
		//	#pragma multi_compile_fwdadd

		//	#pragma vertex vert
		//	#pragma fragment frag

		//	#include "Lighting.cginc"
		//	#include "AutoLight.cginc"

		//	fixed4 _Diffuse;
		//	fixed4 _Specular;
		//	float _Gloss;

		//	struct a2v {
		//		float4 vertex : POSITION;
		//		float3 normal : NORMAL;
		//	};

		//	struct v2f {
		//		float4 pos : SV_POSITION;
		//		float3 worldNormal : TEXCOORD0;
		//		float3 worldPos : TEXCOORD1;
		//	};

		//	v2f vert(a2v v) {
		//		v2f o;
		//		o.pos = UnityObjectToClipPos(v.vertex);

		//		o.worldNormal = UnityObjectToWorldNormal(v.normal);

		//		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

		//		return o;
		//	}

		//	fixed4 frag(v2f i) : SV_Target {
		//		fixed3 worldNormal = normalize(i.worldNormal);
		//		#ifdef USING_DIRECTIONAL_LIGHT
		//			fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz);
		//		#else
		//			fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos.xyz);
		//		#endif

		//		fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * max(0, dot(worldNormal, worldLightDir));

		//		fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
		//		fixed3 halfDir = normalize(worldLightDir + viewDir);
		//		fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(worldNormal, halfDir)), _Gloss);

		//		#ifdef USING_DIRECTIONAL_LIGHT
		//			fixed atten = 1.0;
		//		#else
		//			#if defined (POINT)
		//						float3 lightCoord = mul(unity_WorldToLight, float4(i.worldPos, 1)).xyz;
		//						fixed atten = tex2D(_LightTexture0, dot(lightCoord, lightCoord).rr).UNITY_ATTEN_CHANNEL;
		//				#elif defined (SPOT)
		//						float4 lightCoord = mul(unity_WorldToLight, float4(i.worldPos, 1));
		//						fixed atten = (lightCoord.z > 0) * tex2D(_LightTexture0, lightCoord.xy / lightCoord.w + 0.5).w * tex2D(_LightTextureB0, dot(lightCoord, lightCoord).rr).UNITY_ATTEN_CHANNEL;
		//				#else
		//						fixed atten = 1.0;
		//				#endif
		//		#endif

		//		return fixed4((diffuse + specular) * atten, 1.0);
		//	}

		//	ENDCG
		//}
	}
		FallBack "Diffuse"
}
