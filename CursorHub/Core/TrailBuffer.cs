using System.Numerics;

namespace CursorHub.Core;

// Independent of the renderer. Fixed memory and bounded work, including teleports.
public sealed class TrailBuffer
{
    public struct Sample
    {
        public Vector2 Position;
        public double Born;
    }

    private readonly Sample[] samples;
    private int next;
    private Vector2? previous;
    private double previousTime;
    private float remainder;
    public int Capacity => samples.Length;
    public int Count { get; private set; }
    public Sample this[int index] => samples[(next - Count + index + Capacity) % Capacity];

    public TrailBuffer(int capacity = 256)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        samples = new Sample[capacity];
    }

    public void Clear()
    {
        Count = next = 0;
        previous = null;
        remainder = 0;
    }

    public void Advance(Vector2? position, double now, double lifetime, float spacing)
    {
        while (Count > 0 && now - this[0].Born >= lifetime) Count--;
        if (position is not { } current)
        {
            previous = null;
            remainder = 0;
            return;
        }
        if (previous is not { } last)
        {
            previous = current;
            previousTime = now;
            Add(current, now);
            return;
        }

        float distance = Vector2.Distance(last, current);
        float step = Math.Max(1, spacing);
        // Skip huge discontinuities instead of drawing across monitor transitions.
        if (distance > 2000)
        {
            Clear();
            previous = current;
            previousTime = now;
            Add(current, now);
            return;
        }
        if (distance > 0)
        {
            float offset = step - remainder;
            // At most Capacity samples emitted for one update.
            if ((distance - offset) / step >= Capacity)
                offset += MathF.Floor((distance - offset) / step - Capacity + 1) * step;
            for (float d = offset; d <= distance; d += step)
            {
                float t = d / distance;
                double born = previousTime + (now - previousTime) * t;
                if (now - born < lifetime) Add(Vector2.Lerp(last, current, t), born);
            }
            remainder = (remainder + distance) % step;
        }
        previous = current;
        previousTime = now;
    }

    private void Add(Vector2 point, double now)
    {
        samples[next] = new Sample { Position = point, Born = now };
        next = (next + 1) % Capacity;
        Count = Math.Min(Count + 1, Capacity);
    }
}
