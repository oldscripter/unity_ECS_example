using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct MoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var (transform, moveTo) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<MoveTo>>())
        {
            // Если не движется — пропускаем
            if (!moveTo.ValueRO.IsMoving)
                continue;
            
            // Вычисляем направление к цели
            float3 direction = moveTo.ValueRO.TargetPosition - transform.ValueRO.Position;
            float distance = math.length(direction);
            
            // Проверяем, достигли ли цели
            if (distance <= moveTo.ValueRO.StoppingDistance)
            {
                // Останавливаемся
                moveTo.ValueRW.IsMoving = false;
                continue;
            }
            
            // Двигаемся к цели
            direction = math.normalize(direction);
            float3 newPosition = transform.ValueRO.Position + 
                                  direction * moveTo.ValueRO.MoveSpeed * deltaTime;
            
            // Обновляем позицию
            transform.ValueRW.Position = newPosition;
        }
    }
}