using UnityEngine;
using Unity.Mathematics;

/// <summary>
/// A little helper for mathfx class.
/// </summary>
public static class MathfxHelper
{
    #region [Mathfx]

    /// <summary>
    /// Return value based on curve from Mathfx class.
    /// </summary>
    /// <returns>The value.</returns>
    /// <param name="animationCurve">Animation curve.</param>
    /// <param name="start">Start.</param>
    /// <param name="end">End.</param>
    /// <param name="t">T.</param>
    public static float CurvedValue(AnimationCurveEnum animationCurve, float start, float end, float t)
    {
        switch (animationCurve)
        {
            case AnimationCurveEnum.Hermite:
                return Mathfx.Hermite(start, end, t);
            case AnimationCurveEnum.Sinerp:
                return Mathfx.Sinerp(start, end, t);
            case AnimationCurveEnum.Coserp:
                return Mathfx.Coserp(start, end, t);
            case AnimationCurveEnum.Berp:
                return Mathfx.Berp(start, end, t);
            case AnimationCurveEnum.Bounce:
                return start + ((end - start) * Mathfx.Bounce(t));
            case AnimationCurveEnum.Lerp:
                return Mathfx.Lerp(start, end, t);
            case AnimationCurveEnum.Clerp:
                return Mathfx.Clerp(start, end, t);
            default:
                return 0;
        }
    }

    /// <summary>
    /// Return value based on curve from Mathfx class.
    /// </summary>
    /// <returns>The value.</returns>
    /// <param name="animationCurve">Animation curve.</param>
    /// <param name="start">Start.</param>
    /// <param name="end">End.</param>
    /// <param name="t">T.</param>
    public static Vector2 CurvedValue(AnimationCurveEnum animationCurve, Vector2 start, Vector2 end, float t)
    {
        return new Vector2(CurvedValue(animationCurve, start.x, end.x, t), CurvedValue(animationCurve, start.y, end.y, t));
    }

    /// <summary>
    /// Return value based on curve from Mathfx class.
    /// </summary>
    /// <returns>The value.</returns>
    /// <param name="animationCurve">Animation curve.</param>
    /// <param name="start">Start.</param>
    /// <param name="end">End.</param>
    /// <param name="t">T.</param>
    public static Vector3 CurvedValue(AnimationCurveEnum animationCurve, Vector3 start, Vector3 end, float t)
    {
        return new Vector3(CurvedValue(animationCurve, start.x, end.x, t), CurvedValue(animationCurve, start.y, end.y, t), CurvedValue(animationCurve, start.z, end.z, t));
    }

    #endregion

    #region [MathfxECS]

    /// <summary>
    /// Return value based on curve from MathfxECS class.
    /// </summary>
    /// <returns>The value.</returns>
    /// <param name="animationCurve">Animation curve.</param>
    /// <param name="start">Start.</param>
    /// <param name="end">End.</param>
    /// <param name="t">T.</param>
    public static float CurvedValueECS(AnimationCurveEnum animationCurve, float start, float end, float t)
    {
        switch (animationCurve)
        {
            case AnimationCurveEnum.Hermite:
                return MathfxECS.Hermite(start, end, t);
            case AnimationCurveEnum.Sinerp:
                return MathfxECS.Sinerp(start, end, t);
            case AnimationCurveEnum.Coserp:
                return MathfxECS.Coserp(start, end, t);
            case AnimationCurveEnum.Berp:
                return MathfxECS.Berp(start, end, t);
            case AnimationCurveEnum.Bounce:
                return start + ((end - start) * MathfxECS.Bounce(t));
            case AnimationCurveEnum.Lerp:
                return MathfxECS.Lerp(start, end, t);
            case AnimationCurveEnum.Clerp:
                return MathfxECS.Clerp(start, end, t);
            default:
                return 0;
        }
    }

    /// <summary>
    /// Return value based on curve from MathfxECS class.
    /// </summary>
    /// <returns>The value.</returns>
    /// <param name="animationCurve">Animation curve.</param>
    /// <param name="start">Start.</param>
    /// <param name="end">End.</param>
    /// <param name="t">T.</param>
    public static float2 CurvedValueECS(AnimationCurveEnum animationCurve, float2 start, float2 end, float t)
    {
        return new float2(CurvedValueECS(animationCurve, start.x, end.x, t), CurvedValueECS(animationCurve, start.y, end.y, t));
    }

    /// <summary>
    /// Return value based on curve from MathfxECS class.
    /// </summary>
    /// <returns>The value.</returns>
    /// <param name="animationCurve">Animation curve.</param>
    /// <param name="start">Start.</param>
    /// <param name="end">End.</param>
    /// <param name="t">T.</param>
    public static float3 CurvedValueECS(AnimationCurveEnum animationCurve, float3 start, float3 end, float t)
    {
        return new float3(CurvedValueECS(animationCurve, start.x, end.x, t), CurvedValueECS(animationCurve, start.y, end.y, t), CurvedValueECS(animationCurve, start.z, end.z, t));
    }

    #endregion
}
