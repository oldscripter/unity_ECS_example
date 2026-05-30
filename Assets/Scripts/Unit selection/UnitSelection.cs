using Unity.Entities;
using UnityEngine;

// Компонент для хранения состояния выделения
public struct UnitSelection : IComponentData
{
    public bool IsSelected;
}

// Авторинг для добавления компонента на юнитов
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