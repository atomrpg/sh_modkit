
#define NUM_THROUGH 10
float4 _ThroughObjects[NUM_THROUGH];

void GetWallThroughFactor_float(float3 WorldPos, out float factor)
{
#if !UNITY_PASS_SHADOWCASTER
    factor = 10;
    float aspectRatio = _ScreenParams.x / _ScreenParams.y;
    float4 clipPos = TransformWorldToHClip(WorldPos);

    [unroll]
    for(int i = 0; i < NUM_THROUGH; i++)
    {
        float4 throughObj = _ThroughObjects[i];

        float4 clipPos2 = TransformWorldToHClip(throughObj.xyz);

#if ISOMETRIC_MODE
        float2 d = clipPos.xy - clipPos2.xy;
        d.x *= aspectRatio;
        float objFactor = length(d);
        float r = (throughObj.w * 0.02) - (unity_OrthoParams.y * 0.01);
#else
        float2 d = (clipPos.xy / clipPos.w) - (clipPos2.xy / clipPos2.w);
        d.x *= aspectRatio;
        float objFactor = length(d);
        float r = (clipPos.xyz / clipPos.w).z * throughObj.w;
 #endif

        if(clipPos.z < clipPos2.z)
        {
            objFactor = 10;
        }
        else if(objFactor > r)
        {
            objFactor = (objFactor - r) * 50;
        }
        else
        {
            objFactor = 0;
        }

        factor = min(objFactor, factor); 
    }
#else
    factor = 10;
#endif
}
