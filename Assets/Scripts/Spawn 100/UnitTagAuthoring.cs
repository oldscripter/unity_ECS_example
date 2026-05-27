using Unity.Entities;
using UnityEngine;

// Пустой компонент-тег для идентификации юнитов
public struct UnitTag : IComponentData { }

public class UnitTagAuthoring : MonoBehaviour
{
    public class Baker : Baker<UnitTagAuthoring>
    {
        public override void Bake(UnitTagAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitTag());
        }
    }
}