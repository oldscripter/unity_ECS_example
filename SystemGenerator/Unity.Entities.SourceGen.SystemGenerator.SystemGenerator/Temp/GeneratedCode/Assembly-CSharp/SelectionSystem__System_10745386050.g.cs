using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionSystem : SystemBase
{
    private Vector2 selectionStart;
    private bool isSelecting;
    private Camera mainCamera;
    
    protected override void OnCreate()
    {
        Enabled = true;
        mainCamera = Camera.main;
    }
    
    protected override void OnUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || mainCamera == null) return;
        
        // Начало выделения
        if (mouse.leftButton.wasPressedThisFrame)
        {
            selectionStart = mouse.position.ReadValue();
            isSelecting = true;
            ClearSelection();
        }
        
        // Конец выделения
        if (isSelecting && mouse.leftButton.wasReleasedThisFrame)
        {
            Vector2 selectionEnd = mouse.position.ReadValue();
            SelectUnitsInRectangle(selectionStart, selectionEnd);
            isSelecting = false;
        }
        
        // Визуальный эффект
        ApplySelectionVisual();
    }
    
    private void ClearSelection()
    {
        Entities.ForEach((ref UnitSelection selection) =>
        {
            selection.IsSelected = false;
        }).WithoutBurst().Run();
    }
    
    private void SelectUnitsInRectangle(Vector2 start, Vector2 end)
    {
        Rect selectionRect = GetScreenRect(start, end);
        int selectedCount = 0;
        
        Entities.ForEach((ref UnitSelection selection, in LocalTransform transform) =>
        {
            Vector2 screenPos = mainCamera.WorldToScreenPoint(transform.Position);
            
            if (selectionRect.Contains(screenPos))
            {
                selection.IsSelected = true;
                selectedCount++;
            }
            else
            {
                selection.IsSelected = false;
            }
        }).WithoutBurst().Run();
        
        if (selectedCount > 0)
        {
            Debug.Log($"Selected {selectedCount} units");
        }
    }
    
    private void ApplySelectionVisual()
    {
        Entities.ForEach((ref LocalTransform transform, in UnitSelection selection) =>
        {
            transform.Scale = selection.IsSelected ? 1.2f : 1f;
        }).WithoutBurst().Run();
    }
    
    private Rect GetScreenRect(Vector2 start, Vector2 end)
    {
        float x = Mathf.Min(start.x, end.x);
        float y = Mathf.Min(start.y, end.y);
        float width = Mathf.Abs(start.x - end.x);
        float height = Mathf.Abs(start.y - end.y);
        return new Rect(x, y, width, height);
    }
}