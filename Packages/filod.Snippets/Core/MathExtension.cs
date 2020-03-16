using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

public static class MathExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Angle(float3 from, float3 to)
    {
        return math.degrees(math.acos(math.dot(math.normalize(from), math.normalize(to))));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static quaternion FromToRotation(quaternion from, quaternion to)
    {
        return math.mul(math.inverse(from), to);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float AngleSigned(float3 from, float3 to)
    {
        float angle = math.acos(math.dot(math.normalize(from), math.normalize(to)));
        float3 cross = math.cross(from, to);
        angle *= math.sign(math.dot(math.up(), cross));
        return math.degrees(angle);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
    {

        float sqrMag = math.dot(planeNormal, planeNormal);
        if (sqrMag < float.Epsilon)
            return vector;
        else
        {
            var dot = math.dot(vector, planeNormal);
            return new float3(vector.x - planeNormal.x * dot / sqrMag,
                vector.y - planeNormal.y * dot / sqrMag,
                vector.z - planeNormal.z * dot / sqrMag);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 ProjectOnNormal(float3 vector, float3 onNormal)
    {
        return onNormal * math.dot(vector, onNormal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 ClampMagnitude(float3 vector, float magnitude)
    {
        float lengthScale = math.length(vector) / magnitude;
        if (lengthScale > 1f)
        {
            vector = vector * (1f / lengthScale);
        }
        return vector;
    }

}
