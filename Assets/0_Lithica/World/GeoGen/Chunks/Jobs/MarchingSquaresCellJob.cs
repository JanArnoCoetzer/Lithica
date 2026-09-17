using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MarchingSquaresCellJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> DensityField;
    [ReadOnly] public int TotalPointsX;
    [ReadOnly] public int CellsPerChunk;
    [ReadOnly] public int StartX;
    [ReadOnly] public int StartY;
    [ReadOnly] public float CellSize;
    [ReadOnly] public float IsoLevel;
    [ReadOnly] public float MeshZ;

    [WriteOnly] public NativeArray<int> Configs;
    [WriteOnly] public NativeArray<float3> PBL;
    [WriteOnly] public NativeArray<float3> PBR;
    [WriteOnly] public NativeArray<float3> PTR;
    [WriteOnly] public NativeArray<float3> PTL;
    [WriteOnly] public NativeArray<float3> EBottom;
    [WriteOnly] public NativeArray<float3> ERight;
    [WriteOnly] public NativeArray<float3> ETop;
    [WriteOnly] public NativeArray<float3> ELeft;

    public void Execute(int index)
    {
        int x = index % CellsPerChunk;
        int y = index / CellsPerChunk;

        int gx = StartX + x;
        int gy = StartY + y;

        float bl = GetDensity(gx, gy);
        float br = GetDensity(gx + 1, gy);
        float tr = GetDensity(gx + 1, gy + 1);
        float tl = GetDensity(gx, gy + 1);

        bool sBL = bl >= IsoLevel;
        bool sBR = br >= IsoLevel;
        bool sTR = tr >= IsoLevel;
        bool sTL = tl >= IsoLevel;

        int config = 0;
        if (sBL) config |= 1;
        if (sBR) config |= 2;
        if (sTR) config |= 4;
        if (sTL) config |= 8;

        float3 pbl = new float3(x * CellSize, y * CellSize, MeshZ);
        float3 pbr = new float3((x + 1) * CellSize, y * CellSize, MeshZ);
        float3 ptr = new float3((x + 1) * CellSize, (y + 1) * CellSize, MeshZ);
        float3 ptl = new float3(x * CellSize, (y + 1) * CellSize, MeshZ);

        Configs[index] = config;

        PBL[index] = pbl;
        PBR[index] = pbr;
        PTR[index] = ptr;
        PTL[index] = ptl;

        EBottom[index] = Interpolate(pbl, pbr, bl, br);
        ERight[index] = Interpolate(pbr, ptr, br, tr);
        ETop[index] = Interpolate(ptl, ptr, tl, tr);
        ELeft[index] = Interpolate(pbl, ptl, bl, tl);
    }

    private float GetDensity(int x, int y)
    {
        return DensityField[y * TotalPointsX + x];
    }

    private float3 Interpolate(float3 a, float3 b, float da, float db)
    {
        float aShift = da - IsoLevel;
        float bShift = db - IsoLevel;

        if (math.abs(bShift - aShift) < 0.0001f)
            return math.lerp(a, b, 0.5f);

        float t = (-aShift) / (bShift - aShift);
        t = math.clamp(t, 0f, 1f);
        return math.lerp(a, b, t);
    }
}