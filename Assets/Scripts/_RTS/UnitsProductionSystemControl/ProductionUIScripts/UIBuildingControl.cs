using UnityEngine;
using UnityEngine.UI;
using TMPro; // Важно: добавить для TextMeshPro
using System.Collections;

public class BuildingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button produceButton;           // Стандартная Unity Button
    [SerializeField] private Image buttonBackground;         // Фон кнопки
    [SerializeField] private TMP_Text buttonText;           // TextMeshPro текст
    [SerializeField] private Image progressFill;            // Отдельный Image для заливки (опционально)
    
    [Header("TextMeshPro Settings")]
    [SerializeField] private TMP_FontAsset customFont;      // Опциональный шрифт
    [SerializeField] private Material fontMaterial;         // Опциональный материал шрифта
    
    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private Color producingColor = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] private Color completeColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color blockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    [Header("Text Colors")]
    [SerializeField] private Color textIdleColor = Color.white;
    [SerializeField] private Color textProducingColor = Color.white;
    [SerializeField] private Color textBlockedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [Header("Animation")]
    [SerializeField] private bool enablePulseAnimation = true;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.05f;
    
    private BuildingProduction building;
    private Camera mainCamera;
    private int queuedUnits = 0;
    private bool isProducing = false;
    private float productionProgress = 0f;
    private Vector3 originalScale;
    private Coroutine pulseCoroutine;
    
    private void Start()
    {
        mainCamera = Camera.main;
        building = GetComponentInParent<BuildingProduction>();
        
        if (building == null)
        {
            Debug.LogError("BuildingUI: BuildingProduction not found in parent!");
            return;
        }
        
        // Настраиваем кнопку
        if (produceButton != null)
        {
            produceButton.onClick.AddListener(OnProduceButtonClick);
        }
        
        // Настройка TextMeshPro
        if (buttonText != null)
        {
            if (customFont != null)
                buttonText.font = customFont;
            if (fontMaterial != null)
                buttonText.fontMaterial = fontMaterial;
                
            // Настройка выравнивания
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontSize = 36;
            buttonText.fontStyle = FontStyles.Bold;
        }
        
        // Сохраняем оригинальный масштаб для анимации
        if (produceButton != null)
        {
            originalScale = produceButton.transform.localScale;
        }
        
        // Настраиваем прогресс
        if (progressFill != null)
        {
            progressFill.fillAmount = 0f;
            progressFill.gameObject.SetActive(false);
        }
        
        UpdateUI();
        StartCoroutine(UpdateUIRoutine());
    }
    
    private void Update()
    {
        // UI всегда смотрит на камеру
        if (mainCamera != null && transform.parent != null)
        {
            // Опция 1: Полный поворот к камере
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
            
            // Опция 2: Только поворот по Y (чтобы текст не переворачивался)
            // Vector3 direction = transform.position - mainCamera.transform.position;
            // direction.y = 0;
            // transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    private IEnumerator UpdateUIRoutine()
    {
        while (true)
        {
            UpdateUI();
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void OnProduceButtonClick()
    {
        if (building != null && !isProducing)
        {
            queuedUnits++;
            building.QueueProduction();
            UpdateUI();
        }
    }
    
    public void OnProductionStart()
    {
        isProducing = true;
        productionProgress = 0f;
        
        if (produceButton != null)
        {
            produceButton.interactable = false;
        }
        
        // Запускаем анимацию пульсации
        if (enablePulseAnimation)
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }
        
        UpdateUI();
    }
    
    public void OnProductionProgress(float progress)
    {
        productionProgress = Mathf.Clamp01(progress);
        UpdateUI();
    }
    
    public void OnProductionComplete()
    {
        isProducing = false;
        queuedUnits = Mathf.Max(0, queuedUnits - 1);
        productionProgress = 0f;
        
        if (produceButton != null)
        {
            produceButton.interactable = true;
        }
        
        // Останавливаем анимацию пульсации
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // Возвращаем оригинальный масштаб
        if (produceButton != null)
        {
            produceButton.transform.localScale = originalScale;
        }
        
        UpdateUI();
        
        // Если есть еще юниты в очереди
        if (queuedUnits > 0 && building != null)
        {
            building.QueueProduction();
        }
    }
    
    public void UpdateUI()
    {
        if (building == null) return;
        
        int currentUnits = building.GetCurrentUnits();
        int maxUnits = building.GetMaxUnits();
        bool isFull = currentUnits >= maxUnits;
        
        // Обновляем текст кнопки с эмодзи и форматированием
        if (buttonText != null)
        {
            string displayText = "";
            
            if (queuedUnits > 0)
            {
                // Используем TMP тэги для цвета и размера
                displayText = $"<size=80%>⚡</size> {queuedUnits}";
                // Или просто цифру
                // displayText = queuedUnits.ToString();
            }
            else if (isFull)
            {
                displayText = "<color=#FF6B6B>MAX</color>";
            }
            else
            {
                displayText = "<size=120%>+</size>";
            }
            
            buttonText.text = displayText;
            
            // Обновляем цвет текста
            if (isFull)
            {
                buttonText.color = textBlockedColor;
            }
            else if (isProducing)
            {
                buttonText.color = textProducingColor;
            }
            else
            {
                buttonText.color = textIdleColor;
            }
        }
        
        // Обновляем внешний вид кнопки
        UpdateButtonAppearance(isFull);
    }
    
    private void UpdateButtonAppearance(bool isFull)
    {
        if (buttonBackground == null) return;
        
        Color targetColor;
        bool showProgress = false;
        
        if (isFull || (building != null && building.GetCurrentUnits() >= building.GetMaxUnits()))
        {
            targetColor = blockedColor;
        }
        else if (isProducing)
        {
            // Смешиваем цвета с учетом прогресса
            targetColor = Color.Lerp(idleColor, producingColor, productionProgress);
            showProgress = true;
            
            // Обновляем заливку
            if (progressFill != null)
            {
                progressFill.gameObject.SetActive(true);
                progressFill.fillAmount = productionProgress;
                progressFill.color = Color.Lerp(producingColor, completeColor, productionProgress);
            }
            else
            {
                // Используем заливку самой кнопки
                buttonBackground.fillAmount = productionProgress;
                buttonBackground.fillMethod = Image.FillMethod.Horizontal;
            }
        }
        else
        {
            targetColor = idleColor;
            
            // Скрываем заливку
            if (progressFill != null)
            {
                progressFill.gameObject.SetActive(false);
            }
            else
            {
                buttonBackground.fillAmount = 0f;
            }
        }
        
        buttonBackground.color = targetColor;
        
        // Обновляем доступность кнопки
        if (produceButton != null)
        {
            produceButton.interactable = !isFull && !isProducing;
        }
    }
    
    private IEnumerator PulseAnimation()
    {
        if (produceButton == null) yield break;
        
        float time = 0f;
        Vector3 baseScale = originalScale;
        
        while (isProducing)
        {
            time += Time.deltaTime * pulseSpeed;
            float scaleMultiplier = 1f + Mathf.Sin(time) * pulseAmount;
            
            produceButton.transform.localScale = baseScale * scaleMultiplier;
            
            yield return null;
        }
    }
    
    public void RefreshUI()
    {
        UpdateUI();
    }
    
    // Метод для обновления количества в очереди извне
    public void UpdateQueueCount(int count)
    {
        queuedUnits = count;
        UpdateUI();
    }
}