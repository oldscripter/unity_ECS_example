// oldsripter@gmail.com

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
            // Skip if not moving
            if (!moveTo.ValueRO.IsMoving)
                continue;
            
            // Calculate the direction
            float3 direction = moveTo.ValueRO.TargetPosition - transform.ValueRO.Position;
            float distance = math.length(direction);
            
            // Checj if we reach the target
            if (distance <= moveTo.ValueRO.StoppingDistance)
            {
                // Stopping
                moveTo.ValueRW.IsMoving = false;
                continue;
            }
            
            // Move to target
            direction = math.normalize(direction);
            float3 newPosition = transform.ValueRO.Position + 
                                  direction * moveTo.ValueRO.MoveSpeed * deltaTime;
            
            // Update position
            transform.ValueRW.Position = newPosition;
        }
    }
}