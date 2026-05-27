using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Данные движения для ECS
public struct MoveTo : IComponentData
{
    public float3 TargetPosition;   // Целевая позиция
    public float MoveSpeed;         // Скорость движения
    public float StoppingDistance;  // Дистанция остановки
    public bool IsMoving;           // Движется ли сейчас
}

// Авторинг для настройки в редакторе
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