using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

public class UnitAuthoring : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.5f;
    
    [Header("Visual")]
    public float rotationSpeed = 2f;
    
    public class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Добавляем все компоненты юнита
            AddComponent(entity, new MoveTo
            {
                TargetPosition = float3.zero,
                MoveSpeed = authoring.moveSpeed,
                StoppingDistance = authoring.stoppingDistance,
                IsMoving = false
            });
            
            AddComponent(entity, new RotationSpeed
            {
                RadiansPerSecond = authoring.rotationSpeed
            });
            
            // AddComponent(entity, new UnitSelection
            // {
            //     IsSelected = false,
            //     SelectionTime = 0f
            // });
            
            AddComponent<UnitTag>(entity);
        }
    }
}