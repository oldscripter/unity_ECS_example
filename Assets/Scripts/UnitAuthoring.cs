// oldsripter@gmail.com

using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using Unity.Mathematics;

public class UnitAuthoring : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.5f;
    
    [Header("Visual")]
    public float rotationSpeed = 2f;
     public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    
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
            
            // Rotation
            AddComponent(entity, new RotationSpeed
            {
                RadiansPerSecond = authoring.rotationSpeed
            });
            
            AddComponent<UnitTag>(entity);

            // Color
            var colorComponent = new URPMaterialPropertyBaseColor
            {
                Value = new float4(authoring.normalColor.r, authoring.normalColor.g, 
                                   authoring.normalColor.b, authoring.normalColor.a)
            };
            AddComponent(entity, colorComponent);
        }
    }
}