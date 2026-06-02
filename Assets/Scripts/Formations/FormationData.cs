using Unity.Entities;
using Unity.Mathematics;

public struct FormationData : IComponentData
{
    public float3 TargetCenter;      // Центр цели
    public float3 FormationForward;  // Зафиксированное направление
    public float3 FormationOffset;   // Смещение в формации
    public int GroupId;              // ID группы
}