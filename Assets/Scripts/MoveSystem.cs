using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

[BurstCompile]
public partial struct MoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, moveTo, velocity) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveTo>, RefRW<PhysicsVelocity>>())
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
                velocity.ValueRW.Linear = float3.zero;
                continue;
            }
            
            float3 moveDirection = math.normalize(direction);
            velocity.ValueRW.Linear = moveDirection * moveTo.ValueRO.MoveSpeed;
        }
    }
}