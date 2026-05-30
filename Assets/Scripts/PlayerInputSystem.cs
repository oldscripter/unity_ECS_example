// oldsripter@gmail.com

using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerInputSystem : SystemBase
{
    private Camera mainCamera;
    
    protected override void OnCreate()
    {
        mainCamera = Camera.main;
    }
    
    protected override void OnUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            return;
        }
        
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                float3 clickPosition = new float3(hit.point.x, 0, hit.point.z);
                
                // Двигаем ТОЛЬКО выделенных юнитов
                Entities.ForEach((ref MoveTo moveTo, in UnitSelection selection) =>
                {
                    if (selection.IsSelected)
                    {
                        moveTo.TargetPosition = clickPosition;
                        moveTo.IsMoving = true;
                    }
                }).WithoutBurst().Run();
            }
        }
    }
}