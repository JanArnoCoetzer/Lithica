using System.Text;
using UnityEngine;

public struct BreakTiming
{
    private StringBuilder sb;
    private double start;
    private double last;
    private string label;

    public BreakTiming(string label)
    {
        this.label = label;
        sb = new StringBuilder(256);
        start = Time.realtimeSinceStartupAsDouble;
        last = start;
        sb.Append($"[{label}] ");
    }

    public void Step(string stepName)
    {
        double now = Time.realtimeSinceStartupAsDouble;
        double ms = (now - last) * 1000.0;
        last = now;
        sb.Append($"{stepName}={ms:F3}ms | ");
    }

    public void End(MonoBehaviour context = null)
    {
        double total = (Time.realtimeSinceStartupAsDouble - start) * 1000.0;
        sb.Append($"TOTAL={total:F3}ms");
        Debug.Log(sb.ToString(), context);
    }
}