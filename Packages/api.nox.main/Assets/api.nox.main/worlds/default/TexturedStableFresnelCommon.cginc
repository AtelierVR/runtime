#ifndef STABLE_FRESNEL_COMMON
#define STABLE_FRESNEL_COMMON

half4 _EdgeColor;   // Color and alpha of the fresnel effect
half4 _Color;   // Color and alpha of the base of the object
half4 _EdgeData;    // Min, Max, Power, Blend values

sampler2D _MainTex;
float4 _MainTex_ST;

struct appdata_fresnel
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;

    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct fresnel_vertex
{
    float4 pos : SV_POSITION;
    float3 worldPos : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float2 uv : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

fresnel_vertex vert(appdata_fresnel v)
{
    fresnel_vertex o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    // Transform UV coordinates
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    
    // Transform vertex to clip space
    o.pos = UnityObjectToClipPos(v.vertex);
    
    // Transform position to world space
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    
    // Transform normal to world space with safety check
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    
    // Safety check for degenerate normals
    if (length(o.worldNormal) < 0.001)
    {
        o.worldNormal = float3(0, 1, 0); // Default to up vector
    }
    
    return o;
}

half4 fragEmpty(fresnel_vertex i) : COLOR
{
    return half4(0,0,0,1);
}

half4 fragRimShader(fresnel_vertex i) : COLOR
{
    // Safety check for degenerate normals
    if (length(i.worldNormal) < 0.001)
    {
        return _Color * tex2D(_MainTex, i.uv);
    }
    
    half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
    half3 normalizedNormal = normalize(i.worldNormal);
    
    // Safety check for NaN or invalid vectors
    if (any(isnan(worldViewDir)) || any(isnan(normalizedNormal)))
    {
        return _Color * tex2D(_MainTex, i.uv);
    }
    
    // Ensure valid dot product to avoid magenta error
    half dotProduct = saturate(dot(worldViewDir, normalizedNormal));
    
    // Safety check for invalid EdgeData parameters
    half edgeRange = max(0.001, abs(_EdgeData.y - _EdgeData.x));
    half rim = saturate(((1.0 - dotProduct) - _EdgeData.x) / edgeRange);
    
    // Clamp power values to avoid invalid calculations
    half clampedPower = clamp(_EdgeData.z, 0.01, 10.0);
    half processedRim = (3 + clampedPower) * pow(rim, clampedPower + 1) - (2 + clampedPower) * pow(rim, clampedPower + 2);
    
    // Safety check for texture sampling
    half4 texColor = tex2D(_MainTex, i.uv);
    if (any(isnan(texColor)) || any(isinf(texColor)))
    {
        texColor = half4(1, 1, 1, 1);
    }
    
    // Ensure blend factor is valid
    half blendFactor = saturate(_EdgeData.w);
    half4 finalColor = lerp(_Color, _EdgeColor, lerp(rim, processedRim, blendFactor)) * texColor;
    
    // Final safety checks
    if (any(isnan(finalColor)) || any(isinf(finalColor)))
    {
        return _Color * texColor;
    }
    
    // Ensure all components are in valid range
    finalColor = saturate(finalColor);
    
    return finalColor;
}

#endif // STABLE_FRESNEL_COMMON
