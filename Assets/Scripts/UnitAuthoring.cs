// oldsripter@gmail.com

using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;

public class UnitAuthoring : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.5f;
    
    [Header("Visual")]
    public float rotationSpeed = 2f;
     public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Physics")]
    public float radius = 0.5f;
    public float mass = 1f;
    
    public class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Move
            AddComponent(entity, new MoveTo
            {
                TargetPosition = float3.zero,
                MoveSpeed = authoring.moveSpeed,
                StoppingDistance = authoring.stoppingDistance,
                IsMoving = false
            });
            
            AddComponent<UnitTag>(entity);

            // Color
            var colorComponent = new URPMaterialPropertyBaseColor
            {
                Value = new float4(authoring.normalColor.r, authoring.normalColor.g, 
                                   authoring.normalColor.b, authoring.normalColor.a)
            };
            AddComponent(entity, colorComponent);

            AddComponent(entity, new PhysicsCollider
            {
                Value = Unity.Physics.SphereCollider.Create(
                    new SphereGeometry { Center = float3.zero, Radius = authoring.radius },
                    new CollisionFilter { BelongsTo = 1u << 0, CollidesWith = 1u << 0 }
                )
            });
            
            AddComponent(entity, new PhysicsMass
            {
                InverseMass = 1f / authoring.mass,
                Transform = new RigidTransform(quaternion.identity, float3.zero)
            });
            
            AddComponent(entity, new PhysicsVelocity { Linear = float3.zero, Angular = float3.zero });
            AddComponent(entity, new PhysicsGravityFactor { Value = 0f });
        }
    }
}