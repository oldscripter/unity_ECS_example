using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                float3 clickPosition = new float3(hit.point.x, 0, hit.point.z);
                
                // Отправляем ВСЕХ юнитов с тегом UnitTag
                Entities.ForEach((ref MoveTo moveTo, in UnitTag unit) =>
                {
                    moveTo.TargetPosition = clickPosition;
                    moveTo.IsMoving = true;
                }).WithoutBurst().Run();
            }
        }
    }
}