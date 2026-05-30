// oldscripter@gmail.com

using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using UnityEngine;

public partial class SelectionSystem : SystemBase
{
    private Camera mainCamera;
    
    protected override void OnCreate()
    {
        Enabled = true;
        Debug.Log("Selection system is ready");Debug.Log("Selection system is ready");
    }
    
    protected override void OnUpdate()
    {
        if (mainCamera == null) 
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.Log("MainCamera not found");
                return;
            }
            Debug.Log("MainCamera not found");
        }
        
        // Click processing
        if (Input.GetMouseButtonUp(0) && !IsDragging())
        {
            HandleSingleClick();
        }
        
        // Rect selection processing
        if (Input.GetMouseButtonUp(0) && IsDragging())
        {
            Vector2 startPos = SelectionBoxUI.GetStartPosition();
            Vector2 endPos = SelectionBoxUI.GetEndPosition();
            
            if (startPos != endPos)
            {
                SelectUnitsInRectangle(startPos, endPos, Input.GetKey(KeyCode.LeftControl));
            }
        }
        
        ApplySelectionVisual();
    }
    
    private bool IsDragging()
    {
        Vector2 start = SelectionBoxUI.GetStartPosition();
        Vector2 end = SelectionBoxUI.GetEndPosition();
        return start != end;
    }
    
    private void HandleSingleClick()
    {
        // Raycast from camera to mouse position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if we click on unit
            bool clickedOnUnit = false;
            Entity clickedEntity = Entity.Null;
            
            Entities.ForEach((Entity entity, in LocalTransform transform, in UnitTag tag) =>
            {
                if (!clickedOnUnit)
                {
                    float distance = Vector3.Distance(transform.Position, hit.point);
                    if (distance < 0.5f) // If click close to unit
                    {
                        clickedOnUnit = true;
                        clickedEntity = entity;
                    }
                }
            }).WithoutBurst().Run();
            
            if (clickedOnUnit)
            {
                // Клик по юниту
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    // Ctrl + click: add/remove selected
                    ToggleUnitSelection(clickedEntity);
                }
                else
                {
                    // Click on object: unit selection
                    SelectSingleUnit(clickedEntity);
                }
            }
            else
            {
                // Void click should remove all selections
                if (!Input.GetKey(KeyCode.LeftControl))
                {
                    ClearAllSelection();
                }
            }
        }
        else
        {
            // Void click (not an object)
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                ClearAllSelection();
            }
        }
    }
    
    private void SelectSingleUnit(Entity targetEntity)
    {
        Entities.ForEach((Entity entity, ref UnitSelection selection) =>
        {
            selection.IsSelected = (entity == targetEntity);
        }).WithoutBurst().Run();
    }
    
    private void ToggleUnitSelection(Entity targetEntity)
    {
        Entities.ForEach((Entity entity, ref UnitSelection selection) =>
        {
            if (entity == targetEntity)
            {
                selection.IsSelected = !selection.IsSelected;
            }
        }).WithoutBurst().Run();
    }
    
    private void ClearAllSelection()
    {
        Entities.ForEach((ref UnitSelection selection) =>
        {
            selection.IsSelected = false;
        }).WithoutBurst().Run();
    }
    
    private void SelectUnitsInRectangle(Vector2 start, Vector2 end, bool additive)
    {
        if (mainCamera == null) return;
        
        Rect selectionRect = GetRect(start, end);
        int selectedCount = 0;
        
        if (!additive)
        {
            // Without Ctrl: first of all we should remove all selections
            Entities.ForEach((ref UnitSelection selection) =>
            {
                selection.IsSelected = false;
            }).WithoutBurst().Run();
        }
        
        // Select units in rect
        Entities.ForEach((ref UnitSelection selection, in LocalTransform transform) =>
        {
            Vector2 screenPos = mainCamera.WorldToScreenPoint(transform.Position);
            
            if (selectionRect.Contains(screenPos))
            {
                selection.IsSelected = true;
                selectedCount++;
            }
            // If additive == true, units out of the rect NOT deselected
        }).WithoutBurst().Run();
        
        if (selectedCount > 0)
            Debug.Log($"Selected {selectedCount} units (additive: {additive})");
    }
    
    private void ApplySelectionVisual()
    {
        // Scale change
        Entities.ForEach((ref LocalTransform transform, in UnitSelection selection) =>
        {
            transform.Scale = selection.IsSelected ? 1.2f : 1f;
        }).WithoutBurst().Run();
        
        // Color change
        Entities.ForEach((ref URPMaterialPropertyBaseColor color, in UnitSelection selection) =>
        {
            if (selection.IsSelected)
            {
                color.Value = new float4(1f, 0.92f, 0.016f, 1f); // Жёлтый
            }
            else
            {
                color.Value = new float4(1f, 1f, 1f, 1f); // Белый
            }
        }).WithoutBurst().Run();
    }
    
    private Rect GetRect(Vector2 start, Vector2 end)
    {
        float x = Mathf.Min(start.x, end.x);
        float y = Mathf.Min(start.y, end.y);
        float width = Mathf.Abs(start.x - end.x);
        float height = Mathf.Abs(start.y - end.y);
        return new Rect(x, y, width, height);
    }
}