using Unity.Entities;
using Unity.Mathematics;

public struct Spawner : IComponentData
{
    public Entity PrefabEntity;      // Ссылка на префаб-сущность
    public float3 SpawnPosition;     // Позиция спавна
    public float NextSpawnTime;      // Когда спавнить следующего
    public float SpawnRate;          // Интервал между спавнами
    public int UnitsToSpawn;         // Сколько всего юнитов нужно создать
    public int UnitsSpawned;         // Сколько уже создано
    public float Spacing;             // Расстояние между юнитами в сетке
    public int GridSize;              // Размер сетки (sqrt от UnitsToSpawn)
}