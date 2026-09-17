using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct TerrainBreakJobResult
{
    public JobHandle Handle;
    public NativeArray<byte> HitMask;
    public NativeArray<int2> PixelCoords;
    public int Count;

    public bool IsCreated => HitMask.IsCreated && PixelCoords.IsCreated;

    public void Dispose()
    {
        if (HitMask.IsCreated) HitMask.Dispose();
        if (PixelCoords.IsCreated) PixelCoords.Dispose();
    }
}