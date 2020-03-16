using Unity.Mathematics;

// Variation of Mathfx class for ECS.
// Original Source: http://wiki.unity3d.com/index.php/Mathfx
public sealed class MathfxECS
{
    //Ease in out
    public static float Hermite(float start, float end, float value)
    {
        return math.lerp(start, end, value * value * (3.0f - 2.0f * value));
    }

    public static float2 Hermite(float2 start, float2 end, float value)
    {
        return new float2(Hermite(start.x, end.x, value), Hermite(start.y, end.y, value));
    }

    public static float3 Hermite(float3 start, float3 end, float value)
    {
        return new float3(Hermite(start.x, end.x, value), Hermite(start.y, end.y, value), Hermite(start.z, end.z, value));
    }

    //Ease out
    public static float Sinerp(float start, float end, float value)
    {
        return math.lerp(start, end, math.sin(value * math.PI * 0.5f));
    }

    public static float2 Sinerp(float2 start, float2 end, float value)
    {
        return new float2(math.lerp(start.x, end.x, math.sin(value * math.PI * 0.5f)), math.lerp(start.y, end.y, math.sin(value * math.PI * 0.5f)));
    }

    public static float3 Sinerp(float3 start, float3 end, float value)
    {
        return new float3(math.lerp(start.x, end.x, math.sin(value * math.PI * 0.5f)), math.lerp(start.y, end.y, math.sin(value * math.PI * 0.5f)), math.lerp(start.z, end.z, math.sin(value * math.PI * 0.5f)));
    }
    //Ease in
    public static float Coserp(float start, float end, float value)
    {
        return math.lerp(start, end, 1.0f - math.cos(value * math.PI * 0.5f));
    }

    public static float2 Coserp(float2 start, float2 end, float value)
    {
        return new float2(Coserp(start.x, end.x, value), Coserp(start.y, end.y, value));
    }

    public static float3 Coserp(float3 start, float3 end, float value)
    {
        return new float3(Coserp(start.x, end.x, value), Coserp(start.y, end.y, value), Coserp(start.z, end.z, value));
    }

    //Boing
    public static float Berp(float start, float end, float value)
    {
        value = math.clamp(value, 0, 1);
        value = (math.sin(value * math.PI * (0.2f + 2.5f * value * value * value)) * math.pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
        return start + (end - start) * value;
    }

    public static float2 Berp(float2 start, float2 end, float value)
    {
        return new float2(Berp(start.x, end.x, value), Berp(start.y, end.y, value));
    }

    public static float3 Berp(float3 start, float3 end, float value)
    {
        return new float3(Berp(start.x, end.x, value), Berp(start.y, end.y, value), Berp(start.z, end.z, value));
    }

    //Like lerp with ease in ease out
    public static float SmoothStep(float x, float min, float max)
    {
        x = math.clamp(x, min, max);
        float v1 = (x - min) / (max - min);
        float v2 = (x - min) / (max - min);
        return -2 * v1 * v1 * v1 + 3 * v2 * v2;
    }

    public static float2 SmoothStep(float2 vec, float min, float max)
    {
        return new float2(SmoothStep(vec.x, min, max), SmoothStep(vec.y, min, max));
    }

    public static float3 SmoothStep(float3 vec, float min, float max)
    {
        return new float3(SmoothStep(vec.x, min, max), SmoothStep(vec.y, min, max), SmoothStep(vec.z, min, max));
    }

    public static float Lerp(float start, float end, float value)
    {
        return ((1.0f - value) * start) + (value * end);
    }

    public static float2 Lerp(float2 start, float2 end, float value)
    {
        return new float2(Lerp(start.x, end.x, value), Lerp(start.y, end.y, value));
    }

    public static float3 Lerp(float3 start, float3 end, float value)
    {
        return new float3(Lerp(start.x, end.x, value), Lerp(start.y, end.y, value), Lerp(start.z, end.z, value));
    }

    public static float3 NearestPoint(float3 lineStart, float3 lineEnd, float3 point)
    {
        float3 lineDirection = math.normalize(lineEnd - lineStart);
        float closestPoint = math.dot((point - lineStart), lineDirection);
        return lineStart + (closestPoint * lineDirection);
    }

    public static float3 NearestPointStrict(float3 lineStart, float3 lineEnd, float3 point)
    {
        float3 fullDirection = lineEnd - lineStart;
        float3 lineDirection = math.normalize(fullDirection);
        float closestPoint = math.dot((point - lineStart), lineDirection);
        return lineStart + (math.clamp(closestPoint, 0.0f, math.length(fullDirection)) * lineDirection);
    }

    //Bounce
    public static float Bounce(float x)
    {
        return math.abs(math.sin(6.28f * (x + 1f) * (x + 1f)) * (1f - x));
    }

    public static float2 Bounce(float2 vec)
    {
        return new float2(Bounce(vec.x), Bounce(vec.y));
    }

    public static float3 Bounce(float3 vec)
    {
        return new float3(Bounce(vec.x), Bounce(vec.y), Bounce(vec.z));
    }

    // test for value that is near specified float (due to floating point inprecision)
    // all thanks to Opless for this!
    public static bool Approx(float val, float about, float range)
    {
        return ((math.abs(val - about) < range));
    }

    // test if a float3 is close to another float3 (due to floating point inprecision)
    // compares the square of the distance to the square of the range as this 
    // avoids calculating a square root which is much slower than squaring the range
    public static bool Approx(float3 val, float3 about, float range)
    {
        return (math.lengthsq(val - about) < range * range);
    }

    /*
      * CLerp - Circular Lerp - is like lerp but handles the wraparound from 0 to 360.
      * This is useful when interpolating eulerAngles and the object
      * crosses the 0/360 boundary.  The standard Lerp function causes the object
      * to rotate in the wrong direction and looks stupid. Clerp fixes that.
      */
    public static float Clerp(float start, float end, float value)
    {
        float min = 0.0f;
        float max = 360.0f;
        float half = math.abs((max - min) / 2.0f);//half the distance between min and max
        float retval = 0.0f;
        float diff = 0.0f;

        if ((end - start) < -half)
        {
            diff = ((max - start) + end) * value;
            retval = start + diff;
        }
        else if ((end - start) > half)
        {
            diff = -((max - end) + start) * value;
            retval = start + diff;
        }
        else retval = start + (end - start) * value;

        // Debug.Log("Start: "  + start + "   End: " + end + "  Value: " + value + "  Half: " + half + "  Diff: " + diff + "  Retval: " + retval);
        return retval;
    }
}
