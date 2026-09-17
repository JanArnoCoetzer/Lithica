using UnityEngine;

public static class TerrainSamplingHelper
{
    public static Vector2 DamagePixelToWorldCenter(WorldManager world, int px, int py)
    {
        float pixelWorldSize = world.cellSize / world.TilePixelSize;

        return new Vector2(
            world.transform.position.x + (px + 0.5f) * pixelWorldSize,
            world.transform.position.y + (py + 0.5f) * pixelWorldSize
        );
    }

    public static float SampleDensityAtWorld(WorldManager world, Vector2 worldPos)
    {
        float localX = (worldPos.x - world.transform.position.x) / world.cellSize;
        float localY = (worldPos.y - world.transform.position.y) / world.cellSize;

        int x0 = Mathf.FloorToInt(localX);
        int y0 = Mathf.FloorToInt(localY);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        float tx = localX - x0;
        float ty = localY - y0;

        x0 = Mathf.Clamp(x0, 0, world.TotalCellsX);
        y0 = Mathf.Clamp(y0, 0, world.TotalCellsY);
        x1 = Mathf.Clamp(x1, 0, world.TotalCellsX);
        y1 = Mathf.Clamp(y1, 0, world.TotalCellsY);

        float bl = world.GetDensityPoint(x0, y0);
        float br = world.GetDensityPoint(x1, y0);
        float tl = world.GetDensityPoint(x0, y1);
        float tr = world.GetDensityPoint(x1, y1);

        float bottom = Mathf.Lerp(bl, br, tx);
        float top = Mathf.Lerp(tl, tr, tx);
        return Mathf.Lerp(bottom, top, ty);
    }

    public static Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        Vector2 rotated = new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );

        return rotated.normalized;
    }
}