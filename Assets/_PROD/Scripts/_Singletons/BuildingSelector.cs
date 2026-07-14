using UnityEngine;

public class BuildingSelector : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField] private LayerMask buildingLayer;
       
    private Building selectedBuilding;
    
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, buildingLayer))
            {
                Building building = hit.collider.GetComponentInParent<Building>();
                
                if (building)
                {
                    SelectBuilding(building);
                    return;
                }
            }
            
            DeselectBuilding(); // Deselect if click out of the building
        }
    }
    
    private void SelectBuilding(Building building)
    {
        if (selectedBuilding == building) return;
        DeselectBuilding();
        building.Select();
        selectedBuilding = building;
    }

    private void DeselectBuilding()
    {
        if (!selectedBuilding) return;
        selectedBuilding.Deselect();
        selectedBuilding = null;        
    }
    
    private void OnDestroy()
    {
        DeselectBuilding();
    }
}