using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject UnitPrefab;     // Префаб юнита (перетащи сюда)
    public int UnitsToSpawn = 100;    // Количество юнитов
    public float Spacing = 1.5f;      // Расстояние между юнитами
    public float SpawnRate = 0.1f;    // Как быстро создавать
    
    public class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            // Получаем Entity для префаба юнита
            var prefabEntity = GetEntity(authoring.UnitPrefab, TransformUsageFlags.Dynamic);
            
            // Создаём Entity для спавнера
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Добавляем компонент Spawner с настройками
            AddComponent(entity, new Spawner
            {
                PrefabEntity = prefabEntity,
                SpawnPosition = authoring.transform.position,
                NextSpawnTime = 0f,
                SpawnRate = authoring.SpawnRate,
                UnitsToSpawn = authoring.UnitsToSpawn,
                UnitsSpawned = 0,
                Spacing = authoring.Spacing,
                GridSize = (int)math.sqrt(authoring.UnitsToSpawn)
            });
        }
    }
}