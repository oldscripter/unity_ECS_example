using UnityEngine;

public class RTSCamera : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float moveSpeedMultiplier = 2f;
    [SerializeField] private float edgeScrollSpeed = 15f;
    [SerializeField] private float edgeScrollBorder = 20f;
    [SerializeField] private bool enableEdgeScrolling = true;
    [SerializeField] private bool enableWASD = true;
    
    [Header("Настройки зума")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;
    [SerializeField] private float zoomSmoothness = 10f;
    
    [Header("Настройки поворота")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float minRotationAngle = 20f;
    [SerializeField] private float maxRotationAngle = 80f;
    [SerializeField] private bool enableRotation = true;
    
    [Header("Настройки ограничений")]
    [SerializeField] private bool enableBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);
    
    [Header("Настройки сглаживания")]
    [SerializeField] private float smoothness = 5f;
    [SerializeField] private float rotationSmoothness = 5f;
    [SerializeField] private bool useSmoothMovement = false;
    
    [Header("Стартовые настройки (для возврата)")]
    [SerializeField] private float startZoom = 30f;
    [SerializeField] private float startRotationX = 45f;
    [SerializeField] private float startRotationY = 0f;
    
    // Приватные переменные
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private float targetZoom;
    private float currentZoom;
    private float targetRotationX;
    private float currentRotationX;
    private float targetRotationY;
    private float currentRotationY;
    
    private Camera cam;
    private bool isDragging = false;
    private bool isCursorLocked = false;
    private bool isMiddleMouseButtonPressed = false;
    
    // Сохраняем стартовые значения
    private float savedStartZoom;
    private float savedStartRotationX;
    private float savedStartRotationY;
    
    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        savedStartZoom = startZoom;
        savedStartRotationX = startRotationX;
        savedStartRotationY = startRotationY;
        
        // Инициализация позиции
        targetPosition = transform.position;
        
        // Инициализация зума
        currentZoom = startZoom;
        targetZoom = startZoom;
        
        // Инициализация поворота
        currentRotationX = startRotationX;
        targetRotationX = startRotationX;
        currentRotationY = startRotationY;
        targetRotationY = startRotationY;
        
        transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        InvokeRepeating("EnsureCursorVisible", 0.1f, 0.5f);
    }
    
    private void Update()
    {
        UpdateMouseButtonStates();
        
        if (Input.GetKeyDown(KeyCode.Home))
        {
            ResetToStartSettings();
        }
        
        if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftControl))
        {
            ResetToStartSettings();
        }
        
        Vector3 inputDirection = GetInputDirection();
        Vector3 moveDirection = CalculateMovement(inputDirection);
        MoveCamera(moveDirection);
        
        HandleZoom();
        
        if (enableRotation)
        {
            HandleRotation();
        }
        
        ApplySmoothing();
    }
    
    private void UpdateMouseButtonStates()
    {
        isMiddleMouseButtonPressed = Input.GetMouseButton(2);
    }
    
    private bool CanUseEdgeScrolling()
    {
        if (!enableEdgeScrolling) return false;
        if (!Cursor.visible) return false;
        if (isMiddleMouseButtonPressed) return false;
        if (isDragging) return false;
        return true;
    }
    
    public void ResetToStartSettings()
    {
        targetZoom = savedStartZoom;
        targetRotationX = savedStartRotationX;
        targetRotationY = savedStartRotationY;
    }
    
    public void ResetToStartSettingsSmooth(float duration = 1f)
    {
        StartCoroutine(SmoothResetCoroutine(duration));
    }
    
    private System.Collections.IEnumerator SmoothResetCoroutine(float duration)
    {
        Vector3 startPos = transform.position;
        float startZoom = currentZoom;
        float startRotX = currentRotationX;
        float startRotY = currentRotationY;
        
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            currentZoom = Mathf.Lerp(startZoom, savedStartZoom, smoothProgress);
            targetZoom = currentZoom;
            
            currentRotationX = Mathf.Lerp(startRotX, savedStartRotationX, smoothProgress);
            currentRotationY = Mathf.Lerp(startRotY, savedStartRotationY, smoothProgress);
            targetRotationX = currentRotationX;
            targetRotationY = currentRotationY;
            
            transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);
            
            yield return null;
        }
        
        currentZoom = savedStartZoom;
        targetZoom = savedStartZoom;
        currentRotationX = savedStartRotationX;
        targetRotationX = savedStartRotationX;
        currentRotationY = savedStartRotationY;
        targetRotationY = savedStartRotationY;
        transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0f);
    }
    
    public void SetCurrentAsStartSettings()
    {
        savedStartZoom = currentZoom;
        savedStartRotationX = currentRotationX;
        savedStartRotationY = currentRotationY;
        
        startZoom = savedStartZoom;
        startRotationX = savedStartRotationX;
        startRotationY = savedStartRotationY;
    }
    
    private void ToggleCursorLock()
    {
        if (isCursorLocked)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            isCursorLocked = false;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            isCursorLocked = true;
        }
    }
    
    private void EnsureCursorVisible()
    {
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    private Vector3 GetInputDirection()
    {
        Vector3 input = Vector3.zero;
        
        if (enableWASD)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            input = new Vector3(horizontal, 0, vertical);
        }
        
        if (CanUseEdgeScrolling())
        {
            Vector3 edgeInput = GetEdgeScrollInput();
            input += edgeInput;
        }
        
        if (input.magnitude > 1f)
        {
            input.Normalize();
        }
        
        return input;
    }
    
    private Vector3 GetEdgeScrollInput()
    {
        Vector3 edgeInput = Vector3.zero;
        
        if (!Cursor.visible) return edgeInput;
        
        Vector3 mousePosition = Input.mousePosition;
        
        if (mousePosition.x < 0 || mousePosition.x > Screen.width ||
            mousePosition.y < 0 || mousePosition.y > Screen.height)
        {
            return edgeInput;
        }
        
        if (mousePosition.x < edgeScrollBorder)
        {
            edgeInput.x = -1f;
        }
        else if (mousePosition.x > Screen.width - edgeScrollBorder)
        {
            edgeInput.x = 1f;
        }
        
        if (mousePosition.y < edgeScrollBorder)
        {
            edgeInput.z = -1f;
        }
        else if (mousePosition.y > Screen.height - edgeScrollBorder)
        {
            edgeInput.z = 1f;
        }
        
        return edgeInput;
    }
    
    private Vector3 CalculateMovement(Vector3 inputDirection)
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        Vector3 moveDirection = (forward * inputDirection.z + right * inputDirection.x).normalized;
        
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= moveSpeedMultiplier;
        }
        
        return moveDirection * currentSpeed * Time.deltaTime;
    }
    
    private void MoveCamera(Vector3 moveDirection)
    {
        Vector3 newPosition = targetPosition + moveDirection;
        
        if (enableBounds)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
            newPosition.z = Mathf.Clamp(newPosition.z, minBounds.y, maxBounds.y);
        }
        
        targetPosition = newPosition;
    }
    
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetZoom -= scroll * zoomSpeed * 10f;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
        
        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl))
        {
            float mouseY = Input.GetAxis("Mouse Y");
            targetZoom -= mouseY * zoomSpeed * 0.5f;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }
    
    private void HandleRotation()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                targetRotationY += scroll * rotationSpeed * 2f;
            }
        }
        
        if (Input.GetKey(KeyCode.Q))
        {
            targetRotationY -= rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            targetRotationY += rotationSpeed * Time.deltaTime;
        }
        
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            targetRotationY += mouseX * rotationSpeed * 0.5f;
            
            float mouseY = Input.GetAxis("Mouse Y");
            targetRotationX -= mouseY * rotationSpeed * 0.3f;
            targetRotationX = Mathf.Clamp(targetRotationX, minRotationAngle, maxRotationAngle);
        }
    }
    
    private void ApplySmoothing()
    {
        // ДВИЖЕНИЕ
        if (!useSmoothMovement)
        {
            // Мгновенное применение позиции без сглаживания TODO: подумать на счет сглаживания
            Vector3 targetPosWithZoom = new Vector3(targetPosition.x, targetZoom, targetPosition.z);
            transform.position = targetPosWithZoom;
        }
        else
        {
            // Сглаженное движение
            Vector3 targetPosWithZoom = new Vector3(targetPosition.x, targetZoom, targetPosition.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosWithZoom, ref velocity, 1f / smoothness);
        }
        
        // ПОВОРОТ
        Quaternion targetRotation = Quaternion.Euler(targetRotationX, targetRotationY, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    // Визуализация границ в Scene View
    private void OnDrawGizmosSelected()
    {
        if (!enableBounds) return;
        
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) / 2f,
            0f,
            (minBounds.y + maxBounds.y) / 2f
        );
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            0f,
            maxBounds.y - minBounds.y
        );
        Gizmos.DrawWireCube(center, size);
    }
    
    // Публичные методы
    
    public void FocusOnObject(Vector3 position)
    {
        targetPosition = position;
    }
    
    public void SetBounds(Vector2 newMinBounds, Vector2 newMaxBounds)
    {
        minBounds = newMinBounds;
        maxBounds = newMaxBounds;
    }
    
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    public void SetZoomSpeed(float newSpeed)
    {
        zoomSpeed = newSpeed;
    }
    
    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        isCursorLocked = false;
    }
    
    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        isCursorLocked = true;
    }
}