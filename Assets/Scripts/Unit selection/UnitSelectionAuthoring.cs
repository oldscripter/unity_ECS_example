// oldscripter@gmail.com
using Unity.Entities;
using UnityEngine;

public struct UnitSelection : IComponentData
{
    public bool IsSelected;
}

public class UnitSelectionAuthoring : MonoBehaviour
{
    public class Baker : Baker<UnitSelectionAuthoring>
    {
        public override void Bake(UnitSelectionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitSelection { IsSelected = false });
        }
    }
}