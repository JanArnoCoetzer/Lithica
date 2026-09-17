using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingSquaresChunk : MonoBehaviour
{
    private WorldManager world;
    private int chunkX;
    private int chunkY;
    private float meshZ;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private enum CellPoint
    {
        BL, BR, TR, TL,
        BOTTOM, RIGHT, TOP, LEFT
    }

    public void Initialize(WorldManager world, int chunkX, int chunkY, Material material, float meshZ)
    {
        this.world = world;
        this.chunkX = chunkX;
        this.chunkY = chunkY;
        this.meshZ = meshZ;

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (material != null)
            meshRenderer.sharedMaterial = material;

        mesh = new Mesh();
        mesh.name = $"MarchingSquares_{chunkX}_{chunkY}";
        meshFilter.sharedMesh = mesh;
    }

    public void BuildMesh(StepTiming timing)
    {
        if (world == null)
            return;

        int cells = world.cellsPerChunk * world.cellsPerChunk;
        timing.Step("calc cells");

        NativeArray<float> densityNative = world.GetDensityFieldNative(Allocator.TempJob, timing.Child("GetDensityFieldNative"));
        timing.Step("density native ready");

        NativeArray<int> configs = new NativeArray<int>(cells, Allocator.TempJob);
        NativeArray<float3> pbl = new NativeArray<float3>(cells, Allocator.TempJob);
        NativeArray<float3> pbr = new NativeArray<float3>(cells, Allocator.TempJob);
        NativeArray<float3> ptr = new NativeArray<float3>(cells, Allocator.TempJob);
        NativeArray<float3> ptl = new NativeArray<float3>(cells, Allocator.TempJob);
        NativeArray<float3> eBottom = new NativeArray<float3>(cells, Allocator.TempJob);
        NativeArray<float3> eRight = new NativeArray<float3>(cells, Allocator.TempJob);
        NativeArray<float3> eTop = new NativeArray<float3>(cells, Allocator.TempJob);
        NativeArray<float3> eLeft = new NativeArray<float3>(cells, Allocator.TempJob);
        timing.Step("alloc NativeArrays");

        int startX = chunkX * world.cellsPerChunk;
        int startY = chunkY * world.cellsPerChunk;

        MarchingSquaresCellJob job = new MarchingSquaresCellJob
        {
            DensityField = densityNative,
            TotalPointsX = world.TotalPointsX,
            CellsPerChunk = world.cellsPerChunk,
            StartX = startX,
            StartY = startY,
            CellSize = world.cellSize,
            IsoLevel = world.IsoLevel,
            MeshZ = meshZ,
            Configs = configs,
            PBL = pbl,
            PBR = pbr,
            PTR = ptr,
            PTL = ptl,
            EBottom = eBottom,
            ERight = eRight,
            ETop = eTop,
            ELeft = eLeft
        };

        JobHandle handle = job.Schedule(cells, 32);
        timing.Step("schedule job");

        handle.Complete();
        timing.Step("complete job");

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < cells; i++)
        {
            int config = configs[i];
            if (config == 0)
                continue;

            Vector3[] points =
            {
            pbl[i], pbr[i], ptr[i], ptl[i],
            eBottom[i], eRight[i], eTop[i], eLeft[i]
        };
            float bl = densityNative[(startY + (i / world.cellsPerChunk)) * world.TotalPointsX + (startX + (i % world.cellsPerChunk))];
            float br = densityNative[(startY + (i / world.cellsPerChunk)) * world.TotalPointsX + (startX + (i % world.cellsPerChunk) + 1)];
            float tr = densityNative[(startY + (i / world.cellsPerChunk) + 1) * world.TotalPointsX + (startX + (i % world.cellsPerChunk) + 1)];
            float tl = densityNative[(startY + (i / world.cellsPerChunk) + 1) * world.TotalPointsX + (startX + (i % world.cellsPerChunk))];

            switch (config)
            {
                case 1:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BL, CellPoint.BOTTOM, CellPoint.LEFT);
                    break;
                case 2:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BR, CellPoint.RIGHT, CellPoint.BOTTOM);
                    break;
                case 3:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BL, CellPoint.BR, CellPoint.RIGHT, CellPoint.LEFT);
                    break;
                case 4:
                    AddCasePolygon(vertices, triangles, points, CellPoint.TR, CellPoint.TOP, CellPoint.RIGHT);
                    break;
                case 5:
                    {
                        bool connectTLBR = ResolveAmbiguousAsymptotic(bl, br, tr, tl);

                        if (connectTLBR)
                        {
                            AddCasePolygon(vertices, triangles, points, CellPoint.BL, CellPoint.BOTTOM, CellPoint.LEFT);
                            AddCasePolygon(vertices, triangles, points, CellPoint.TR, CellPoint.TOP, CellPoint.RIGHT);
                        }
                        else
                        {
                            AddCasePolygon(vertices, triangles, points, CellPoint.BL, CellPoint.BOTTOM, CellPoint.RIGHT, CellPoint.TR, CellPoint.TOP, CellPoint.LEFT);
                        }
                        break;
                    }
                case 6:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BR, CellPoint.TR, CellPoint.TOP, CellPoint.BOTTOM);
                    break;
                case 7:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BL, CellPoint.BR, CellPoint.TR, CellPoint.TOP, CellPoint.LEFT);
                    break;
                case 8:
                    AddCasePolygon(vertices, triangles, points, CellPoint.TL, CellPoint.LEFT, CellPoint.TOP);
                    break;
                case 9:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BL, CellPoint.BOTTOM, CellPoint.TOP, CellPoint.TL);
                    break;
                case 10:
                    {
                        bool connectTLBR = ResolveAmbiguousAsymptotic(bl, br, tr, tl);

                        if (connectTLBR)
                        {
                            AddCasePolygon(vertices, triangles, points, CellPoint.BR, CellPoint.RIGHT, CellPoint.BOTTOM);
                            AddCasePolygon(vertices, triangles, points, CellPoint.TL, CellPoint.LEFT, CellPoint.TOP);
                        }
                        else
                        {
                            AddCasePolygon(vertices, triangles, points, CellPoint.BR, CellPoint.RIGHT, CellPoint.TOP, CellPoint.TL, CellPoint.LEFT, CellPoint.BOTTOM);
                        }
                        break;
                    }
                case 11:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BL, CellPoint.BR, CellPoint.RIGHT, CellPoint.TOP, CellPoint.TL);
                    break;
                case 12:
                    AddCasePolygon(vertices, triangles, points, CellPoint.TL, CellPoint.TR, CellPoint.RIGHT, CellPoint.LEFT);
                    break;
                case 13:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BL, CellPoint.BOTTOM, CellPoint.RIGHT, CellPoint.TR, CellPoint.TL);
                    break;
                case 14:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BR, CellPoint.TR, CellPoint.TL, CellPoint.LEFT, CellPoint.BOTTOM);
                    break;
                case 15:
                    AddCasePolygon(vertices, triangles, points, CellPoint.BL, CellPoint.BR, CellPoint.TR, CellPoint.TL);
                    break;
            }
        }

        timing.Step("build verts/tris");

        densityNative.Dispose();
        configs.Dispose();
        pbl.Dispose();
        pbr.Dispose();
        ptr.Dispose();
        ptl.Dispose();
        eBottom.Dispose();
        eRight.Dispose();
        eTop.Dispose();
        eLeft.Dispose();
        timing.Step("dispose NativeArrays");

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        timing.Step("apply mesh");
    }

    private Vector3 LocalPoint(int x, int y)
    {
        return new Vector3(x * world.cellSize, y * world.cellSize, meshZ);
    }

    private Vector3 Interpolate(Vector3 a, Vector3 b, float da, float db)
    {
        float aShift = da - world.IsoLevel;
        float bShift = db - world.IsoLevel;

        if (Mathf.Abs(bShift - aShift) < 0.0001f)
            return Vector3.Lerp(a, b, 0.5f);

        float t = (-aShift) / (bShift - aShift);
        t = Mathf.Clamp01(t);
        return Vector3.Lerp(a, b, t);
    }

    private bool ResolveAmbiguousAsymptotic(float bl, float br, float tr, float tl)
    {
        bl -= world.IsoLevel;
        br -= world.IsoLevel;
        tr -= world.IsoLevel;
        tl -= world.IsoLevel;

        float q = tl * br - bl * tr;

        if (Mathf.Approximately(q, 0f))
        {
            float center = (bl + br + tr + tl) * 0.25f;
            return center >= 0f;
        }

        return q > 0f;
    }

    private void AddCasePolygon(List<Vector3> verts, List<int> tris, Vector3[] points, params CellPoint[] outline)
    {
        if (outline == null || outline.Length < 3)
            return;

        int start = verts.Count;

        for (int i = 0; i < outline.Length; i++)
            verts.Add(points[(int)outline[i]]);

        if (!IsClockwise(verts, start, outline.Length))
            ReverseRange(verts, start, outline.Length);

        for (int i = 1; i < outline.Length - 1; i++)
        {
            tris.Add(start);
            tris.Add(start + i);
            tris.Add(start + i + 1);
        }
    }

    private bool IsClockwise(List<Vector3> verts, int start, int count)
    {
        float sum = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 a = verts[start + i];
            Vector3 b = verts[start + ((i + 1) % count)];
            sum += (b.x - a.x) * (b.y + a.y);
        }

        return sum > 0f;
    }

    private void ReverseRange(List<Vector3> verts, int start, int count)
    {
        int end = start + count - 1;
        while (start < end)
        {
            Vector3 temp = verts[start];
            verts[start] = verts[end];
            verts[end] = temp;
            start++;
            end--;
        }
    }

    private void OnDrawGizmos()
    {
        if (world == null || !world.drawChunkBounds)
            return;

        Gizmos.color = world.chunkLineColor;

        Vector3 worldMin = transform.position;
        Vector3 size = new Vector3(world.ChunkWorldSize, world.ChunkWorldSize, 0f);
        Vector3 center = worldMin + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);

        Gizmos.DrawWireCube(center, size);
    }
}