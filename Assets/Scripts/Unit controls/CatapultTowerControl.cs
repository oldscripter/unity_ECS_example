using UnityEngine;
using System.Collections.Generic;

public class BallisticThrow : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private Transform targetPoint;      // Целевая точка
    [SerializeField] private GameObject projectilePrefab; // Префаб снаряда
    [SerializeField] private Transform launchPoint;      // Точка запуска
    [SerializeField] private Transform barrelPivot;      // Вращающийся ствол (X-поворот)
    
    [Header("Параметры броска")]
    [SerializeField] private float launchAngle = 45f;    // Угол броска в градусах
    [SerializeField] private bool useOptimalAngle = true; // Использовать оптимальный угол
    [SerializeField] private float minAngle = 10f;       // Минимальный угол
    [SerializeField] private float maxAngle = 80f;       // Максимальный угол
    [SerializeField] private float barrelRotationSpeed = 5f; // Скорость поворота ствола
    
    [Header("Автоматическая стрельба")]
    [SerializeField] private bool autoShoot = false;     // Включить автоматическую стрельбу
    [SerializeField] private float fireRate = 1f;        // Скорострельность (выстрелов в секунду)
    [SerializeField] private float detectionRange = 50f; // Радиус обнаружения
    [SerializeField] private LayerMask enemyLayerMask;   // Слой врагов
    [SerializeField] private bool shootOnlyWhenTargetVisible = true; // Стрелять только при видимости
    [SerializeField] private float targetUpdateInterval = 0.5f; // Интервал обновления цели
    
    [Header("Настройки прицеливания")]
    [SerializeField] private bool aimAtCenterOfMass = true; // Целиться в центр массы
    [SerializeField] private Vector3 aimOffset = Vector3.zero; // Смещение прицеливания
    [SerializeField] private float predictionTime = 0.5f; // Время предсказания движения (сек)
    [SerializeField] private bool usePrediction = false;  // Использовать предсказание
    
    [Header("Настройки поворота башни")]
    [SerializeField] private Transform towerPivot;        // Вращающаяся башня (Y-поворот)
    [SerializeField] private float towerRotationSpeed = 5f; // Скорость поворота башни
    
    [Header("Отладка")]
    [SerializeField] private bool showTrajectory = true;
    [SerializeField] private int trajectoryPoints = 40;
    [SerializeField] private Color trajectoryColor = Color.yellow;
    [SerializeField] private bool showDetectionRange = true;
    [SerializeField] private Color detectionRangeColor = new Color(0, 1, 0, 0.2f);
    [SerializeField] private bool showDebugInfo = true;

    // Приватные переменные
    private Rigidbody rb;
    private GameObject currentProjectile;
    private float nextFireTime = 0f;
    private Transform currentTarget = null;
    private float targetUpdateTimer = 0f;
    private List<Enemy> enemiesInRange = new List<Enemy>();
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private float currentBarrelAngle = 0f;
    private Quaternion targetTowerRotation;

    private void Start()
    {
        // Настраиваем слой врагов
        if (enemyLayerMask == 0)
        {
            enemyLayerMask = LayerMask.GetMask("Default");
        }

        // Если нет точки запуска, используем текущий объект
        if (launchPoint == null)
        {
            launchPoint = transform;
        }

        // Если нет ствола, используем точку запуска
        if (barrelPivot == null)
        {
            barrelPivot = launchPoint;
        }

        // Устанавливаем начальный угол ствола
        currentBarrelAngle = launchAngle;
        SetBarrelAngle(currentBarrelAngle);

        // Сохраняем начальный поворот башни
        if (towerPivot != null)
        {
            targetTowerRotation = towerPivot.rotation;
        }
    }

    private void Update()
    {
        // Ручной бросок по пробелу
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ThrowProjectile();
        }
        
        // Ручной бросок по ЛКМ
        if (Input.GetMouseButtonDown(0))
        {
            ThrowProjectile();
        }
        
        // Переключение режима по клавише A
        if (Input.GetKeyDown(KeyCode.A))
        {
            autoShoot = !autoShoot;
            Debug.Log($"🔄 Автоматический режим: {(autoShoot ? "ВКЛ" : "ВЫКЛ")}");
        }

        // Автоматическая стрельба
        if (autoShoot)
        {
            UpdateTarget();
            
            // Поворачиваем башню к цели
            if (currentTarget != null)
            {
                RotateTowerToTarget(currentTarget);
            }
            
            // Обновляем угол ствола
            UpdateBarrelAngle();
            
            // Стрельба
            if (currentTarget != null && Time.time >= nextFireTime)
            {
                Vector3 targetPosition = GetTargetPosition(currentTarget);
                
                if (!shootOnlyWhenTargetVisible || IsTargetVisible(currentTarget))
                {
                    ThrowProjectile(targetPosition);
                    nextFireTime = Time.time + (1f / fireRate);
                }
            }
        }
        else
        {
            // В ручном режиме обновляем угол ствола по колесику мыши
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0 && !useOptimalAngle)
            {
                launchAngle = Mathf.Clamp(launchAngle + scroll * 10f, minAngle, maxAngle);
                if (showDebugInfo)
                    Debug.Log($"🎯 Угол изменен: {launchAngle:F1}°");
            }
            
            // Плавно поворачиваем ствол к текущему углу
            UpdateBarrelAngle();
        }
        
        // Вращение башни с помощью мыши (ручной режим)
        if (!autoShoot && towerPivot != null)
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (Mathf.Abs(mouseX) > 0.01f)
            {
                towerPivot.Rotate(Vector3.up, mouseX * towerRotationSpeed * 10f * Time.deltaTime);
                targetTowerRotation = towerPivot.rotation;
            }
        }
    }

    /// <summary>
    /// Поворачивает башню к цели
    /// </summary>
    private void RotateTowerToTarget(Transform target)
    {
        if (towerPivot == null || target == null) return;
        
        Vector3 directionToTarget = target.position - towerPivot.position;
        Vector3 horizontalDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z);
        
        if (horizontalDirection.magnitude > 0.1f)
        {
            targetTowerRotation = Quaternion.LookRotation(horizontalDirection, Vector3.up);
            
            // Плавный поворот
            towerPivot.rotation = Quaternion.Slerp(
                towerPivot.rotation,
                targetTowerRotation,
                Time.deltaTime * towerRotationSpeed
            );
        }
    }

    /// <summary>
    /// Обновляет угол ствола
    /// </summary>
    private void UpdateBarrelAngle()
    {
        float targetAngle = launchAngle;
        
        // Если есть цель и включена оптимизация, используем оптимальный угол
        if (autoShoot && currentTarget != null && useOptimalAngle)
        {
            Vector3 targetPos = GetTargetPosition(currentTarget);
            float optimalAngle = CalculateOptimalAngle(launchPoint.position, targetPos);
            targetAngle = Mathf.Clamp(optimalAngle, minAngle, maxAngle);
        }
        else if (autoShoot && currentTarget != null)
        {
            // Используем угол, рассчитанный для попадания
            Vector3 targetPos = GetTargetPosition(currentTarget);
            float requiredSpeed = CalculateRequiredSpeed(launchPoint.position, targetPos, launchAngle);
            
            // Если цель недостижима с текущим углом, корректируем угол
            if (float.IsNaN(requiredSpeed) || float.IsInfinity(requiredSpeed))
            {
                float adjustedAngle = AdjustAngleForTarget(launchPoint.position, targetPos);
                if (!float.IsNaN(adjustedAngle))
                {
                    targetAngle = Mathf.Clamp(adjustedAngle, minAngle, maxAngle);
                }
            }
        }
        
        // Плавно поворачиваем ствол
        currentBarrelAngle = Mathf.Lerp(currentBarrelAngle, targetAngle, Time.deltaTime * barrelRotationSpeed);
        
        // Если разница маленькая, сразу устанавливаем точное значение
        if (Mathf.Abs(currentBarrelAngle - targetAngle) < 0.01f)
        {
            currentBarrelAngle = targetAngle;
        }
        
        SetBarrelAngle(currentBarrelAngle);
    }

    /// <summary>
    /// Устанавливает угол ствола
    /// </summary>
    private void SetBarrelAngle(float angleDeg)
    {
        if (barrelPivot == null) return;
        
        // Поворачиваем ствол вокруг оси X
        barrelPivot.localRotation = Quaternion.Euler(-angleDeg, 0f, 0f);
    }

    /// <summary>
    /// Вычисляет оптимальный угол для попадания в цель
    /// </summary>
    private float CalculateOptimalAngle(Vector3 start, Vector3 target)
    {
        Vector3 toTarget = target - start;
        float horizontalDistance = new Vector3(toTarget.x, 0, toTarget.z).magnitude;
        float verticalDistance = toTarget.y;
        
        if (horizontalDistance < 0.01f)
        {
            return 90f;
        }
        
        float g = Mathf.Abs(Physics.gravity.y);
        
        // Оптимальный угол для минимальной скорости
        // θ = 45° + 0.5 * arcsin(h / sqrt(d² + h²))
        float angle = 45f * Mathf.Deg2Rad + 0.5f * Mathf.Asin(verticalDistance / Mathf.Sqrt(horizontalDistance * horizontalDistance + verticalDistance * verticalDistance));
        
        return angle * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Корректирует угол для достижения цели
    /// </summary>
    private float AdjustAngleForTarget(Vector3 start, Vector3 target)
    {
        float bestAngle = launchAngle;
        float bestError = float.MaxValue;
        
        // Проверяем углы от min до max с шагом 1 градус
        for (float angle = minAngle; angle <= maxAngle; angle += 1f)
        {
            float speed = CalculateRequiredSpeed(start, target, angle);
            if (!float.IsNaN(speed) && !float.IsInfinity(speed) && speed > 0)
            {
                // Проверяем, попадает ли снаряд в цель
                float flightTime = CalculateFlightTime(speed, angle * Mathf.Deg2Rad, start.y, target.y);
                if (flightTime > 0)
                {
                    Vector3 hitPoint = CalculateHitPoint(start, speed, angle * Mathf.Deg2Rad, flightTime);
                    float error = Vector3.Distance(hitPoint, target);
                    
                    if (error < bestError)
                    {
                        bestError = error;
                        bestAngle = angle;
                    }
                }
            }
        }
        
        return bestAngle;
    }

    /// <summary>
    /// Вычисляет точку попадания
    /// </summary>
    private Vector3 CalculateHitPoint(Vector3 start, float speed, float angleRad, float time)
    {
        Vector3 direction = CalculateLaunchDirection(start, start + Vector3.forward);
        Vector3 velocity = direction * speed * Mathf.Cos(angleRad) + Vector3.up * speed * Mathf.Sin(angleRad);
        return start + velocity * time + 0.5f * Physics.gravity * time * time;
    }

    /// <summary>
    /// Обновление цели
    /// </summary>
    private void UpdateTarget()
    {
        targetUpdateTimer += Time.deltaTime;
        
        if (targetUpdateTimer >= targetUpdateInterval)
        {
            targetUpdateTimer = 0f;
            
            Transform newTarget = FindNearestEnemy();
            
            if (newTarget != currentTarget)
            {
                currentTarget = newTarget;
                if (showDebugInfo && currentTarget != null)
                {
                    Debug.Log($"🎯 Новая цель: {currentTarget.name}");
                }
            }
            
            if (currentTarget != null)
            {
                Vector3 currentPos = GetTargetPosition(currentTarget);
                targetVelocity = (currentPos - lastTargetPosition) / targetUpdateInterval;
                lastTargetPosition = currentPos;
            }
        }
    }

    /// <summary>
    /// Находит ближайшего врага в радиусе
    /// </summary>
    private Transform FindNearestEnemy()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, enemyLayerMask);
        
        Transform nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider collider in hitColliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead())
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                
                if (!shootOnlyWhenTargetVisible || IsTargetVisible(collider.transform))
                {
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = collider.transform;
                    }
                }
            }
        }
        
        return nearestEnemy;
    }

    /// <summary>
    /// Проверяет видимость цели
    /// </summary>
    private bool IsTargetVisible(Transform target)
    {
        if (target == null) return false;
        
        Vector3 startPoint = launchPoint != null ? launchPoint.position : transform.position;
        Vector3 endPoint = target.position + Vector3.up * 0.5f;
        
        RaycastHit hit;
        if (Physics.Raycast(startPoint, (endPoint - startPoint).normalized, out hit, detectionRange))
        {
            if (hit.transform == target || hit.transform.IsChildOf(target))
            {
                return true;
            }
            
            Enemy enemy = hit.transform.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead())
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Получает позицию для прицеливания
    /// </summary>
    private Vector3 GetTargetPosition(Transform target)
    {
        if (target == null) return Vector3.zero;
        
        Vector3 targetPos;
        
        if (aimAtCenterOfMass)
        {
            targetPos = target.position + Vector3.up * 1.2f;
        }
        else
        {
            targetPos = target.position;
        }
        
        targetPos += aimOffset;
        
        if (usePrediction)
        {
            targetPos += targetVelocity * predictionTime;
        }
        
        return targetPos;
    }

    /// <summary>
    /// Запустить снаряд в текущую цель
    /// </summary>
    public void ThrowProjectile()
    {
        if (currentTarget != null)
        {
            Vector3 targetPos = GetTargetPosition(currentTarget);
            ThrowProjectile(targetPos);
        }
        else
        {
            Vector3 defaultTarget = launchPoint.position + launchPoint.forward * 30f + Vector3.up * 2f;
            ThrowProjectile(defaultTarget);
        }
    }

    /// <summary>
    /// Запустить снаряд в указанную точку
    /// </summary>
    public void ThrowProjectile(Vector3 targetPosition)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("❌ Не назначен префаб снаряда!");
            return;
        }

        if (launchPoint == null)
        {
            launchPoint = transform;
        }

        // Создаем снаряд
        currentProjectile = Instantiate(projectilePrefab, launchPoint.position, launchPoint.rotation);
        
        // Получаем или добавляем Rigidbody
        rb = currentProjectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = currentProjectile.AddComponent<Rigidbody>();
        }
        
        // Настраиваем физику
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Вычисляем необходимую скорость
        float calculatedSpeed = CalculateRequiredSpeed(
            launchPoint.position, 
            targetPosition, 
            launchAngle
        );

        if (float.IsNaN(calculatedSpeed) || float.IsInfinity(calculatedSpeed) || calculatedSpeed < 0.01f)
        {
            Debug.LogWarning("⚠️ Цель недостижима! Использую стандартную скорость.");
            calculatedSpeed = 30f;
        }

        // Получаем направление
        Vector3 direction = CalculateLaunchDirection(launchPoint.position, targetPosition);
        
        // Вычисляем вектор скорости
        float angleRad = launchAngle * Mathf.Deg2Rad;
        Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z).normalized;
        
        float vx = calculatedSpeed * Mathf.Cos(angleRad);
        float vy = calculatedSpeed * Mathf.Sin(angleRad);
        
        Vector3 velocity = horizontalDir * vx + Vector3.up * vy;
        
        // Применяем скорость
        rb.linearVelocity = velocity;
        
        // Логирование
        if (showDebugInfo)
        {
            float distance = Vector3.Distance(launchPoint.position, targetPosition);
            float flightTime = CalculateFlightTime(calculatedSpeed, angleRad, launchPoint.position.y, targetPosition.y);
            
            Debug.Log($"🚀 Снаряд запущен!");
            Debug.Log($"   🎯 Цель: {targetPosition}");
            Debug.Log($"   📏 Расстояние: {distance:F1}м");
            Debug.Log($"   📐 Угол: {launchAngle:F1}°");
            Debug.Log($"   ⚡ Скорость: {calculatedSpeed:F1} м/с");
            Debug.Log($"   ⏱️ Время полета: {flightTime:F2}с");
        }

        // Автоматическое уничтожение через время
        float destroyTime = 10f;
        Destroy(currentProjectile, destroyTime);
    }

    /// <summary>
    /// Вычисляет необходимую скорость для попадания в цель
    /// </summary>
    private float CalculateRequiredSpeed(Vector3 start, Vector3 target, float angleDeg)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector3 toTarget = target - start;
        Vector3 horizontal = new Vector3(toTarget.x, 0, toTarget.z);
        float horizontalDistance = horizontal.magnitude;
        float verticalDistance = toTarget.y;
        
        if (horizontalDistance < 0.01f)
        {
            if (verticalDistance > 0)
            {
                return Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * verticalDistance);
            }
            else
            {
                return 0.1f;
            }
        }
        
        float g = Mathf.Abs(Physics.gravity.y);
        float cos = Mathf.Cos(angleRad);
        float tan = Mathf.Tan(angleRad);
        
        float denominator = 2 * cos * cos * (horizontalDistance * tan - verticalDistance);
        
        if (denominator <= 0)
        {
            return float.NaN;
        }
        
        float speedSquared = (g * horizontalDistance * horizontalDistance) / denominator;
        
        if (speedSquared < 0)
        {
            return float.NaN;
        }
        
        return Mathf.Sqrt(speedSquared);
    }

    /// <summary>
    /// Вычисляет направление к цели в горизонтальной плоскости
    /// </summary>
    private Vector3 CalculateLaunchDirection(Vector3 start, Vector3 target)
    {
        Vector3 toTarget = target - start;
        Vector3 horizontal = new Vector3(toTarget.x, 0, toTarget.z);
        
        if (horizontal.magnitude < 0.01f)
        {
            return Vector3.forward;
        }
        
        return horizontal.normalized;
    }

    /// <summary>
    /// Вычисляет время полета снаряда
    /// </summary>
    private float CalculateFlightTime(float speed, float angleRad, float startY, float targetY)
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float vy = speed * Mathf.Sin(angleRad);
        
        float a = -0.5f * g;
        float b = vy;
        float c = startY - targetY;
        
        float discriminant = b * b - 4 * a * c;
        
        if (discriminant < 0)
        {
            return 0;
        }
        
        float sqrtDisc = Mathf.Sqrt(discriminant);
        float t1 = (-b + sqrtDisc) / (2 * a);
        float t2 = (-b - sqrtDisc) / (2 * a);
        
        float time = Mathf.Max(t1, t2);
        if (time < 0)
        {
            time = Mathf.Min(t1, t2);
        }
        
        return Mathf.Abs(time);
    }

    // Визуализация в Scene View
    private void OnDrawGizmos()
    {
        // Радиус обнаружения
        if (showDetectionRange)
        {
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawSphere(transform.position, detectionRange);
            
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
        
        // Траектория
        if (showTrajectory)
        {
            Vector3 start = launchPoint != null ? launchPoint.position : transform.position;
            Vector3 target = targetPoint != null ? targetPoint.position : start + transform.forward * 20f;
            
            if (currentTarget != null && autoShoot)
            {
                target = GetTargetPosition(currentTarget);
            }
            
            DrawTrajectory(start, target);
            
            // Рисуем цель
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target, 0.5f);
            
            if (currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(start, target);
            }
        }
        
        // Точка запуска
        if (launchPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(launchPoint.position, 0.3f);
        }
        
        // Ствол
        if (barrelPivot != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(barrelPivot.position, barrelPivot.forward * 3f);
        }
    }

    private void DrawTrajectory(Vector3 start, Vector3 target)
    {
        float speed = CalculateRequiredSpeed(start, target, launchAngle);
        
        if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0.01f)
        {
            DrawSimpleTrajectory(start, target);
            return;
        }
        
        float angleRad = launchAngle * Mathf.Deg2Rad;
        Vector3 direction = CalculateLaunchDirection(start, target);
        float g = Mathf.Abs(Physics.gravity.y);
        
        Vector3 velocity = direction * speed * Mathf.Cos(angleRad) + Vector3.up * speed * Mathf.Sin(angleRad);
        
        Gizmos.color = trajectoryColor;
        Vector3 prevPoint = start;
        
        for (int i = 1; i <= trajectoryPoints; i++)
        {
            float t = (i / (float)trajectoryPoints) * 10f;
            Vector3 point = start + velocity * t + 0.5f * Physics.gravity * t * t;
            
            if (point.y < -10f) break;
            
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
            
            if (i % 5 == 0)
            {
                Gizmos.DrawWireSphere(point, 0.1f);
            }
        }
    }

    private void DrawSimpleTrajectory(Vector3 start, Vector3 target)
    {
        Gizmos.color = Color.gray;
        Vector3 mid = Vector3.Lerp(start, target, 0.5f);
        mid.y += 5f;
        
        Gizmos.DrawLine(start, mid);
        Gizmos.DrawLine(mid, target);
    }

    // Публичные методы

    public void SetTarget(Vector3 targetPosition)
    {
        if (targetPoint == null)
        {
            GameObject targetObj = new GameObject("Target");
            targetObj.transform.position = targetPosition;
            targetPoint = targetObj.transform;
        }
        else
        {
            targetPoint.position = targetPosition;
        }
    }

    public void SetTarget(Transform target)
    {
        targetPoint = target;
    }

    public void SetLaunchAngle(float angle)
    {
        launchAngle = Mathf.Clamp(angle, minAngle, maxAngle);
    }

    public void ToggleAutoShoot()
    {
        autoShoot = !autoShoot;
        Debug.Log($"🔄 Автоматический режим: {(autoShoot ? "ВКЛ" : "ВЫКЛ")}");
    }

    public void SetAutoShoot(bool enabled)
    {
        autoShoot = enabled;
        if (!enabled)
        {
            currentTarget = null;
        }
    }

    public void SetFireRate(float rate)
    {
        fireRate = Mathf.Clamp(rate, 0.1f, 10f);
    }

    public void SetDetectionRange(float range)
    {
        detectionRange = Mathf.Clamp(range, 1f, 200f);
    }

    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }

    public bool HasTarget()
    {
        return currentTarget != null;
    }

    public float GetCurrentBarrelAngle()
    {
        return currentBarrelAngle;
    }

    // Отображение информации на экране
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        float yPos = 10f;
        
        GUI.Label(new Rect(10, yPos, 300, 20), $"🎯 Режим: {(autoShoot ? "АВТОМАТ" : "РУЧНОЙ")}");
        yPos += 20f;
        
        if (autoShoot)
        {
            GUI.Label(new Rect(10, yPos, 300, 20), $"👾 Цель: {(currentTarget != null ? currentTarget.name : "Не найдена")}");
            yPos += 20f;
            
            if (currentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, currentTarget.position);
                GUI.Label(new Rect(10, yPos, 300, 20), $"📏 Дистанция: {distance:F1}м");
                yPos += 20f;
            }
            
            GUI.Label(new Rect(10, yPos, 300, 20), $"⚡ Скорострельность: {fireRate:F1} выстр/сек");
            yPos += 20f;
        }
        
        GUI.Label(new Rect(10, yPos, 300, 20), $"📐 Угол ствола: {currentBarrelAngle:F1}°");
        yPos += 20f;
        
        GUI.Label(new Rect(10, yPos, 300, 20), $"🎯 Заданный угол: {launchAngle:F1}°");
        yPos += 20f;
        
        // Клавиши управления
        GUI.Label(new Rect(10, Screen.height - 80, 300, 20), "Space/ЛКМ - Выстрел");
        GUI.Label(new Rect(10, Screen.height - 60, 300, 20), "A - Переключить авт. режим");
        GUI.Label(new Rect(10, Screen.height - 40, 300, 20), "Колесо мыши - Изменить угол");
        GUI.Label(new Rect(10, Screen.height - 20, 300, 20), "Мышь - Поворот башни (ручной режим)");
    }
}