﻿using UnityEngine;
using System.Collections;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Authoring;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class PhysicsTerrain : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField] PhysicsCategoryTags belongsTo;
    [SerializeField] PhysicsCategoryTags collidesWith;
    [SerializeField] int groupIndex;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var terrain = GetComponent<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("No terrain found!");
            return;
        }

        CollisionFilter collisionFilter = new CollisionFilter
        {
            BelongsTo = belongsTo.Value,
            CollidesWith = collidesWith.Value,
            GroupIndex = groupIndex
        };

        dstManager.AddComponentData(entity,
            CreateTerrainCollider(terrain.terrainData, collisionFilter));
    }
    public static PhysicsCollider CreateTerrainCollider(TerrainData terrainData, CollisionFilter filter)
    {
        var physicsCollider = new PhysicsCollider();
        var size = new int2(terrainData.heightmapResolution, terrainData.heightmapResolution);
        var scale = terrainData.heightmapScale;

        var colliderHeights = new NativeArray<float>(terrainData.heightmapResolution * terrainData.heightmapResolution,
            Allocator.TempJob);

        var terrainHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution,
            terrainData.heightmapResolution);


        for (int j = 0; j < size.y; j++)
            for (int i = 0; i < size.x; i++)
            {
                var h = terrainHeights[i, j];
                colliderHeights[j + i * size.x] = h;
            }

        physicsCollider.Value = Unity.Physics.TerrainCollider.Create(colliderHeights, size, scale,
            Unity.Physics.TerrainCollider.CollisionMethod.Triangles, filter);

        colliderHeights.Dispose();

        return physicsCollider;
    }

}