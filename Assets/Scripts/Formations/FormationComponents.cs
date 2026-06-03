using Unity.Entities;
using Unity.Mathematics;

namespace ECS_Formation
{
    // Компонент для юнита - его позиция в формации
    public struct FormationMember : IComponentData
    {
        public int FormationID;      // ID группы формации
        public int RowIndex;         // Ряд в формации
        public int ColIndex;         // Колонка в формации
    }

    // Компонент для группы формации (создаётся отдельно)
    public struct FormationGroupData : IComponentData
    {
        public int FormationID;      // ID группы
        public float3 TargetCenter;  // Центр цели
        public float3 FormationForward; // Направление
        public int MaxUnitsPerRow;   // Юнитов в ряду
        public float Spacing;        // Расстояние между юнитами
        public int TotalUnits;       // Всего юнитов
        public bool IsMoving;        // Движется ли группа
    }
}