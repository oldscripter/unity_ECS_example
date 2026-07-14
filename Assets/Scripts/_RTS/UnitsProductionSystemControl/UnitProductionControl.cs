using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BuildControl : MonoBehaviour
{
    [System.Serializable]
    public class BuildingData
    {
        [Header("Настройки здания")]
        public GameObject prefab;
        public string buildingName = "Здание";
        public KeyCode hotkey = KeyCode.Alpha1;
        public int cost = 100;
        public Sprite icon;
        public bool isWall = false;
        
        [Header("Настройки стен")]
        public float wallSpacing = 1f;
        public bool snapToGrid = true;
        
        [Header("Привязка к GUI")]
        public Button uiButton;
        public Text buttonText;
        public Image buttonImage;
        
        [Header("Дополнительные параметры")]
        public Vector3 buildingOffset = Vector3.zero;
        public bool allowRotation = true;
        public float rotationStep = 90f;
        public Color selectedColor = Color.yellow;
        public Color normalColor = Color.white;
    }
    
    [Header("Настройки строительства")]
    [SerializeField] private List<BuildingData> buildings = new List<BuildingData>();
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float maxPlacementDistance = 50f;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private LayerMask buildingLayerMask;
    
    [Header("Настройки GUI")]
    [SerializeField] private Text statusText;
    [SerializeField] private KeyCode toggleBuildModeKey = KeyCode.B;
    
    [Header("Визуализация")]
    [SerializeField] private Color validPlacementColor = Color.green;
    [SerializeField] private Color invalidPlacementColor = Color.red;
    [SerializeField] private Color wallPreviewColor = Color.cyan;
    [SerializeField] private bool showGrid = true;
    
    [Header("Система ресурсов")]
    [SerializeField] private bool useResources = false;
    
    // Приватные переменные
    private bool isBuildMode = false;
    private int selectedBuildingIndex = -1;
    private GameObject previewBuilding;
    private Vector3 currentGridPosition;
    private bool isPositionValid = false;
    private Camera mainCamera;
    private RaycastHit groundHit;
    private float currentRotation = 0f;
    
    // Переменные для рисования стен
    private bool isDrawingWall = false;
    private Vector3 wallStartPosition;
    private Vector3 wallEndPosition;
    private List<GameObject> wallPreviews = new List<GameObject>();
    private List<Vector3> wallPlacementPositions = new List<Vector3>();
    private bool isWallHorizontal = true;
    private bool hasSinglePreview = false;
    
    // Список размещенных зданий
    private List<Vector3> placedBuildings = new List<Vector3>();
    private List<GameObject> buildingObjects = new List<GameObject>();
    
    // События
    public System.Action<GameObject, Vector3, int> OnBuildingPlaced;
    public System.Action<int> OnBuildModeActivated;
    public System.Action OnBuildModeDeactivated;
    public System.Action<int> OnBuildingSelected;
    public System.Action<Vector3, Vector3, int> OnWallPlaced;
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        if (groundLayerMask == 0)
            groundLayerMask = LayerMask.GetMask("Default");
        
        if (buildingLayerMask == 0)
            buildingLayerMask = LayerMask.GetMask("Default");
        
        SetupButtons();
        UpdateAllButtons();
        UpdateStatusText();
    }
    
    private void SetupButtons()
    {
        for (int i = 0; i < buildings.Count; i++)
        {
            BuildingData building = buildings[i];
            
            if (building.uiButton != null)
            {
                int index = i;
                building.uiButton.onClick.RemoveAllListeners();
                building.uiButton.onClick.AddListener(() => SelectBuilding(index));
            }
            
            if (building.buttonText != null)
            {
                string costText = useResources ? $" ({building.cost})" : "";
                string wallText = building.isWall ? " 🧱" : "";
                building.buttonText.text = $"{building.buildingName}{wallText}{costText}\n[{building.hotkey}]";
            }
            
            if (building.buttonImage != null && building.icon != null)
            {
                building.buttonImage.sprite = building.icon;
            }
        }
    }
    
    private void Update()
    {
        // Переключение режима по клавише
        if (Input.GetKeyDown(toggleBuildModeKey))
        {
            ToggleBuildMode();
        }
        
        // Выбор здания по горячим клавишам
        for (int i = 0; i < buildings.Count; i++)
        {
            if (Input.GetKeyDown(buildings[i].hotkey))
            {
                SelectBuilding(i);
            }
        }
        
        if (!isBuildMode || selectedBuildingIndex < 0) return;
        
        BuildingData currentBuilding = buildings[selectedBuildingIndex];
        
        // Обработка Esc - приоритетная
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentBuilding.isWall && isDrawingWall)
            {
                // Если рисуем стену - отменяем рисование, но остаемся в режиме строительства
                CancelWallDrawing();
            }
            else
            {
                // Выходим из режима строительства полностью
                CancelBuildMode();
            }
            return; // Важно: выходим, чтобы не обрабатывать другие действия
        }
        
        if (currentBuilding.isWall)
        {
            UpdateWallPlacement();
            return;
        }
        
        // Обычное размещение
        UpdatePreviewPosition();
        
        // Размещение по ЛКМ
        if (Input.GetMouseButtonDown(0) && isPositionValid)
        {
            PlaceBuilding();
        }
        
        // Поворот здания по ПКМ
        if (Input.GetMouseButtonDown(1) && currentBuilding.allowRotation)
        {
            RotatePreview();
        }
    }
    
    #region Логика размещения стен
    
    private void UpdateWallPlacement()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (!Physics.Raycast(ray, out groundHit, maxPlacementDistance, groundLayerMask))
        {
            if (!isDrawingWall)
            {
                ClearWallPreviews();
                hasSinglePreview = false;
            }
            return;
        }
        
        Vector3 hitPoint = groundHit.point;
        
        if (!isDrawingWall)
        {
            // Показываем превью одного куска стены под курсором
            Vector3 snapPosition = SnapToGrid(hitPoint);
            ShowSingleWallPreview(snapPosition);
            
            // Первый клик - начало стены
            if (Input.GetMouseButtonDown(0))
            {
                wallStartPosition = snapPosition;
                isDrawingWall = true;
                isWallHorizontal = true;
                hasSinglePreview = false;
                
                // Очищаем одиночное превью
                ClearWallPreviews();
                
                Debug.Log("🧱 Начало рисования стены. Нажмите ЛКМ для завершения, ESC для отмены");
                UpdateStatusText();
            }
        }
        else
        {
            // Рисуем стену
            Vector3 startPos = wallStartPosition;
            Vector3 endPos = SnapToGrid(hitPoint);
            
            // Определяем направление стены
            Vector3 delta = endPos - startPos;
            isWallHorizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z);
            
            // Если стена горизонтальная - фиксируем Z, если вертикальная - фиксируем X
            if (isWallHorizontal)
                endPos.z = startPos.z;
            else
                endPos.x = startPos.x;
            
            wallEndPosition = endPos;
            
            // Обновляем превью стены
            UpdateWallPreviews(startPos, endPos, isWallHorizontal);
            
            // Подтверждение размещения
            if (Input.GetMouseButtonDown(0))
                ConfirmWallPlacement();
        }
    }
    
    private void ShowSingleWallPreview(Vector3 position)
    {
        // Если уже есть превью и позиция не изменилась - ничего не делаем
        if (wallPreviews.Count > 0 && wallPlacementPositions.Count > 0)
        {
            if (Vector3.Distance(wallPlacementPositions[0], position) < 0.01f)
                return;
        }
        
        // Очищаем старые превью
        ClearWallPreviews();
        
        // Проверяем валидность позиции
        bool isValid = IsPositionValid(position);
        
        // Создаем превью одного куска стены
        BuildingData building = buildings[selectedBuildingIndex];
        GameObject preview;
        
        if (building.prefab != null)
        {
            preview = Instantiate(building.prefab, position, Quaternion.identity);
            preview.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.transform.position = position;
            preview.transform.localScale = new Vector3(1f, 0.5f, 1f);
            Destroy(preview.GetComponent<Collider>());
        }
        
        preview.name = "WallPreview_Single";
        
        // Отключаем все скрипты
        foreach (MonoBehaviour script in preview.GetComponentsInChildren<MonoBehaviour>())
        {
            script.enabled = false;
        }
        
        // Делаем полупрозрачным
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = new Material(renderer.material);
            Color color = isValid ? validPlacementColor : invalidPlacementColor;
            color.a = 0.5f;
            mat.color = color;
            renderer.material = mat;
        }
        
        // Отключаем коллайдеры
        foreach (Collider col in preview.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
        
        wallPreviews.Add(preview);
        wallPlacementPositions.Add(position);
        hasSinglePreview = true;
    }
    
    private void UpdateWallPreviews(Vector3 start, Vector3 end, bool horizontal)
    {
        ClearWallPreviews();
        hasSinglePreview = false;
        
        // Вычисляем количество стен
        float distance = horizontal ? Mathf.Abs(end.x - start.x) : Mathf.Abs(end.z - start.z);
        int wallCount = Mathf.Max(1, Mathf.RoundToInt(distance / buildings[selectedBuildingIndex].wallSpacing) + 1);
        
        // Создаем превью для каждой стены
        for (int i = 0; i < wallCount; i++)
        {
            float t = wallCount > 1 ? (float)i / (wallCount - 1) : 0f;
            Vector3 position;
            
            if (horizontal)
            {
                position = new Vector3(
                    Mathf.Lerp(start.x, end.x, t),
                    start.y,
                    start.z
                );
            }
            else
            {
                position = new Vector3(
                    start.x,
                    start.y,
                    Mathf.Lerp(start.z, end.z, t)
                );
            }
            
            // Создаем превью стены
            GameObject wallPreview = CreateWallPreview(position, horizontal);
            wallPreviews.Add(wallPreview);
            wallPlacementPositions.Add(position);
        }
        
        UpdateWallPreviewColors();
    }
    
    private GameObject CreateWallPreview(Vector3 position, bool horizontal)
    {
        BuildingData building = buildings[selectedBuildingIndex];
        GameObject preview;
        
        if (building.prefab != null)
        {
            preview = Instantiate(building.prefab, position, Quaternion.identity);
            
            if (horizontal)
                preview.transform.rotation = Quaternion.Euler(0, 0, 0);
            else
                preview.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else
        {
            preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.transform.position = position;
            preview.transform.localScale = new Vector3(1f, 0.5f, 1f);
            Destroy(preview.GetComponent<Collider>());
        }
        
        preview.name = "WallPreview";
        
        foreach (MonoBehaviour script in preview.GetComponentsInChildren<MonoBehaviour>())
        {
            script.enabled = false;
        }
        
        foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>())
        {
            Material mat = new Material(renderer.material);
            Color color = wallPreviewColor;
            color.a = 0.5f;
            mat.color = color;
            renderer.material = mat;
        }
        
        foreach (Collider col in preview.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
        
        return preview;
    }
    
    private void UpdateWallPreviewColors()
    {
        bool allValid = true;
        
        foreach (Vector3 pos in wallPlacementPositions)
        {
            if (!IsPositionValid(pos))
            {
                allValid = false;
                break;
            }
        }
        
        Color color = allValid ? validPlacementColor : invalidPlacementColor;
        color.a = 0.5f;
        
        foreach (GameObject preview in wallPreviews)
        {
            Renderer[] renderers = preview.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = color;
            }
        }
    }
    
    private void ConfirmWallPlacement()
    {
        if (wallPreviews.Count == 0) return;
        
        // Проверяем все позиции
        bool allValid = true;
        foreach (Vector3 pos in wallPlacementPositions)
        {
            if (!IsPositionValid(pos))
            {
                allValid = false;
                break;
            }
        }
        
        if (!allValid)
        {
            Debug.Log("⚠️ Некоторые позиции стены заняты!");
            return;
        }
        
        // Проверяем ресурсы
        int totalCost = wallPlacementPositions.Count * buildings[selectedBuildingIndex].cost;
        if (useResources && !CheckResources(totalCost))
        {
            Debug.Log($"⚠️ Недостаточно ресурсов! Нужно: {totalCost}");
            return;
        }
        
        // Размещаем стены
        List<GameObject> placedWalls = new List<GameObject>();
        foreach (Vector3 pos in wallPlacementPositions)
        {
            GameObject wall = PlaceSingleWall(pos);
            if (wall != null)
                placedWalls.Add(wall);
        }
        
        if (useResources)
            SpendResources(totalCost);
        
        Debug.Log($"Размещено {placedWalls.Count} стен");
        OnWallPlaced?.Invoke(wallStartPosition, wallEndPosition, selectedBuildingIndex);
        
        ClearWallPreviews();
        isDrawingWall = false;
        hasSinglePreview = false;
        UpdateStatusText();
    }
    
    private GameObject PlaceSingleWall(Vector3 position)
    {
        BuildingData building = buildings[selectedBuildingIndex];
        
        Quaternion rotation = Quaternion.Euler(0, isWallHorizontal ? 0 : 90, 0);
        GameObject wall = Instantiate(building.prefab, position, rotation);
        wall.name = $"{building.buildingName}_{placedBuildings.Count}";
        
        foreach (MonoBehaviour script in wall.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = true;
        
        foreach (Collider col in wall.GetComponentsInChildren<Collider>())
            col.enabled = true;
        
        foreach (Rigidbody rb in wall.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.detectCollisions = true;
        }
        
        wall.tag = "Building";
        
        placedBuildings.Add(position);
        buildingObjects.Add(wall);
        
        OnBuildingPlaced?.Invoke(wall, position, selectedBuildingIndex);
        
        return wall;
    }
    
    private void ClearWallPreviews()
    {
        foreach (GameObject preview in wallPreviews)
        {
            if (preview != null)
                Destroy(preview);
        }
        wallPreviews.Clear();
        wallPlacementPositions.Clear();
        hasSinglePreview = false;
    }
    
    private void CancelWallDrawing()
    {
        ClearWallPreviews();
        isDrawingWall = false;
        hasSinglePreview = false;
        Debug.Log("🚫 Рисование стены отменено");
        UpdateStatusText();
    }
    
    #endregion
    
    #region Основная логика строительства
    
    private void SelectBuilding(int index)
    {
        if (index < 0 || index >= buildings.Count) return;
        
        BuildingData building = buildings[index];
        if (building.prefab == null)
        {
            Debug.LogWarning($"Префаб для здания {index} не назначен");
            return;
        }
        
        // Если рисовали стену - отменяем рисование и очищаем все превью
        if (isDrawingWall)
        {
            ClearWallPreviews();
            isDrawingWall = false;
            hasSinglePreview = false;
            Debug.Log("Рисование стены отменено при смене здания");
        }
        
        // Дополнительная очистка превью стен на всякий случай
        if (wallPreviews.Count > 0)
        {
            ClearWallPreviews();
            hasSinglePreview = false;
        }
        
        // Если режим не активен - активируем
        if (!isBuildMode)
        {
            isBuildMode = true;
            OnBuildModeActivated?.Invoke(index);
        }
        
        int previousIndex = selectedBuildingIndex;
        selectedBuildingIndex = index;
        currentRotation = 0f;
        
        // Обновляем состояние кнопок
        if (previousIndex >= 0 && previousIndex < buildings.Count)
            UpdateButtonState(previousIndex);
        UpdateButtonState(index);
        
        // Удаляем старое превью здания
        if (previewBuilding != null)
        {
            Destroy(previewBuilding);
            previewBuilding = null;
        }
        
        // Создаем превью для нового здания (если это не стена)
        if (!building.isWall)
            CreatePreview();
        else
        {
            // Для стены сразу показываем одиночное превью
            // Оно будет создано в UpdateWallPlacement при движении мыши
            hasSinglePreview = false;
        }
        
        Debug.Log($"🏗️ Выбрано здание: {building.buildingName}");
        OnBuildingSelected?.Invoke(index);
        UpdateStatusText();
    }
    
    private void RotatePreview()
    {
        if (previewBuilding == null || selectedBuildingIndex < 0) return;
        
        BuildingData building = buildings[selectedBuildingIndex];
        currentRotation += building.rotationStep;
        if (currentRotation >= 360f)
            currentRotation = 0f;
        
        previewBuilding.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
        Debug.Log($"Поворот: {currentRotation}");
    }
    
    private void UpdatePreviewPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out groundHit, maxPlacementDistance, groundLayerMask))
        {
            Vector3 rawPosition = groundHit.point;
            currentGridPosition = SnapToGrid(rawPosition);
            isPositionValid = IsPositionValid(currentGridPosition);
            UpdatePreview(currentGridPosition, isPositionValid);
        }
        else
        {
            if (previewBuilding != null)
                previewBuilding.SetActive(false);
            isPositionValid = false;
        }
    }
    
    private Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float z = Mathf.Round(position.z / gridSize) * gridSize;
        float y = GetGroundHeight(new Vector3(x, 100f, z));
        return new Vector3(x, y, z);
    }
    
    private float GetGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position, Vector3.down, out hit, 200f, groundLayerMask))
            return hit.point.y;
        return 0f;
    }
    
    private bool IsPositionValid(Vector3 position)
    {
        foreach (Vector3 placedPos in placedBuildings)
        {
            if (Vector3.Distance(placedPos, position) < gridSize * 0.8f)
                return false;
        }
        
        Collider[] colliders = Physics.OverlapSphere(position, gridSize * 0.4f, buildingLayerMask);
        foreach (Collider col in colliders)
        {
            if (col.isTrigger) continue;
            if (col.gameObject == previewBuilding) continue;
            if (wallPreviews.Contains(col.gameObject)) continue;
            if (col.gameObject.CompareTag("Player")) continue;
            return false;
        }
        
        return true;
    }
    
    private void UpdatePreview(Vector3 position, bool isValid)
    {
        if (previewBuilding == null)
            CreatePreview();
        
        previewBuilding.transform.position = position;
        previewBuilding.SetActive(true);
        
        Renderer[] renderers = previewBuilding.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                Color color = isValid ? validPlacementColor : invalidPlacementColor;
                color.a = 0.5f;
                mat.color = color;
            }
        }
    }
    
    private void CreatePreview()
    {
        if (selectedBuildingIndex < 0 || selectedBuildingIndex >= buildings.Count) return;
        
        BuildingData building = buildings[selectedBuildingIndex];
        if (building.prefab == null || building.isWall) return;
        
        previewBuilding = Instantiate(building.prefab, Vector3.zero, Quaternion.identity);
        previewBuilding.name = $"{building.buildingName}_Preview";
        
        foreach (MonoBehaviour script in previewBuilding.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = false;
        
        foreach (Rigidbody rb in previewBuilding.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        
        foreach (Collider col in previewBuilding.GetComponentsInChildren<Collider>())
            col.enabled = false;
        
        foreach (Renderer renderer in previewBuilding.GetComponentsInChildren<Renderer>())
        {
            Material mat = new Material(renderer.material);
            Color color = mat.color;
            color.a = 0.5f;
            mat.color = color;
            renderer.material = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        
        foreach (Animator anim in previewBuilding.GetComponentsInChildren<Animator>())
            anim.enabled = false;
        
        foreach (ParticleSystem ps in previewBuilding.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop();
            var emission = ps.emission;
            emission.enabled = false;
        }
        
        foreach (AudioSource audio in previewBuilding.GetComponentsInChildren<AudioSource>())
            audio.enabled = false;
        
        previewBuilding.SetActive(false);
    }
    
    private void PlaceBuilding()
    {
        if (selectedBuildingIndex < 0 || selectedBuildingIndex >= buildings.Count) return;
        
        BuildingData building = buildings[selectedBuildingIndex];
        if (building.prefab == null)
        {
            Debug.LogError($"Префаб {building.buildingName} не назначен");
            return;
        }
        
        if (!isPositionValid)
        {
            Debug.Log($"Невозможно разместить {building.buildingName} здесь");
            return;
        }
        
        if (useResources && building.cost > 0)
        {
            if (!CheckResources(building.cost))
            {
                Debug.Log($"Недостаточно ресурсов для {building.buildingName}! Нужно: {building.cost}");
                return;
            }
        }
        
        Quaternion rotation = Quaternion.Euler(0, currentRotation, 0);
        Vector3 position = currentGridPosition + building.buildingOffset;
        GameObject buildingObj = Instantiate(building.prefab, position, rotation);
        buildingObj.name = $"{building.buildingName}_{placedBuildings.Count}";
        
        ActivateBuilding(buildingObj);
        
        placedBuildings.Add(currentGridPosition);
        buildingObjects.Add(buildingObj);
        
        if (useResources && building.cost > 0)
            SpendResources(building.cost);
        
        Debug.Log($"{building.buildingName} размещено в позиции: {currentGridPosition}");
        
        OnBuildingPlaced?.Invoke(buildingObj, currentGridPosition, selectedBuildingIndex);
        
        if (previewBuilding != null)
            previewBuilding.SetActive(false);
        
        UpdateStatusText();
    }
    
    private void ActivateBuilding(GameObject buildingObj)
    {
        foreach (MonoBehaviour script in buildingObj.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = true;
        
        foreach (Rigidbody rb in buildingObj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.detectCollisions = true;
        }
        
        foreach (Collider col in buildingObj.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
            col.isTrigger = true;
        }
        
        foreach (Animator anim in buildingObj.GetComponentsInChildren<Animator>())
            anim.enabled = true;
        
        foreach (ParticleSystem ps in buildingObj.GetComponentsInChildren<ParticleSystem>())
        {
            var emission = ps.emission;
            emission.enabled = true;
            ps.Play();
        }
        
        foreach (AudioSource audio in buildingObj.GetComponentsInChildren<AudioSource>())
            audio.enabled = true;
        
        foreach (Renderer renderer in buildingObj.GetComponentsInChildren<Renderer>())
        {
            Color color = renderer.material.color;
            color.a = 1f;
            renderer.material.color = color;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        
        buildingObj.tag = "Building";
    }
    
    private void CancelBuildMode()
    {
        // Если рисовали стену - отменяем рисование
        if (isDrawingWall)
        {
            ClearWallPreviews();
            isDrawingWall = false;
            hasSinglePreview = false;
        }
        
        isBuildMode = false;
        
        // Обновляем состояние кнопок
        if (selectedBuildingIndex >= 0 && selectedBuildingIndex < buildings.Count)
        {
            UpdateButtonState(selectedBuildingIndex);
        }
        
        selectedBuildingIndex = -1;
        
        if (previewBuilding != null)
        {
            Destroy(previewBuilding);
            previewBuilding = null;
        }
        
        // Очищаем все превью стен
        ClearWallPreviews();
        
        Debug.Log($"Режим строительства отменен");
        OnBuildModeDeactivated?.Invoke();
        UpdateStatusText();
    }
    
    public void ToggleBuildMode()
    {
        if (isBuildMode)
            CancelBuildMode();
        else
        {
            if (buildings.Count > 0 && buildings[0].prefab != null)
                SelectBuilding(0);
            else
                Debug.LogWarning("Нет доступных зданий для строительства");
        }
    }
    
    #endregion
    
    #region UI и статус
    
    private void UpdateButtonState(int index)
    {
        BuildingData building = buildings[index];
        if (building.uiButton == null) return;
        
        ColorBlock colors = building.uiButton.colors;
        bool isSelected = (index == selectedBuildingIndex && isBuildMode);
        
        colors.normalColor = isSelected ? building.selectedColor : building.normalColor;
        colors.highlightedColor = isSelected ? building.selectedColor : building.normalColor * 1.2f;
        colors.pressedColor = isSelected ? building.selectedColor : building.normalColor * 0.8f;
        
        building.uiButton.colors = colors;
        
        if (building.buttonImage != null)
        {
            Outline outline = building.buttonImage.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = isSelected ? building.selectedColor : Color.clear;
                outline.effectDistance = isSelected ? new Vector2(2, 2) : Vector2.zero;
            }
        }
    }
    
    private void UpdateAllButtons()
    {
        for (int i = 0; i < buildings.Count; i++)
            UpdateButtonState(i);
    }
    
    private void UpdateStatusText()
    {
        if (statusText == null) return;
        
        if (isDrawingWall)
        {
            statusText.text = $"Рисование стены\n" +
                             $"Начало: {wallStartPosition}\n" +
                             $"Конец: {wallEndPosition}\n" +
                             $"Стен: {wallPreviews.Count}\n" +
                             $"ЛКМ - подтвердить, ESC - отмена рисования";
            statusText.color = wallPreviewColor;
            return;
        }
        
        if (isBuildMode && selectedBuildingIndex >= 0 && selectedBuildingIndex < buildings.Count)
        {
            BuildingData building = buildings[selectedBuildingIndex];
            string costText = useResources ? $" {building.cost}" : "";
            string rotationText = building.allowRotation ? "ПКМ - повернуть" : "";
            string wallText = building.isWall ? "РЕЖИМ СТЕНЫ: ЛКМ - начать/закончить" : "";
            
            statusText.text = $"Строительство: {building.buildingName} {costText}\n" +
                             $"{wallText}\n" +
                             $"ЛКМ - построить, ESC - выйти, {rotationText}\n" +
                             $"Горячие клавиши: ";
            
            for (int i = 0; i < buildings.Count; i++)
            {
                if (i < buildings.Count && buildings[i].prefab != null)
                {
                    statusText.text += $"[{buildings[i].hotkey}] {buildings[i].buildingName} ";
                }
            }
            statusText.color = validPlacementColor;
        }
        else
        {
            statusText.text = $"Нажмите B или выберите здание по хоткею\n" +
                             $"Горячие клавиши: ";
            
            for (int i = 0; i < buildings.Count; i++)
            {
                if (i < buildings.Count && buildings[i].prefab != null)
                {
                    statusText.text += $"[{buildings[i].hotkey}] {buildings[i].buildingName} ";
                }
            }
            statusText.color = Color.white;
        }
    }
    
    #endregion
    
    #region Ресурсы
    
    private bool CheckResources(int cost) { return true; }
    private void SpendResources(int cost) { }
    
    #endregion
    
    #region Визуализация
    
    private void OnDrawGizmos()
    {
        if (!showGrid || !isBuildMode) return;
        
        Gizmos.color = isPositionValid ? validPlacementColor : invalidPlacementColor;
        Gizmos.DrawWireCube(currentGridPosition, Vector3.one * gridSize);
        
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        for (int x = -5; x <= 5; x++)
        {
            for (int z = -5; z <= 5; z++)
            {
                Vector3 pos = new Vector3(
                    Mathf.Round(currentGridPosition.x / gridSize + x) * gridSize,
                    currentGridPosition.y,
                    Mathf.Round(currentGridPosition.z / gridSize + z) * gridSize
                );
                Gizmos.DrawWireCube(pos, Vector3.one * gridSize * 0.9f);
            }
        }
    }
    
    #endregion
    
    #region Публичные методы
    
    public void AddBuilding(BuildingData building)
    {
        buildings.Add(building);
        SetupButtons();
        UpdateAllButtons();
    }
    
    public void RemoveBuilding(int index)
    {
        if (index < 0 || index >= buildings.Count) return;
        buildings.RemoveAt(index);
        if (selectedBuildingIndex == index)
            CancelBuildMode();
        UpdateAllButtons();
    }
    
    public BuildingData GetSelectedBuilding()
    {
        if (selectedBuildingIndex >= 0 && selectedBuildingIndex < buildings.Count)
            return buildings[selectedBuildingIndex];
        return null;
    }
    
    public int GetSelectedBuildingIndex() => selectedBuildingIndex;
    public bool IsBuildModeActive() => isBuildMode;
    public bool IsDrawingWall() => isDrawingWall;
    public int GetBuildingCount() => placedBuildings.Count;
    
    public List<GameObject> GetBuildings() => new List<GameObject>(buildingObjects);
    public List<Vector3> GetBuildingPositions() => new List<Vector3>(placedBuildings);
    
    public void ClearAllBuildings()
    {
        foreach (GameObject building in buildingObjects)
            Destroy(building);
        buildingObjects.Clear();
        placedBuildings.Clear();
    }
    
    public void RemoveBuilding(GameObject building)
    {
        if (buildingObjects.Contains(building))
        {
            int index = buildingObjects.IndexOf(building);
            buildingObjects.RemoveAt(index);
            placedBuildings.RemoveAt(index);
            Destroy(building);
        }
    }
    
    #endregion
}