using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using Unity.Physics;
using Unity.Mathematics;
using Bounds = UnityEngine.Bounds;
using System;
using System.Collections.Generic;

public static unsafe class PhysicsUtils
{
    public enum CollisionFilterLayer : uint
    {
        Nothing = 0,
        StaticEnvironmental = (1 << 0),
        DynamicEnvironmental = (1 << 1),
        KinematicEnvironmental = (1 << 2),
        Characters = (1 << 3),
        Perceptible = (1 << 4),
        Projectile = (1 << 5),
        All = 0xffffffff
    }

    public static uint LayerMask(params CollisionFilterLayer[] layers)
    {
        uint mask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            mask |= (uint)layers[i];
        }
        return mask;
    }


    public static BlobAssetReference<Collider> CreateCollider(UnityEngine.Mesh mesh, ColliderType type)
    {
        switch (type)
        {
            case ColliderType.Sphere:
                {
                    Bounds bounds = mesh.bounds;
                    return SphereCollider.Create(new SphereGeometry
                    {
                        Center = bounds.center,
                        Radius = math.cmax(bounds.extents)
                    });
                }
            case ColliderType.Triangle:
                {
                    return PolygonCollider.CreateTriangle(mesh.vertices[0], mesh.vertices[1], mesh.vertices[2]);
                }
            case ColliderType.Quad:
                {
                    // We assume the first 2 triangles of the mesh are a quad with a shared edge
                    // Work out a correct ordering for the triangle
                    int[] orderedIndices = new int[4];

                    // Find the vertex in first triangle that is not on the shared edge
                    for (int i = 0; i < 3; i++)
                    {
                        if ((mesh.triangles[i] != mesh.triangles[3]) &&
                            (mesh.triangles[i] != mesh.triangles[4]) &&
                            (mesh.triangles[i] != mesh.triangles[5]))
                        {
                            // Push in order or prev, unique, next
                            orderedIndices[0] = mesh.triangles[(i - 1 + 3) % 3];
                            orderedIndices[1] = mesh.triangles[i];
                            orderedIndices[2] = mesh.triangles[(i + 1) % 3];
                            break;
                        }
                    }

                    // Find the vertex in second triangle that is not on a shared edge
                    for (int i = 3; i < 6; i++)
                    {
                        if ((mesh.triangles[i] != orderedIndices[0]) &&
                            (mesh.triangles[i] != orderedIndices[1]) &&
                            (mesh.triangles[i] != orderedIndices[2]))
                        {
                            orderedIndices[3] = mesh.triangles[i];
                            break;
                        }
                    }

                    return PolygonCollider.CreateQuad(
                        mesh.vertices[orderedIndices[0]],
                        mesh.vertices[orderedIndices[1]],
                        mesh.vertices[orderedIndices[2]],
                        mesh.vertices[orderedIndices[3]]);
                }
            case ColliderType.Box:
                {
                    Bounds bounds = mesh.bounds;
                    return BoxCollider.Create(new BoxGeometry
                    {
                        Center = bounds.center,
                        Orientation = quaternion.identity,
                        Size = 2.0f * bounds.extents,
                        BevelRadius = 0.0f
                    });
                }
            case ColliderType.Capsule:
                {
                    Bounds bounds = mesh.bounds;
                    float min = math.cmin(bounds.extents);
                    float max = math.cmax(bounds.extents);
                    int x = math.select(math.select(2, 1, min == bounds.extents.y), 0, min == bounds.extents.x);
                    int z = math.select(math.select(2, 1, max == bounds.extents.y), 0, max == bounds.extents.x);
                    int y = math.select(math.select(2, 1, (1 != x) && (1 != z)), 0, (0 != x) && (0 != z));
                    float radius = bounds.extents[y];
                    float3 vertex0 = bounds.center; vertex0[z] = -(max - radius);
                    float3 vertex1 = bounds.center; vertex1[z] = (max - radius);
                    return CapsuleCollider.Create(new CapsuleGeometry
                    {
                        Vertex0 = vertex0,
                        Vertex1 = vertex1,
                        Radius = radius
                    });
                }
            case ColliderType.Cylinder:
                // TODO: need someone to add
                throw new NotImplementedException();
            case ColliderType.Convex:
                {
                    NativeArray<float3> points = new NativeArray<float3>(mesh.vertices.Length, Allocator.TempJob);
                    for (int i = 0; i < mesh.vertices.Length; i++)
                    {
                        points[i] = mesh.vertices[i];
                    }
                    BlobAssetReference<Collider> collider = ConvexCollider.Create(points, default, CollisionFilter.Default);
                    points.Dispose();
                    return collider;
                }
            default:
                throw new System.NotImplementedException();
        }
    }

    public static unsafe Entity CreateJointEntity(
        BlobAssetReference<JointData> jointData,
        Entity entityA,
        Entity entityB,
        EntityManager entityManager,
        bool enableCollision = true)
    {
        var componentData = new PhysicsJoint
        {
            JointData = jointData,
            EntityA = entityA,
            EntityB = entityB,
            EnableCollision = enableCollision ? 1 : 0,
        };

        ComponentType[] componentTypes = new ComponentType[1];
        componentTypes[0] = typeof(PhysicsJoint);
        Entity jointEntity = entityManager.CreateEntity(componentTypes);
#if UNITY_EDITOR
        var nameEntityA = entityManager.GetName(entityA);
        var nameEntityB = entityB == Entity.Null ? "PhysicsWorld" : entityManager.GetName(entityB);
        entityManager.SetName(jointEntity, $"Joining {nameEntityA} + {nameEntityB}");
#endif

        if (!entityManager.HasComponent<PhysicsJoint>(jointEntity))
        {
            entityManager.AddComponentData(jointEntity, componentData);
        }
        else
        {
            entityManager.SetComponentData(jointEntity, componentData);
        }
        return jointEntity;
    }

    public struct DistanceHitComparer : IComparer<DistanceHit>
    {
        public int Compare(DistanceHit x, DistanceHit y)
        {
            return x.Distance.CompareTo(y.Distance);
        }
    }
}