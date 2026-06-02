using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

[BurstCompile]
public partial struct MoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var (transform, moveTo, velocity) in 
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<MoveTo>, RefRW<PhysicsVelocity>>().WithAll<UnitTag>())
        {
            if (!moveTo.ValueRO.IsMoving)
            {
                velocity.ValueRW.Linear = float3.zero;
                continue;
            }
            
            float3 direction = moveTo.ValueRO.TargetPosition - transform.ValueRO.Position;
            float distance = math.length(direction);
            
            if (distance <= moveTo.ValueRO.StoppingDistance)
            {
                moveTo.ValueRW.IsMoving = false;
                velocity.ValueRW.Linear = float3.zero;
                continue;
            }
            
            // Устанавливаем скорость (физика сама двигает юнита)
            float3 moveDirection = math.normalize(direction);
            velocity.ValueRW.Linear = moveDirection * moveTo.ValueRO.MoveSpeed;
        }
    }
}