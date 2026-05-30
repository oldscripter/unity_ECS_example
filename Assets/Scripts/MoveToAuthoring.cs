// oldsripter@gmail.com

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Data for ECS
public struct MoveTo : IComponentData
{
    public float3 TargetPosition;   // Target position
    public float MoveSpeed;         // Speed
    public float StoppingDistance;  // Stop distance
    public bool IsMoving;           // Is moving right now
}

public class MoveToAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float StoppingDistance = 0.5f;
    
    public class Baker : Baker<MoveToAuthoring>
    {
        public override void Bake(MoveToAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MoveTo
            {
                TargetPosition = float3.zero,
                MoveSpeed = authoring.MoveSpeed,
                StoppingDistance = authoring.StoppingDistance,
                IsMoving = false
            });
        }
    }
}