// using Unity.Entities;
// using Unity.Transforms;
// using Unity.Mathematics;
// using Unity.Collections;
// using ECS_Formation;

// [UpdateAfter(typeof(MoveSystem))]
// [UpdateAfter(typeof(CollisionResolutionSystem))] // Если есть система коллизий
// public partial struct FormationPositionTrackerSystem : ISystem
// {
//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         float correctionStrength = 0.2f; // Сила возврата на позицию (0.1 - медленно, 0.5 - быстро)
//         float maxCorrectionDistance = 1.5f; // Максимальное отклонение для коррекции
        
//         // Собираем все активные формации
//         var formations = new NativeList<FormationGroupData>(Allocator.Temp);
//         foreach (var formation in SystemAPI.Query<RefRO<FormationGroupData>>())
//         {
//             formations.Add(formation.ValueRO);
//         }
        
//         if (formations.Length == 0)
//         {
//             formations.Dispose();
//             return;
//         }
        
//         // Для каждой формации проверяем позиции юнитов
//         foreach (var formation in formations)
//         {
//             if (!formation.IsMoving) continue;
            
//             // Вычисляем направление формации
//             float3 forward = math.normalizesafe(formation.FormationForward);
//             float3 right = math.normalizesafe(math.cross(new float3(0, 1, 0), forward));
            
//             if (math.length(forward) < 0.01f)
//             {
//                 forward = new float3(1, 0, 0);
//                 right = new float3(0, 0, 1);
//             }
            
//             float halfWidth = (formation.MaxUnitsPerRow - 1) * formation.Spacing / 2f;
            
//             // Проверяем позиции всех юнитов в формации
//             foreach (var (member, transform) in 
//                      SystemAPI.Query<RefRO<FormationMember>, RefRW<LocalTransform>>())
//             {
//                 if (member.ValueRO.FormationID != formation.FormationID) continue;
                
//                 // Вычисляем идеальную позицию в формации
//                 float offsetX = (member.ValueRO.ColIndex * formation.Spacing) - halfWidth;
//                 float offsetZ = member.ValueRO.RowIndex * formation.Spacing;
//                 float3 offset = (right * offsetX) + (forward * offsetZ);
//                 float3 idealPosition = formation.TargetCenter + offset;
                
//                 // Проверяем отклонение
//                 float3 currentPos = transform.ValueRO.Position;
//                 float3 deviation = idealPosition - currentPos;
//                 float deviationDistance = math.length(deviation);
                
//                 // Если юнит отклонился слишком далеко, возвращаем его обратно
//                 if (deviationDistance > 0.2f) // Порог отклонения
//                 {
//                     // Ограничиваем максимальную коррекцию
//                     float3 correction = deviation * correctionStrength;
//                     if (math.length(correction) > maxCorrectionDistance)
//                     {
//                         correction = math.normalize(correction) * maxCorrectionDistance;
//                     }
                    
//                     transform.ValueRW.Position += correction;
                    
//                     // Если юнит двигался к цели, но его сдвинули, продолжаем движение
//                     if (SystemAPI.HasComponent<MoveTo>(transform.GetType()))
//                     {
//                         var moveTo = SystemAPI.GetComponentRW<MoveTo>(transform.GetType());
//                         if (moveTo.ValueRO.IsMoving)
//                         {
//                             // Перенаправляем к идеальной позиции
//                             moveTo.ValueRW.TargetPosition = idealPosition;
//                         }
//                     }
//                 }
//             }
//         }
        
//         formations.Dispose();
//     }
// }