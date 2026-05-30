// oldscripter@gmail.com

using Unity.Entities;
using UnityEngine;

// Empty component-tag for unit identification
public partial struct UnitTag : IComponentData { }

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