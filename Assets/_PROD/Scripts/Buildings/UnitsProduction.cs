using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class UnitsProduction : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private GameObject indicator;
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private Transform exitPosition;
    [SerializeField] private float productionTimeInSeconds = 5f;
    [SerializeField] private uint maxCount = 10;
    [SerializeField] private uint costInGold = 100;
    
    [Header("Production Settings")]
    [SerializeField] private bool autoProduce = false;
    
    [Header("Gate Animation")]
    [SerializeField] private Animator gateAnimator;
    [SerializeField] private string gateOpenTrigger = "Open";
    [SerializeField] private string gateCloseTrigger = "Close";
    [SerializeField] private float gateOpenDelay = 0.5f; // Задержка перед открытием
    [SerializeField] private float gateCloseDelay = 1.5f; // Задержка перед закрытием
    [SerializeField] private bool waitForGateToOpen = true; // Ждать открытия ворот
    
    [Header("UI Settings")]
    [SerializeField] private UnityEngine.UI.Slider progressSlider;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI queueText;
    [SerializeField] private UnityEngine.UI.Button produceButton;
    
    // Приватные поля
    private Queue<GameObject> productionQueue = new Queue<GameObject>();
    private bool isProducing = false;
    private float currentProductionTime = 0f;
    private uint currentCount = 0;
    private Coroutine productionCoroutine;
    private List<GameObject> activeUnits = new List<GameObject>();
    private bool isGateOpen = false;
    
    // События для внешних систем
    public System.Action<GameObject> OnUnitProduced;
    public System.Action<uint> OnUnitCountChanged;
    public System.Action OnProductionComplete;
    public System.Action OnProductionFailed;
    public System.Action OnGateOpened;
    public System.Action OnGateClosed;
    
    // Свойства
    public uint CurrentCount => currentCount;
    public uint MaxCount => maxCount;
    public bool IsProducing => isProducing;
    public int QueueSize => productionQueue.Count;
    public bool IsFull => currentCount >= maxCount;
    public bool IsGateOpen => isGateOpen;

    void Start()
    {
        // Проверка ссылок
        ValidateReferences();
        
        // Инициализация индикатора
        if (indicator != null)
            indicator.SetActive(false);
        
        // Инициализация UI
        UpdateUI();
        if (progressSlider != null)
            progressSlider.gameObject.SetActive(false);
        
        // Настройка кнопки
        if (produceButton != null)
            produceButton.onClick.AddListener(TryProduce);
        
        // Проверка аниматора ворот
        if (gateAnimator == null)
        {
            Debug.LogWarning($"[PRODUCTION] Gate Animator is not assigned on {gameObject.name}!");
        }
        else
        {
            // Убеждаемся, что ворота закрыты в начале
            gateAnimator.ResetTrigger(gateOpenTrigger);
            gateAnimator.SetTrigger(gateCloseTrigger);
            isGateOpen = false;
        }
        
        // Автопроизводство
        if (autoProduce && productionQueue.Count > 0)
        {
            StartProduction();
        }
        
        Debug.Log($"[PRODUCTION] {gameObject.name} is ready. Units: {currentCount}/{maxCount}");
    }
    
    void Update()
    {
        // Обновление прогресса производства
        if (isProducing && progressSlider != null)
        {
            float progress = currentProductionTime / productionTimeInSeconds;
            progressSlider.value = Mathf.Clamp01(progress);
        }
        
        // Автоматическое производство
        if (autoProduce && !isProducing && productionQueue.Count > 0 && !IsFull)
        {
            StartProduction();
        }
    }
    
    /// <summary>
    /// Проверка валидности ссылок
    /// </summary>
    private void ValidateReferences()
    {
        if (unitPrefab == null)
            Debug.LogError($"[PRODUCTION] Unit prefab is not assigned on {gameObject.name}!");
        
        if (spawnPosition == null)
            Debug.LogError($"[PRODUCTION] Spawn position is not assigned on {gameObject.name}!");
        
        if (exitPosition == null)
            Debug.LogError($"[PRODUCTION] Exit position is not assigned on {gameObject.name}!");
    }
    
    /// <summary>
    /// Попытка начать производство
    /// </summary>
    public void TryProduce()
    {
        // Проверка на заполненность
        if (IsFull)
        {
            Debug.LogWarning($"[PRODUCTION] Production is full! Max: {maxCount}");
            if (statusText != null)
                statusText.text = "MAX UNITS!";
            OnProductionFailed?.Invoke();
            return;
        }
        
        // Проверка на наличие денег (если есть система ресурсов)
        if (!HasEnoughResources())
        {
            Debug.LogWarning($"[PRODUCTION] Not enough resources! Need: {costInGold} gold");
            if (statusText != null)
                statusText.text = "NOT ENOUGH GOLD!";
            OnProductionFailed?.Invoke();
            return;
        }
        
        // Добавляем в очередь
        productionQueue.Enqueue(unitPrefab);
        SpendResources();
        
        Debug.Log($"[PRODUCTION] Unit added to queue. Queue size: {productionQueue.Count}");
        
        // Обновляем UI
        UpdateUI();
        
        // Запускаем производство
        if (!isProducing)
        {
            StartProduction();
        }
    }
    
    /// <summary>
    /// Добавление в очередь извне
    /// </summary>
    public void AddToQueue(GameObject unitType, uint count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            if (!IsFull)
            {
                productionQueue.Enqueue(unitType);
            }
            else
            {
                Debug.LogWarning($"[PRODUCTION] Cannot add more units. Queue is full!");
                break;
            }
        }
        UpdateUI();
        
        if (!isProducing && productionQueue.Count > 0)
        {
            StartProduction();
        }
    }
    
    /// <summary>
    /// Старт производственного цикла
    /// </summary>
    private void StartProduction()
    {
        if (isProducing || productionQueue.Count == 0 || IsFull)
            return;
        
        isProducing = true;
        currentProductionTime = 0f;
        
        // Показываем индикатор
        if (indicator != null)
            indicator.SetActive(true);
        
        // Запускаем корутину
        if (productionCoroutine != null)
            StopCoroutine(productionCoroutine);
        productionCoroutine = StartCoroutine(ProductionCoroutine());
        
        Debug.Log($"[PRODUCTION] Production started. Time: {productionTimeInSeconds}s");
        UpdateUI();
    }
    
    /// <summary>
    /// Производственноый процесс
    /// </summary>
    private IEnumerator ProductionCoroutine()
    {
        while (productionQueue.Count > 0 && !IsFull)
        {
            // Получаем следующий юнит из очереди
            GameObject unitToProduce = productionQueue.Dequeue();
            
            // Запускаем таймер
            currentProductionTime = 0f;
            
            while (currentProductionTime < productionTimeInSeconds)
            {
                currentProductionTime += Time.deltaTime;
                
                // Обновляем UI прогресса
                if (progressSlider)
                {
                    progressSlider.gameObject.SetActive(true);
                    progressSlider.value = currentProductionTime / productionTimeInSeconds;
                }
                
                // Обновляем статус
                if (statusText != null)
                {
                    float remaining = productionTimeInSeconds - currentProductionTime;
                    statusText.text = $"PRODUCING... {remaining:F1}s";
                }
                
                yield return null;
            }
            
            // Производство завершено - открываем ворота и спавним юнита
            yield return StartCoroutine(SpawnWithGateAnimation(unitToProduce));
        }
        
        // Производство завершено
        isProducing = false;
        
        if (indicator != null)
            indicator.SetActive(false);

        if (progressSlider)
        {
            progressSlider.value = 0;
            progressSlider.gameObject.SetActive(false);
        }

        if (statusText != null)
            statusText.text = "READY";
        
        OnProductionComplete?.Invoke();
        UpdateUI();
        
        Debug.Log($"[PRODUCTION] Production completed. Total units: {currentCount}");
    }
    
    /// <summary>
    /// Спавн юнита с анимацией открытия ворот
    /// </summary>
    private IEnumerator SpawnWithGateAnimation(GameObject unitToSpawn)
    {
        // Открываем ворота
        yield return StartCoroutine(OpenGate());
        
        // Спавним юнита
        Spawn(unitToSpawn);
        
        // Небольшая задержка перед закрытием ворот
        yield return new WaitForSeconds(gateCloseDelay);
        
        // Закрываем ворота
        yield return StartCoroutine(CloseGate());
    }
    
    /// <summary>
    /// Анимация открытия ворот
    /// </summary>
    private IEnumerator OpenGate()
    {
        if (gateAnimator == null || isGateOpen)
        {
            yield break;
        }
        
        Debug.Log($"[PRODUCTION] Opening gates...");
        
        // Задержка перед открытием
        if (gateOpenDelay > 0)
            yield return new WaitForSeconds(gateOpenDelay);
        
        // Запускаем анимацию открытия
        gateAnimator.ResetTrigger(gateCloseTrigger);
        gateAnimator.SetTrigger(gateOpenTrigger);
        isGateOpen = true;
        
        OnGateOpened?.Invoke();
        
        // Если нужно ждать полного открытия
        if (waitForGateToOpen)
        {
            // Ждем пока анимация не дойдет до конца
            AnimatorStateInfo stateInfo = gateAnimator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;
            
            // Ждем пока не закончится анимация открытия
            yield return new WaitForSeconds(animationLength);
            
            Debug.Log($"[PRODUCTION] Gates are fully opened");
        }
        else
        {
            // Даем время на старт анимации
            yield return new WaitForSeconds(0.3f);
        }
    }
    
    /// <summary>
    /// Анимация закрытия ворот
    /// </summary>
    private IEnumerator CloseGate()
    {
        if (gateAnimator == null || !isGateOpen)
        {
            yield break;
        }
        
        Debug.Log($"[PRODUCTION] Closing gates...");
        
        // Запускаем анимацию закрытия
        gateAnimator.ResetTrigger(gateOpenTrigger);
        gateAnimator.SetTrigger(gateCloseTrigger);
        isGateOpen = false;
        
        OnGateClosed?.Invoke();
        
        // Даем время на анимацию закрытия
        AnimatorStateInfo stateInfo = gateAnimator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;
        
        // Ждем пока не закончится анимация закрытия
        yield return new WaitForSeconds(animationLength);
        
        Debug.Log($"[PRODUCTION] Gates are fully closed");
    }
    
    /// <summary>
    /// Спавн юнита
    /// </summary>
    public void Spawn(GameObject unitPrefabToSpawn = null)
    {
        if (unitPrefabToSpawn == null)
            unitPrefabToSpawn = unitPrefab;
        
        if (unitPrefabToSpawn == null || spawnPosition == null)
        {
            Debug.LogError($"[PRODUCTION] Cannot spawn unit - missing references!");
            return;
        }
        
        // Проверка на заполненность
        if (IsFull)
        {
            Debug.LogWarning($"[PRODUCTION] Cannot spawn - max units reached!");
            return;
        }
        
        // Инстанциируем юнит
        GameObject newUnit = Instantiate(unitPrefabToSpawn, spawnPosition.position, spawnPosition.rotation);
        newUnit.name = $"{unitPrefabToSpawn.name}_{currentCount + 1}";
        
        currentCount++;
        activeUnits.Add(newUnit);
        
        // Добавляем компонент для эффекта появления (опционально)
        StartCoroutine(SpawnEffect(newUnit));
        
        // Отправляем к выходу
        GoToExit(newUnit);
        
        // События
        OnUnitProduced?.Invoke(newUnit);
        OnUnitCountChanged?.Invoke(currentCount);
        
        Debug.Log($"[PRODUCTION] Unit spawned: {newUnit.name} ({currentCount}/{maxCount})");
        
        UpdateUI();
    }
    
    /// <summary>
    /// Эффект появления юнита (опционально)
    /// </summary>
    private IEnumerator SpawnEffect(GameObject unit)
    {
        // Эффект масштабирования при появлении
        Vector3 originalScale = unit.transform.localScale;
        unit.transform.localScale = Vector3.zero;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float scale = Mathf.SmoothStep(0, 1, progress);
            unit.transform.localScale = originalScale * scale;
            yield return null;
        }
        
        unit.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// Отправка юнита к точке выхода
    /// </summary>
    private void GoToExit(GameObject unit)
    {
        if (unit == null || exitPosition == null)
            return;
        
        // Простое движение через Transform
        unit.transform.position = exitPosition.position;
        
        Debug.Log($"[PRODUCTION] Unit {unit.name} is moving to exit");
    }
    
    /// <summary>
    /// Проверка наличия ресурсов
    /// </summary>
    private bool HasEnoughResources()
    {
        // Здесь должна быть система ресурсов
        // Например:
        // return ResourceManager.Instance.HasGold(costInGold);
        return true; // Временное решение
    }
    
    /// <summary>
    /// Трата ресурсов
    /// </summary>
    private void SpendResources()
    {
        // Здесь должна быть система ресурсов
        // Например:
        // ResourceManager.Instance.SpendGold(costInGold);
    }
    
    /// <summary>
    /// Обновление UI
    /// </summary>
    private void UpdateUI()
    {
        if (queueText != null)
        {
            if (productionQueue.Count == 0 && !isProducing)
                queueText.text = "+";
            else
                queueText.text = $"{productionQueue.Count + 1}";
        }
        
        if (statusText != null && !isProducing)
        {
            if (IsFull)
                statusText.text = "FULL!";
            else if (productionQueue.Count > 0)
                statusText.text = "READY";
            else
                statusText.text = "IDLE";
        }
    }
    
    /// <summary>
    /// Отмена производства
    /// </summary>
    public void CancelProduction()
    {
        if (productionCoroutine != null)
        {
            StopCoroutine(productionCoroutine);
            productionCoroutine = null;
        }
        
        isProducing = false;
        currentProductionTime = 0f;
        productionQueue.Clear();
        
        if (indicator != null)
            indicator.SetActive(false);
        
        if (statusText != null)
            statusText.text = "CANCELLED";
        
        if (progressSlider != null)
            progressSlider.value = 0f;
        
        // Закрываем ворота если они открыты
        if (isGateOpen)
        {
            StartCoroutine(CloseGate());
        }
        
        UpdateUI();
        
        Debug.Log($"[PRODUCTION] Production cancelled");
    }
    
    /// <summary>
    /// Остановка производства (без очистки очереди)
    /// </summary>
    public void StopProduction()
    {
        if (productionCoroutine != null)
        {
            StopCoroutine(productionCoroutine);
            productionCoroutine = null;
        }
        
        isProducing = false;
        currentProductionTime = 0f;
        
        if (indicator != null)
            indicator.SetActive(false);
        
        if (progressSlider != null)
            progressSlider.value = 0f;
        
        // Закрываем ворота если они открыты
        if (isGateOpen)
        {
            StartCoroutine(CloseGate());
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Возобновление производства
    /// </summary>
    public void ResumeProduction()
    {
        if (!isProducing && productionQueue.Count > 0 && !IsFull)
        {
            StartProduction();
        }
    }
    
    /// <summary>
    /// Получение всех активных юнитов
    /// </summary>
    public List<GameObject> GetActiveUnits()
    {
        return new List<GameObject>(activeUnits);
    }
    
    /// <summary>
    /// Удаление юнита из списка (при смерти)
    /// </summary>
    public void RemoveUnit(GameObject unit)
    {
        if (activeUnits.Contains(unit))
        {
            activeUnits.Remove(unit);
            currentCount = (uint)activeUnits.Count;
            OnUnitCountChanged?.Invoke(currentCount);
            
            Debug.Log($"[PRODUCTION] Unit removed: {unit.name}. Active: {currentCount}");
        }
    }
    
    /// <summary>
    /// Очистка всех юнитов
    /// </summary>
    public void ClearAllUnits()
    {
        foreach (GameObject unit in activeUnits)
        {
            if (unit != null)
                Destroy(unit);
        }
        activeUnits.Clear();
        currentCount = 0;
        OnUnitCountChanged?.Invoke(currentCount);
        
        Debug.Log($"[PRODUCTION] All units cleared");
    }
    
    /// <summary>
    /// Ручное открытие ворот (для тестирования)
    /// </summary>
    public void ManualOpenGate()
    {
        if (gateAnimator != null && !isGateOpen)
        {
            StartCoroutine(OpenGate());
        }
    }
    
    /// <summary>
    /// Ручное закрытие ворот (для тестирования)
    /// </summary>
    public void ManualCloseGate()
    {
        if (gateAnimator != null && isGateOpen)
        {
            StartCoroutine(CloseGate());
        }
    }
}