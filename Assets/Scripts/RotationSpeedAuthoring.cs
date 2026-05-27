using Unity.Entities;
using UnityEngine;

public struct RotationSpeed : IComponentData
{
    public float RadiansPerSecond;
}

public class RotationSpeedAuthoring : MonoBehaviour
{
    public float RadiansPerSecond;
}

public class RotationSpeedBaker : Baker<RotationSpeedAuthoring>
{
    public override void Bake(RotationSpeedAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new RotationSpeed
        {
            RadiansPerSecond = authoring.RadiansPerSecond
        });
    }
}