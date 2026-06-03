// oldscripter@gmail.com

using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

[BurstCompile]
public partial struct SpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Iterate all spawners:
        foreach (var (spawner, transform) in 
                 SystemAPI.Query<RefRW<Spawner>, RefRO<LocalTransform>>())
        {
            // If still not created:
            if (spawner.ValueRO.UnitsSpawned < spawner.ValueRO.UnitsToSpawn)
            {
                // Check next spawn time:
                if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
                {
                    // Get position on grid:
                    int spawned = spawner.ValueRO.UnitsSpawned;
                    int gridSize = spawner.ValueRO.GridSize;
                    float spacing = spawner.ValueRO.Spacing;
                    
                    int x = spawned % gridSize;
                    int z = spawned / gridSize;
                    
                    float startX = -gridSize * spacing / 2;
                    float startZ = -gridSize * spacing / 2;
                    
                    float3 unitPosition = new float3(
                        startX + x * spacing,
                        0.0f,
                        startZ + z * spacing
                    );
                    
                    // New unit creation:
                    Entity newUnit = state.EntityManager.Instantiate(spawner.ValueRO.PrefabEntity);
                    
                    // Set possition:
                    state.EntityManager.SetComponentData(newUnit, LocalTransform.FromPosition(unitPosition));
                    
                    // Update counter & time to next spawn:
                    spawner.ValueRW.UnitsSpawned++;
                    spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;
                }
            }
        }
    }
}