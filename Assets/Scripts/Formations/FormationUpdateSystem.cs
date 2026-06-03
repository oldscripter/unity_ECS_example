using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using ECS_Formation;

[UpdateBefore(typeof(MoveSystem))]
public partial struct FormationUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Собираем все активные формации
        var formations = new NativeList<FormationGroupData>(Allocator.Temp);
        
        foreach (var formation in SystemAPI.Query<RefRO<FormationGroupData>>())
        {
            if (formation.ValueRO.IsMoving)
            {
                formations.Add(formation.ValueRO);
            }
        }
        
        if (formations.Length == 0)
        {
            formations.Dispose();
            return;
        }
        
        // Для каждой формации обновляем позиции юнитов и возвращаем сбившихся
        foreach (var formation in formations)
        {
            // Вычисляем направления формации
            float3 forward = math.normalizesafe(formation.FormationForward);
            float3 right = math.normalizesafe(math.cross(new float3(0, 1, 0), forward));
            
            if (math.length(forward) < 0.01f)
            {
                forward = new float3(1, 0, 0);
                right = new float3(0, 0, 1);
            }
            
            float halfWidth = (formation.MaxUnitsPerRow - 1) * formation.Spacing / 2f;
            
            // Обновляем цели и возвращаем сбившихся юнитов
            foreach (var (member, transform, moveTo) in 
                     SystemAPI.Query<RefRO<FormationMember>, RefRW<LocalTransform>, RefRW<MoveTo>>())
            {
                if (member.ValueRO.FormationID != formation.FormationID) continue;
                
                // Вычисляем идеальную позицию в формации
                float offsetX = (member.ValueRO.ColIndex * formation.Spacing) - halfWidth;
                float offsetZ = member.ValueRO.RowIndex * formation.Spacing;
                float3 offset = (right * offsetX) + (forward * offsetZ);
                float3 idealPosition = formation.TargetCenter + offset;
                
                // Всегда обновляем цель движения (если формация движется)
                moveTo.ValueRW.TargetPosition = idealPosition;
                moveTo.ValueRW.IsMoving = true;
                
                // Возвращаем сбившегося юнита на позицию (если его толкнули)
                float3 currentPos = transform.ValueRO.Position;
                float3 deviation = idealPosition - currentPos;
                float deviationDistance = math.length(deviation);
                
                // Если юнит отклонился от идеальной позиции - возвращаем
                if (deviationDistance > 0.01f && deviationDistance < 1.5f)
                {
                    // Используем квадратичную интерполяцию для плавности
                    float delta = SystemAPI.Time.DeltaTime;
                    
                    // Плавное ускорение при большом отклонении
                    float correctionSpeed = 3f * delta;

                    // Ограничиваем максимальную коррекцию
                    float maxCorrection = 1.5f * delta;
                    float3 correction = deviation * math.min(correctionSpeed, maxCorrection);
                    
                    transform.ValueRW.Position += correction;
                }
            }
        }
        
        formations.Dispose();
    }
}