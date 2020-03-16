using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using System.Linq;

public enum RotationType
{
    none,
    all
    /*
    x,
    y,
    z
    */
}
public enum LoopType
{
    none,
    loop,
    pingPong,
    random,
    yoyo
}
public enum PathType
{
    Linear,
    CatmullRom
}


// TODO: managed type may cause performance issue.
class SplineMove : IComponentData
{
    //DynamicBuffer<float> a = new DynamicBuffer<float>();
    public float3[] controlPoints;
    public bool closedLoop;
    public float speed;
    public LoopType loopType;
    public PathType pathType;
    public int currentPoint;
    public AnimationCurveEnum easeBetweenSection;
}

public class SplineMoveAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public PathManager pathContainer;
    public bool closedLoop = false;
    public float speed = 5;
    public LoopType loopType = LoopType.none;
    public PathType pathType = PathType.CatmullRom;
	public RotationType waypointRotation = RotationType.none;
    public int startPoint = 0;
    public AnimationCurveEnum easeBetweenSection;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData<SplineMove>(entity, new SplineMove
        {
            controlPoints = pathContainer.GetPathPoints().Select(p => (float3)p).ToArray(),
            closedLoop = closedLoop,
            speed = speed,
            loopType = loopType,
            pathType = pathType,
            currentPoint = startPoint,
            easeBetweenSection = easeBetweenSection
        });
    }
}

class SplineMoveSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, SplineMove move, ref Translation translation) =>
        {
            if (EntityManager.HasComponent<MoveTo>(entity)) return;


            float3 p0, p1, m0, m1;

            bool closedLoopFinalPoint = (move.closedLoop && move.currentPoint == move.controlPoints.Length - 1);

            var endPoint = move.currentPoint + 1;
            if (closedLoopFinalPoint)
            {
                endPoint = 0;
            }
            if (endPoint >= move.controlPoints.Length) return;

            p0 = move.controlPoints[move.currentPoint];

            if (closedLoopFinalPoint)
            {
                p1 = move.controlPoints[0];
            }
            else
            {
                p1 = move.controlPoints[move.currentPoint + 1];
            }

            // m0
            if (move.currentPoint == 0) // Tangent M[k] = (P[k+1] - P[k-1]) / 2
            {
                if (move.closedLoop)
                {
                    m0 = p1 - move.controlPoints[move.controlPoints.Length - 1];
                }
                else
                {
                    m0 = p1 - p0;
                }
            }
            else
            {
                m0 = p1 - move.controlPoints[move.currentPoint - 1];
            }

            // m1
            if (move.closedLoop)
            {
                if (move.currentPoint == move.controlPoints.Length - 1) //Last point case
                {
                    m1 = move.controlPoints[(move.currentPoint + 2) % move.controlPoints.Length] - p0;
                }
                else if (move.currentPoint == 0) //First point case
                {
                    m1 = move.controlPoints[move.currentPoint + 2] - p0;
                }
                else
                {
                    m1 = move.controlPoints[(move.currentPoint + 2) % move.controlPoints.Length] - p0;
                }
            }
            else
            {
                if (move.currentPoint < move.controlPoints.Length - 2)
                {
                    m1 = move.controlPoints[(move.currentPoint + 2) % move.controlPoints.Length] - p0;
                }
                else
                {
                    m1 = p1 - p0;
                }
            }

            m0 *= 0.5f; //Doing this here instead of  in every single above statement
            m1 *= 0.5f;

            move.currentPoint = endPoint;

            EntityManager.AddComponentData(entity,
                new MoveTo
                {
                    AnimationCurve = move.easeBetweenSection,
                    StartValue = p0,
                    EndValue = p1,
                    PathType = move.pathType,
                    TangentPoint0 = m0,
                    TangentPoint1 = m1,
                    Duration = math.distance(p0, p1) / move.speed
                });
        });
    }
}