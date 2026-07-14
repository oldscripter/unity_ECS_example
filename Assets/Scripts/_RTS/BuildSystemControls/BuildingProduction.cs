using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BuildingProduction : MonoBehaviour
{
    [Header("Production Settings")]
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float productionTime = 3f;
    [SerializeField] private int maxUnits = 5;
    
    [Header("Animation Settings")]
    [SerializeField] private Animator gateAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string closeTrigger = "Close";
    [SerializeField] private float gateOpenDelay = 0.5f;
    [SerializeField] private float unitExitDelay = 0.5f;
    
    [Header("UI Settings")]
    [SerializeField] private GameObject productionUIPrefab;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, 3, 0);
    
    private int currentUnits = 0;
    private bool isProducing = false;
    private GameObject productionUI;
    private BuildingUI buildingUI;
    private Queue<int> productionQueue = new Queue<int>(); // Очередь заказов
    
    private void Start()
    {
        // Создаем UI
        if (productionUIPrefab != null)
        {
            productionUI = Instantiate(productionUIPrefab, transform.position + uiOffset, Quaternion.identity);
            productionUI.transform.SetParent(transform);
            buildingUI = productionUI.GetComponent<BuildingUI>();
        }
        
        UpdateUI();
    }
    
    public void QueueProduction()
    {
        if (currentUnits >= maxUnits)
        {
            Debug.Log("Maximum units reached!");
            return;
        }
        
        if (unitPrefab == null || spawnPoint == null || exitPoint == null)
        {
            Debug.LogError("Unit prefab or spawn points not assigned!");
            return;
        }
        
        productionQueue.Enqueue(1);
        
        if (!isProducing)
        {
            StartCoroutine(ProductionCoroutine());
        }
        
        UpdateUI();
    }
    
    private IEnumerator ProductionCoroutine()
    {
        isProducing = true;
        
        while (productionQueue.Count > 0 && currentUnits < maxUnits)
        {
            // Уведомляем UI о начале
            if (buildingUI != null)
            {
                buildingUI.OnProductionStart();
            }
            
            float elapsed = 0f;
            
            // Процесс производства с обновлением прогресса
            while (elapsed < productionTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / productionTime;
                
                // Обновляем прогресс в UI
                if (buildingUI != null)
                {
                    buildingUI.OnProductionProgress(progress);
                }
                
                yield return null;
            }
            
            // Создаем юнита
            GameObject newUnit = Instantiate(unitPrefab, spawnPoint.position, spawnPoint.rotation);
            currentUnits++;
            productionQueue.Dequeue();
            
            // Выход юнита
            yield return StartCoroutine(ExitUnit(newUnit));
            
            // Уведомляем UI о завершении
            if (buildingUI != null)
            {
                buildingUI.OnProductionComplete();
            }
            
            UpdateUI();
        }
        
        isProducing = false;
    }
    
    private IEnumerator ExitUnit(GameObject unit)
    {
        // Открываем ворота
        if (gateAnimator != null)
        {
            yield return new WaitForSeconds(gateOpenDelay);
            gateAnimator.SetTrigger(openTrigger);
            yield return new WaitForSeconds(1.5f);
        }
        
        // Движение юнита
        float moveSpeed = 3f;
        Vector3 startPos = unit.transform.position;
        Vector3 endPos = exitPoint.position;
        float journey = 0f;
        
        var unitMovement = unit.GetComponent<UnitMovement>();
        
        while (journey < 1f)
        {
            journey += Time.deltaTime * moveSpeed / Vector3.Distance(startPos, endPos);
            journey = Mathf.Clamp01(journey);
            
            if (unitMovement != null)
            {
                unitMovement.MoveTo(endPos);
                break;
            }
            else
            {
                unit.transform.position = Vector3.Lerp(startPos, endPos, journey);
            }
            
            yield return null;
        }
        
        if (unitMovement == null)
        {
            unit.transform.position = endPos;
        }
        
        // Закрываем ворота
        if (gateAnimator != null)
        {
            yield return new WaitForSeconds(unitExitDelay);
            gateAnimator.SetTrigger(closeTrigger);
        }
    }
    
    private void UpdateUI()
    {
        if (buildingUI != null)
        {
            buildingUI.RefreshUI();
        }
    }
    
    // Геттеры для UI
    public float GetProductionTime() => productionTime;
    public int GetCurrentUnits() => currentUnits;
    public int GetMaxUnits() => maxUnits;
    public bool IsProducing() => isProducing;
    
    private void OnDestroy()
    {
        if (productionUI != null)
        {
            Destroy(productionUI);
        }
    }
}