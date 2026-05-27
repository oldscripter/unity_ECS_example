using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

public partial struct RotationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var (transform, speed) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RotationSpeed>())
        {
            float rotation = speed.RadiansPerSecond * deltaTime;
            transform.ValueRW = transform.ValueRO.RotateY(rotation);
        }
    }
}