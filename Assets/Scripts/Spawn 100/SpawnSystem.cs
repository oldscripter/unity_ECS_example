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
        // Проходим по всем спавнерам
        foreach (var (spawner, transform) in 
                 SystemAPI.Query<RefRW<Spawner>, RefRO<LocalTransform>>())
        {
            // Если ещё не всё создали
            if (spawner.ValueRO.UnitsSpawned < spawner.ValueRO.UnitsToSpawn)
            {
                // Проверяем время следующего спавна
                if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
                {
                    // Вычисляем позицию в сетке
                    int spawned = spawner.ValueRO.UnitsSpawned;
                    int gridSize = spawner.ValueRO.GridSize;
                    float spacing = spawner.ValueRO.Spacing;
                    
                    int x = spawned % gridSize;
                    int z = spawned / gridSize;
                    
                    float startX = -gridSize * spacing / 2;
                    float startZ = -gridSize * spacing / 2;
                    
                    float3 unitPosition = new float3(
                        startX + x * spacing,
                        0.5f,
                        startZ + z * spacing
                    );
                    
                    // Создаём нового юнита
                    Entity newUnit = state.EntityManager.Instantiate(spawner.ValueRO.PrefabEntity);
                    
                    // Устанавливаем позицию
                    state.EntityManager.SetComponentData(newUnit, LocalTransform.FromPosition(unitPosition));
                    
                    // Обновляем счётчик и время следующего спавна
                    spawner.ValueRW.UnitsSpawned++;
                    spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;
                }
            }
        }
    }
}