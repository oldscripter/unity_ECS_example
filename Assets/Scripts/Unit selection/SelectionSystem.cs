// oldscripter@gmail.com

using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using ECS_Formation;
using System;

public partial class SelectionSystem : SystemBase
{
    private Camera mainCamera;
    private int nextFormationID = 1;
    private float formationSpacing = 0.5f;
    private int maxUnitsPerRow = 10;
    
    public float FormationSpacing => formationSpacing;
    public int MaxUnitsPerRow => maxUnitsPerRow;
    
    protected override void OnCreate()
    {
        Enabled = true;
        Debug.Log("Selection system is ready");
    }
    
    protected override void OnUpdate()
    {
        if (mainCamera == null) 
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
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
        
        // Right click for movement with formation
        if (Input.GetMouseButtonUp(1))
        {
            HandleRightClick();
        }
        
        ApplySelectionVisual();
    }
    
    private bool IsDragging()
    {
        Vector2 start = SelectionBoxUI.GetStartPosition();
        Vector2 end = SelectionBoxUI.GetEndPosition();
        return start != end;
    }
    
    private void HandleRightClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            float3 clickPosition = new float3(hit.point.x, 0, hit.point.z);
            
            // Собираем выделенных юнитов
            var selectedUnits = new List<Entity>();
            var unitPositions = new List<float3>();
            
            Entities.ForEach((Entity entity, ref UnitSelection selection, in LocalTransform transform) =>
            {
                if (selection.IsSelected)
                {
                    selectedUnits.Add(entity);
                    unitPositions.Add(transform.Position);
                }
            }).WithoutBurst().Run();
            
            if (selectedUnits.Count == 0) return;
            
            if (selectedUnits.Count == 1)
            {
                // Одиночный юнит - очищаем формацию и двигаем
                ClearFormation(selectedUnits[0]);
                
                if (EntityManager.HasComponent<MoveTo>(selectedUnits[0]))
                {
                    var moveTo = EntityManager.GetComponentData<MoveTo>(selectedUnits[0]);
                    moveTo.TargetPosition = clickPosition;
                    moveTo.IsMoving = true;
                    EntityManager.SetComponentData(selectedUnits[0], moveTo);
                }
            }
            else
            {
                // Несколько юнитов - создаём формацию
                CreateFormation(selectedUnits, unitPositions, clickPosition);
            }
        }
    }
    
    private void CreateFormation(List<Entity> units, List<float3> currentPositions, float3 targetCenter)
    {
        int formationID = nextFormationID++;
        int unitsCount = units.Count;
        
        // Определяем направление формации
        float3 center = float3.zero;
        foreach (var pos in currentPositions)
        {
            center += pos;
        }
        center /= unitsCount;
        
        float3 directionToTarget = center - targetCenter;
        directionToTarget.y = 0;
        float3 formationForward = math.normalizesafe(directionToTarget);
        
        if (math.length(formationForward) < 0.01f)
        {
            formationForward = new float3(1, 0, 0);
        }
        
        // Создаём временные массивы для сортировки (вместо кортежей)
        Entity[] sortedEntities = new Entity[units.Count];
        float3[] sortedPositions = new float3[units.Count];
        int[] indices = new int[units.Count];
        
        for (int i = 0; i < units.Count; i++)
        {
            indices[i] = i;
        }
        
        // Сортируем индексы по позиции
        System.Array.Sort(indices, (a, b) => 
        {
            int xCompare = currentPositions[b].x.CompareTo(currentPositions[a].x);
            if (xCompare != 0) return xCompare;
            return currentPositions[b].z.CompareTo(currentPositions[b].z);
        });
        
        // Заполняем отсортированные массивы
        for (int i = 0; i < units.Count; i++)
        {
            sortedEntities[i] = units[indices[i]];
            sortedPositions[i] = currentPositions[indices[i]];
        }
        
        // Добавляем компоненты формации каждому юниту
        maxUnitsPerRow = (int)Math.Sqrt(sortedEntities.Length) * 2;
        for (int i = 0; i < sortedEntities.Length; i++)
        {
            int row = i / maxUnitsPerRow;
            int col = i % maxUnitsPerRow;
            
            var member = new FormationMember
            {
                FormationID = formationID,
                RowIndex = row,
                ColIndex = col
            };
            
            // Добавляем или обновляем компонент FormationMember
            if (!EntityManager.HasComponent<FormationMember>(sortedEntities[i]))
            {
                EntityManager.AddComponent(sortedEntities[i], typeof(FormationMember));
            }
            EntityManager.SetComponentData(sortedEntities[i], member);
        }
        
        // Создаём группу формации
        Entity groupEntity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(groupEntity, new FormationGroupData
        {
            FormationID = formationID,
            TargetCenter = targetCenter,
            FormationForward = formationForward,
            MaxUnitsPerRow = maxUnitsPerRow,
            Spacing = formationSpacing,
            TotalUnits = unitsCount,
            IsMoving = true
        });
        
        Debug.Log($"Created formation with {unitsCount} units, ID: {formationID}");
    }
    
    private void ClearFormation(Entity unit)
    {
        if (EntityManager.HasComponent<FormationMember>(unit))
        {
            EntityManager.RemoveComponent<FormationMember>(unit);
        }
    }
    
    private void HandleSingleClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            bool clickedOnUnit = false;
            Entity clickedEntity = Entity.Null;
            
            Entities.ForEach((Entity entity, in LocalTransform transform, in UnitTag tag) =>
            {
                if (!clickedOnUnit)
                {
                    float distance = Vector3.Distance(transform.Position, hit.point);
                    if (distance < 0.5f)
                    {
                        clickedOnUnit = true;
                        clickedEntity = entity;
                    }
                }
            }).WithoutBurst().Run();
            
            if (clickedOnUnit)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    ToggleUnitSelection(clickedEntity);
                }
                else
                {
                    SelectSingleUnit(clickedEntity);
                }
            }
            else
            {
                if (!Input.GetKey(KeyCode.LeftControl))
                {
                    ClearAllSelection();
                }
            }
        }
        else
        {
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
            Entities.ForEach((ref UnitSelection selection) =>
            {
                selection.IsSelected = false;
            }).WithoutBurst().Run();
        }
        
        Entities.ForEach((ref UnitSelection selection, in LocalTransform transform) =>
        {
            Vector2 screenPos = mainCamera.WorldToScreenPoint(transform.Position);
            
            if (selectionRect.Contains(screenPos))
            {
                selection.IsSelected = true;
                selectedCount++;
            }
        }).WithoutBurst().Run();
        
        if (selectedCount > 0)
            Debug.Log($"Selected {selectedCount} units (additive: {additive})");
    }
    
    private void ApplySelectionVisual()
    {
        // Изменение масштаба при выделении
        Entities.ForEach((ref LocalTransform transform, in UnitSelection selection) =>
        {
            transform.Scale = selection.IsSelected ? 1.2f : 1f;
        }).WithoutBurst().Run();
        
        // Изменение цвета при выделении
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
    
    // Публичные методы для изменения параметров формации
    public void SetFormationSpacing(float spacing)
    {
        formationSpacing = Mathf.Max(0.5f, spacing);
    }
    
    public void SetMaxUnitsPerRow(int maxUnits)
    {
        maxUnitsPerRow = Mathf.Max(1, maxUnits);
    }
}