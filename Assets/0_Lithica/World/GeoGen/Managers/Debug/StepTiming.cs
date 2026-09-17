using System.Text;
using UnityEngine;

public struct StepTiming
{
    private sealed class SharedState
    {
        public readonly StringBuilder Builder;
        public readonly double RootStart;
        public double Last;
        public readonly bool Enabled;
        public bool Completed;

        public SharedState(string label, bool enabled, int capacity)
        {
            Enabled = enabled;
            Completed = false;

            if (!enabled)
                return;

            Builder = new StringBuilder(capacity);
            RootStart = Time.realtimeSinceStartupAsDouble;
            Last = RootStart;
            Builder.Append($"[{label}] ");
        }
    }

    private SharedState state;

    public bool IsValid => state != null && state.Enabled && !state.Completed;

    public StepTiming(string label, bool enabled = true, int capacity = 512)
    {
        state = new SharedState(label, enabled, capacity);
    }

    private StepTiming(SharedState shared)
    {
        state = shared;
    }

    public StepTiming Child(string label = null)
    {
        if (!IsValid)
            return default;

        if (!string.IsNullOrWhiteSpace(label))
            state.Builder.Append($"<{label}> ");

        return new StepTiming(state);
    }

    public void Step(string stepName)
    {
        if (!IsValid)
            return;

        double now = Time.realtimeSinceStartupAsDouble;
        double ms = (now - state.Last) * 1000.0;
        state.Last = now;
        state.Builder.Append($"{stepName}={ms:F3}ms | ");
    }

    public void Mark(string text)
    {
        if (!IsValid)
            return;

        state.Builder.Append($"{text} | ");
    }

    public void End(MonoBehaviour context = null)
    {
        if (!IsValid)
            return;

        state.Completed = true;

        double total = (Time.realtimeSinceStartupAsDouble - state.RootStart) * 1000.0;
        state.Builder.Append($"TOTAL={total:F3}ms");
        Debug.Log(state.Builder.ToString(), context);
    }
}