using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurretController : MonoBehaviour
{
    [Header("Компоненты")]
    [SerializeField] private Transform towerPivot;      // Вращающаяся башня (Y-поворот)
    [SerializeField] private Transform platformPivot;   // Платформа с стволами (X-поворот для наклона)
    [SerializeField] private Transform[] firePoints;    // Массив точек вылета пуль (дула)

    [Header("Настройки мыши")]
    [SerializeField] private float horizontalSensitivity = 2f;
    [SerializeField] private float verticalSensitivity = 2f;

    [Header("Настройки наклона платформы")]
    [SerializeField] private float minAngle = -5f;
    [SerializeField] private float maxAngle = 5f;
    [SerializeField] private float currentAngle = 0f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Настройки стрельбы")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private int bulletsPerShot = 1;
    [SerializeField] private float spreadAngle = 5f;
    [SerializeField] private float bulletForce = 20f;

    [Header("Настройки отдачи стволов")]
    [SerializeField] private float recoilDistance = 0.3f;
    [SerializeField] private float recoilSpeed = 10f;
    [SerializeField] private float returnSpeed = 5f;

    [Header("Настройки звука")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private float pitchVariation = 0.1f; // Вариация тона

    [Header("Режим стрельбы")]
    [SerializeField] private bool alternateFirePoints = true;

    [Header("Автоматический режим")]
    [SerializeField] private bool autoMode = false;
    [SerializeField] private float detectionRange = 50f;
    [SerializeField] private float autoRotationSpeed = 3f;
    [SerializeField] private LayerMask enemyLayerMask;

    [Header("Визуализация")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showDetectionRange = true;
    [SerializeField] private Color detectionRangeColor = new Color(0f, 1f, 0f, 0.2f);
    [SerializeField] private Color detectionRangeWireColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private int rangeSegments = 32; // Количество сегментов для окружности

    

    // Данные каждого ствола
    private class BarrelData
    {
        public Transform firePoint;
        public Vector3 originalPosition;
        public bool isRecoiling;
        public float recoilProgress;
    }

    private BarrelData[] barrels;
    private int currentFirePointIndex = 0;
    private float nextFireTime = 0f;
    private float targetAngle = 0f;
    private Transform currentTarget = null;
    private float targetSwitchTimer = 0f;

    private void Start()
    {
        // Устанавливаем начальный угол
        currentAngle = 0f;
        targetAngle = 0f;
        SetPlatformAngle(0f);

        // Инициализируем данные стволов
        InitializeBarrels();

        // Настраиваем AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        // Если не задан слой врагов, ищем по тегу
        if (enemyLayerMask == 0)
        {
            enemyLayerMask = LayerMask.GetMask("Default");
        }
    }

    private void InitializeBarrels()
    {
        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogWarning("FirePoints не назначены!");
            return;
        }

        barrels = new BarrelData[firePoints.Length];
        
        for (int i = 0; i < firePoints.Length; i++)
        {
            barrels[i] = new BarrelData
            {
                firePoint = firePoints[i],
                originalPosition = firePoints[i].localPosition,
                isRecoiling = false,
                recoilProgress = 0f
            };
        }
    }

    private void Update()
    {
        if (autoMode)
        {
            // --- Автоматический режим ---
            UpdateAutoMode();
            
            // В авторежиме стрельба автоматическая
            if (currentTarget != null && Time.time >= nextFireTime)
            {
                FireBullet();
                nextFireTime = Time.time + (1f / fireRate);
            }
        }
        else
        {
            // --- Ручной режим ---
            // Поворот башни (горизонтальный)
            float mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity;
            towerPivot.Rotate(Vector3.up, mouseX);

            // Наклон платформы (вертикальный)
            float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity;
            targetAngle -= mouseY;
            targetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);
            
            // Плавный поворот платформы
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * rotationSpeed);
            if (Mathf.Abs(currentAngle - targetAngle) < 0.01f)
            {
                currentAngle = targetAngle;
            }
            SetPlatformAngle(currentAngle);

            // Стрельба при зажатой ЛКМ
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                FireBullet();
                nextFireTime = Time.time + (1f / fireRate);
            }
        }

        // Обновляем отдачу всех стволов (всегда)
        UpdateRecoil();

        // Отладочная информация
        if (showDebugInfo && autoMode)
        {
            Debug.DrawLine(transform.position, 
                transform.position + transform.forward * detectionRange, 
                currentTarget != null ? Color.green : Color.yellow);
            
            if (currentTarget != null)
            {
                Debug.DrawLine(transform.position, currentTarget.position, Color.red);
            }
        }
    }

    private void UpdateAutoMode()
    {
        // Поиск ближайшего врага
        FindNearestEnemy();

        if (currentTarget != null)
        {
            // Вычисляем направление на цель
            Vector3 directionToTarget = (currentTarget.position - towerPivot.position).normalized;
            
            // --- Поворот башни (горизонтальный) ---
            // Проекция направления на горизонтальную плоскость
            Vector3 horizontalDirection = Vector3.ProjectOnPlane(directionToTarget, Vector3.up);
            
            if (horizontalDirection.magnitude > 0.01f)
            {
                // Вычисляем целевой поворот
                Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection, Vector3.up);
                
                // Плавно поворачиваем башню
                towerPivot.rotation = Quaternion.Slerp(
                    towerPivot.rotation,
                    targetRotation,
                    Time.deltaTime * autoRotationSpeed
                );
            }

            // --- Наклон платформы (вертикальный) ---
            // Вычисляем угол между направлением на цель и горизонталью
            Vector3 localTarget = towerPivot.InverseTransformDirection(directionToTarget);
            float targetVerticalAngle = -Mathf.Atan2(localTarget.y, localTarget.z) * Mathf.Rad2Deg;
            
            // Ограничиваем угол
            targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, minAngle, maxAngle);
            
            // Плавно поворачиваем платформу
            currentAngle = Mathf.Lerp(currentAngle, targetVerticalAngle, Time.deltaTime * autoRotationSpeed * 0.5f);
            
            if (Mathf.Abs(currentAngle - targetVerticalAngle) < 0.1f)
            {
                currentAngle = targetVerticalAngle;
            }
            SetPlatformAngle(currentAngle);
        }
    }

    private void FindNearestEnemy()
    {
        // Поиск всех врагов в радиусе
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, enemyLayerMask);
        
        Transform nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider collider in hitColliders)
        {
            // Проверяем, есть ли у объекта компонент Enemy
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead())
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                
                // Проверяем, видим ли врага (проверка на препятствия)
                if (IsTargetVisible(collider.transform))
                {
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = collider.transform;
                    }
                }
            }
        }

        // Обновляем цель
        currentTarget = nearestEnemy;
    }

    private bool IsTargetVisible(Transform target)
    {
        // Проверка прямой видимости (луч из центра башни)
        Vector3 startPoint = towerPivot.position;
        Vector3 endPoint = target.position;
        
        RaycastHit hit;
        if (Physics.Raycast(startPoint, (endPoint - startPoint).normalized, out hit, detectionRange))
        {
            // Если луч попал в цель или её часть
            if (hit.transform == target || hit.transform.IsChildOf(target))
            {
                return true;
            }
            
            // Если луч попал в другой объект - цель не видна
            return false;
        }
        
        return false;
    }

    private void SetPlatformAngle(float angleDeg)
    {
        platformPivot.localRotation = Quaternion.Euler(angleDeg, 0f, 0f);
    }

    private void UpdateRecoil()
    {
        if (barrels == null) return;

        foreach (BarrelData barrel in barrels)
        {
            if (barrel.isRecoiling)
            {
                barrel.recoilProgress += Time.deltaTime * recoilSpeed;
                
                float progress = Mathf.Clamp01(barrel.recoilProgress);
                float offset = -recoilDistance * progress;
                
                barrel.firePoint.localPosition = barrel.originalPosition + new Vector3(0, 0, offset);
                
                if (progress >= 1f)
                {
                    barrel.isRecoiling = false;
                }
            }
            else
            {
                if (barrel.firePoint.localPosition != barrel.originalPosition)
                {
                    barrel.firePoint.localPosition = Vector3.Lerp(
                        barrel.firePoint.localPosition, 
                        barrel.originalPosition, 
                        Time.deltaTime * returnSpeed
                    );
                    
                    if (Vector3.Distance(barrel.firePoint.localPosition, barrel.originalPosition) < 0.001f)
                    {
                        barrel.firePoint.localPosition = barrel.originalPosition;
                    }
                }
            }
        }
    }

    private void FireBullet()
    {
        if (bulletPrefab == null || firePoints == null || firePoints.Length == 0)
        {
            Debug.LogWarning("BulletPrefab или FirePoints не назначены!");
            return;
        }

        PlayShootSound();

        int[] indicesToFire;
        
        if (alternateFirePoints)
        {
            indicesToFire = new int[] { currentFirePointIndex };
            currentFirePointIndex = (currentFirePointIndex + 1) % firePoints.Length;
        }
        else
        {
            indicesToFire = new int[firePoints.Length];
            for (int i = 0; i < firePoints.Length; i++)
            {
                indicesToFire[i] = i;
            }
        }

        foreach (int index in indicesToFire)
        {
            Transform firePoint = firePoints[index];
            
            StartRecoil(index);

            for (int i = 0; i < bulletsPerShot; i++)
            {
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

                Vector3 direction = firePoint.forward;
                
                if (spreadAngle > 0)
                {
                    float spreadX = Random.Range(-spreadAngle, spreadAngle);
                    float spreadY = Random.Range(-spreadAngle, spreadAngle);
                    direction = Quaternion.Euler(spreadX, spreadY, 0) * direction;
                }

                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * bulletSpeed;
                    rb.AddForce(direction * bulletForce, ForceMode.Impulse);
                }
                else
                {
                    Debug.LogWarning("У пули нет Rigidbody!");
                }

                Destroy(bullet, 5f);
            }
        }
    }

    private void StartRecoil(int barrelIndex)
    {
        if (barrels == null || barrelIndex >= barrels.Length) return;
        
        BarrelData barrel = barrels[barrelIndex];
        barrel.isRecoiling = true;
        barrel.recoilProgress = 0f;
    }

    private void PlayShootSound()
    {
        if (shootSound == null) return;
        
        // Проверяем, существует ли AudioManager
        if (AudioManager.Instance != null)
        {
            // Воспроизводим 3D звук в позиции башни с вариацией тона
            float pitch = AudioManager.GetRandomPitch(pitchVariation);
            AudioManager.PlaySound(shootSound, transform.position, volume, pitch);
        }
        else
        {
            // Fallback: используем локальный AudioSource если AudioManager не найден
            if (audioSource != null)
            {
                audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
                audioSource.PlayOneShot(shootSound, volume);
            }
            else
            {
                Debug.LogWarning("[TurretController] Нет AudioManager и нет AudioSource!");
            }
        }
    }

    // Публичный метод для переключения режима (можно вызвать из другого скрипта)
    public void ToggleAutoMode()
    {
        autoMode = !autoMode;
        if (!autoMode)
        {
            currentTarget = null;
        }
    }

    // Публичный метод для установки режима
    public void SetAutoMode(bool enabled)
    {
        autoMode = enabled;
        if (!autoMode)
        {
            currentTarget = null;
        }
    }

    /// <summary>
    /// Визуализация радиуса обнаружения в Scene View
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDetectionRange) return;

        // Устанавливаем цвет и прозрачность
        Gizmos.color = detectionRangeColor;
        
        // Рисуем полупрозрачную сферу
        Gizmos.DrawSphere(transform.position, detectionRange);
        
        // Рисуем контур сферы
        Gizmos.color = detectionRangeWireColor;
        DrawWireSphere(transform.position, detectionRange, rangeSegments);
        
        // Если включен авторежим и есть цель, рисуем линию к цели
        if (autoMode && currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            
            // Рисуем маленькую сферу вокруг цели
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }

    /// <summary>
    /// Рисует каркас сферы с заданным количеством сегментов
    /// </summary>
    private void DrawWireSphere(Vector3 center, float radius, int segments)
    {
        // Рисуем вертикальные круги
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * 360f * Mathf.Deg2Rad;
            float angle2 = ((i + 1) / (float)segments) * 360f * Mathf.Deg2Rad;
            
            // Круг на горизонтальной плоскости (XY)
            Vector3 p1 = new Vector3(
                center.x + radius * Mathf.Cos(angle1),
                center.y,
                center.z + radius * Mathf.Sin(angle1)
            );
            Vector3 p2 = new Vector3(
                center.x + radius * Mathf.Cos(angle2),
                center.y,
                center.z + radius * Mathf.Sin(angle2)
            );
            Gizmos.DrawLine(p1, p2);
            
            // Круг в вертикальной плоскости (XZ)
            Vector3 p3 = new Vector3(
                center.x + radius * Mathf.Cos(angle1),
                center.y + radius * Mathf.Sin(angle1),
                center.z
            );
            Vector3 p4 = new Vector3(
                center.x + radius * Mathf.Cos(angle2),
                center.y + radius * Mathf.Sin(angle2),
                center.z
            );
            Gizmos.DrawLine(p3, p4);
            
            // Круг в вертикальной плоскости (YZ)
            Vector3 p5 = new Vector3(
                center.x,
                center.y + radius * Mathf.Cos(angle1),
                center.z + radius * Mathf.Sin(angle1)
            );
            Vector3 p6 = new Vector3(
                center.x,
                center.y + radius * Mathf.Cos(angle2),
                center.z + radius * Mathf.Sin(angle2)
            );
            Gizmos.DrawLine(p5, p6);
        }
    }
}