using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Префабы")]
    [SerializeField] private GameObject enemyPrefab; // Префаб врага
    
    [Header("Настройки спауна")]
    [SerializeField] private int maxEnemies = 10; // Максимум врагов на сцене
    [SerializeField] private float spawnInterval = 3f; // Интервал между спаунами
    [SerializeField] private float spawnRadius = 20f; // Радиус спауна вокруг точки
    [SerializeField] private bool spawnAtStart = true; // Спаунить сразу при старте
    
    [Header("Сложность")]
    [SerializeField] private bool increaseDifficulty = false; // Увеличивать сложность
    [SerializeField] private float difficultyIncreaseInterval = 30f; // Интервал увеличения сложности
    [SerializeField] private int maxEnemiesIncrease = 2; // На сколько увеличивать максимум врагов
    [SerializeField] private float spawnIntervalDecrease = 0.2f; // На сколько уменьшать интервал спауна
    [SerializeField] private float minSpawnInterval = 0.5f; // Минимальный интервал спауна
    
    [Header("Зоны спауна (опционально)")]
    [SerializeField] private Transform[] spawnPoints; // Конкретные точки спауна (если пусто - спаунит вокруг)
    [SerializeField] private bool useRandomPoints = true; // Использовать случайные точки из массива
    
    [Header("Визуализация")]
    [SerializeField] private bool showGizmos = true; // Показывать радиус в редакторе
    [SerializeField] private Color gizmoColor = Color.green; // Цвет радиуса
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private float nextSpawnTime = 0f;
    private float nextDifficultyIncrease = 0f;
    private int currentMaxEnemies;
    private float currentSpawnInterval;
    private Transform player;
    
    private void Start()
    {
        // Находим игрока
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        
        // Инициализация параметров
        currentMaxEnemies = maxEnemies;
        currentSpawnInterval = spawnInterval;
        nextSpawnTime = Time.time;
        nextDifficultyIncrease = Time.time + difficultyIncreaseInterval;
        
        // Спауним начальных врагов
        if (spawnAtStart)
        {
            SpawnInitialEnemies();
        }
    }
    
    private void Update()
    {
        // Очищаем список от уничтоженных врагов
        activeEnemies.RemoveAll(enemy => enemy == null);
        
        // Проверяем, нужно ли спаунить нового врага
        if (activeEnemies.Count < currentMaxEnemies && Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + currentSpawnInterval;
        }
        
        // Увеличение сложности
        if (increaseDifficulty && Time.time >= nextDifficultyIncrease)
        {
            IncreaseDifficulty();
            nextDifficultyIncrease = Time.time + difficultyIncreaseInterval;
        }
    }
    
    /// <summary>
    /// Создает начальных врагов
    /// </summary>
    private void SpawnInitialEnemies()
    {
        int initialCount = Mathf.Min(currentMaxEnemies, 3); // Спауним 3 врагов или меньше
        for (int i = 0; i < initialCount; i++)
        {
            SpawnEnemy();
        }
        nextSpawnTime = Time.time + currentSpawnInterval;
    }
    
    /// <summary>
    /// Создает одного врага
    /// </summary>
    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemyPrefab не назначен!");
            return;
        }
        
        // Определяем позицию спауна
        Vector3 spawnPosition = GetSpawnPosition();
        
        // Создаем врага
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemies.Add(newEnemy);
        
        // Если есть игрок - враг автоматически найдет его через скрипт
    }
    
    /// <summary>
    /// Определяет позицию для спауна
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        Vector3 spawnPosition = Vector3.zero;
        
        // Если есть точки спауна и используем их
        if (spawnPoints != null && spawnPoints.Length > 0 && useRandomPoints)
        {
            // Выбираем случайную точку
            int randomIndex = Random.Range(0, spawnPoints.Length);
            spawnPosition = spawnPoints[randomIndex].position;
        }
        else
        {
            // Спауним вокруг спаунера
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            
            // Проверяем, что позиция не слишком близко к игроку
            if (player != null)
            {
                float minDistanceFromPlayer = 5f;
                int attempts = 0;
                while (Vector3.Distance(spawnPosition, player.position) < minDistanceFromPlayer && attempts < 10)
                {
                    randomCircle = Random.insideUnitCircle * spawnRadius;
                    spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
                    attempts++;
                }
            }
        }
        
        // Попытка найти поверхность (если есть террейн)
        if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
        {
            spawnPosition.y = hit.point.y;
        }
        
        return spawnPosition;
    }
    
    /// <summary>
    /// Увеличивает сложность
    /// </summary>
    private void IncreaseDifficulty()
    {
        // Увеличиваем максимум врагов
        currentMaxEnemies = Mathf.Min(currentMaxEnemies + maxEnemiesIncrease, 50);
        
        // Уменьшаем интервал спауна
        currentSpawnInterval = Mathf.Max(currentSpawnInterval - spawnIntervalDecrease, minSpawnInterval);
        
        Debug.Log($"Сложность увеличена! Максимум врагов: {currentMaxEnemies}, Интервал спауна: {currentSpawnInterval:F1}с");
    }
    
    /// <summary>
    /// Получить количество активных врагов
    /// </summary>
    public int GetActiveEnemyCount()
    {
        activeEnemies.RemoveAll(enemy => enemy == null);
        return activeEnemies.Count;
    }
    
    /// <summary>
    /// Уничтожить всех врагов
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
    }
    
    /// <summary>
    /// Включить/выключить спаун
    /// </summary>
    public void SetSpawningEnabled(bool enabled)
    {
        enabled = enabled;
    }
    
    /// <summary>
    /// Визуализация в редакторе
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Рисуем радиус спауна
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        // Рисуем точки спауна
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Gizmos.color = Color.blue;
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.5f);
                    Gizmos.DrawLine(transform.position, point.position);
                }
            }
        }
        
        // Рисуем текущих врагов (всегда зеленые)
        Gizmos.color = Color.green;
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireSphere(enemy.transform.position, 0.5f);
            }
        }
    }
}