using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    [Header("Настройки снаряда")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float upwardModifier = 1f;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float destroyDelay = 5f;
    [SerializeField] private LayerMask enemyLayerMask;
    
    [Header("Настройки воздействия на трупы")]
    [SerializeField] private bool affectCorpses = true;
    [SerializeField] private float corpseForceMultiplier = 0.8f;
    [SerializeField] private float corpseUpwardModifier = 1.5f;
    
    [Header("Настройки воронки")]
    [SerializeField] private Sprite craterSprite;           // Спрайт воронки
    [SerializeField] private float craterDuration = 5f;     // Время отображения воронки
    [SerializeField] private float craterScale = 1.5f;      // Размер воронки
    [SerializeField] private float craterFadeTime = 1f;     // Время исчезновения
    [SerializeField] private bool randomRotation = true;    // Случайный поворот
    [SerializeField] private bool randomScale = true;       // Случайный размер
    [SerializeField] private Color craterColor = new Color(0.2f, 0.15f, 0.1f, 0.9f); // Цвет воронки
    
    [Header("Настройки траектории")]
    [SerializeField] private bool useGravity = true;
    [SerializeField] private float gravityScale = 1f;
    
    private Rigidbody rb;
    private Vector3 launchDirection;
    private bool hasExploded = false;
    private float lifeTimer = 0f;
    private bool isInitialized = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            rb.useGravity = useGravity;
        }
        
        if (enemyLayerMask == 0)
        {
            enemyLayerMask = LayerMask.GetMask("Default");
        }
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        
        if (lifeTimer >= destroyDelay && !hasExploded)
        {
            Explode();
        }
        
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }
    }

    private void FixedUpdate()
    {
        if (useGravity && rb != null && isInitialized)
        {
            rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        }
    }

    public void Initialize(float launchSpeed, Vector3 direction)
    {
        speed = launchSpeed;
        launchDirection = direction.normalized;
        isInitialized = true;
        
        if (rb != null)
        {
            rb.linearVelocity = launchDirection * speed;
            Debug.Log($"🚀 Снаряд инициализирован: скорость {speed}, направление {launchDirection}");
        }
        else
        {
            Debug.LogError("❌ У снаряда нет Rigidbody!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        
        Debug.Log($"💫 Снаряд столкнулся с: {collision.gameObject.name}");
        
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null && !enemy.IsDead())
        {
            enemy.TakeDamage(damage * 2f);
            Debug.Log($"🎯 Прямое попадание во врага {enemy.name}!");
        }
        
        Explode();
    }

    private void Explode()
    {
        if (hasExploded) return;
        
        hasExploded = true;
        Debug.Log($"💥 ВЗРЫВ! Сила: {explosionForce}, Радиус: {explosionRadius}");
        
        // Эффект взрыва
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // █████████████████████████████████████████████████████████████████████████
        // ███  СОЗДАЕМ ВОРОНКУ  █████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████
        if (craterSprite != null)
        {
            CreateCrater(transform.position);
        }
        // █████████████████████████████████████████████████████████████████████████
        
        // Находим всех врагов в радиусе
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayerMask);
        Debug.Log($"👾 Найдено объектов в радиусе: {hitColliders.Length}");
        
        int livingHit = 0;
        int corpseHit = 0;
        
        foreach (Collider collider in hitColliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                bool isDead = enemy.IsDead();
                
                if (!isDead)
                {
                    enemy.TakeDamage(damage);
                    livingHit++;
                }
                else if (affectCorpses)
                {
                    corpseHit++;
                }
                
                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 direction = (enemy.transform.position - transform.position).normalized;
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    
                    float forceMultiplier = 1f - (distance / explosionRadius);
                    forceMultiplier = Mathf.Clamp01(forceMultiplier);
                    
                    float baseForce = explosionForce * 2f;
                    float corpseMultiplier = isDead ? corpseForceMultiplier : 1f;
                    
                    Vector3 forceDirection = direction;
                    float upward = isDead ? corpseUpwardModifier : upwardModifier;
                    forceDirection.y += upward * 0.5f;
                    forceDirection.Normalize();
                    
                    float finalForce = baseForce * forceMultiplier * corpseMultiplier;
                    finalForce *= Random.Range(0.9f, 1.1f);
                    
                    Vector3 explosionForceVector = forceDirection * finalForce;
                    
                    if (enemyRb.isKinematic)
                    {
                        enemyRb.isKinematic = false;
                        enemyRb.useGravity = true;
                        enemyRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        
                        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        if (agent != null && agent.isOnNavMesh)
                        {
                            agent.enabled = false;
                        }
                    }
                    
                    enemyRb.AddForce(explosionForceVector, ForceMode.Impulse);
                    
                    float torqueMultiplier = isDead ? 1.5f : 1f;
                    Vector3 torque = new Vector3(
                        Random.Range(-50f, 50f),
                        Random.Range(-50f, 50f),
                        Random.Range(-50f, 50f)
                    ) * torqueMultiplier * (1f + forceMultiplier);
                    enemyRb.AddTorque(torque, ForceMode.Impulse);
                    
                    if (isDead)
                    {
                        Debug.Log($"💀 Труп {enemy.name} отброшен с силой {explosionForceVector.magnitude:F1}");
                    }
                    else
                    {
                        Debug.Log($"💨 Враг {enemy.name} отброшен с силой {explosionForceVector.magnitude:F1}");
                    }
                }
            }
        }
        
        Debug.Log($"📊 Итог: {livingHit} живых врагов, {corpseHit} трупов отброшено");
        
        Destroy(gameObject, 0.1f);
    }

    // █████████████████████████████████████████████████████████████████████████
    // ███  МЕТОДЫ ДЛЯ ВОРОНКИ  █████████████████████████████████████████████████
    // █████████████████████████████████████████████████████████████████████████

    private void CreateCrater(Vector3 position)
    {
        if (craterSprite == null) return;
        
        // Проверяем, есть ли поверхность под воронкой
        RaycastHit hit;
        Vector3 craterPosition = position;
        Quaternion craterRotation = Quaternion.identity;
        
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out hit, 5f))
        {
            craterPosition = hit.point + hit.normal * 0.05f;
            craterRotation = Quaternion.LookRotation(-hit.normal);
        }
        
        // Создаем объект воронки
        GameObject craterObj = new GameObject("Crater");
        craterObj.transform.position = craterPosition;
        craterObj.transform.rotation = craterRotation;
        
        // Добавляем SpriteRenderer
        SpriteRenderer renderer = craterObj.AddComponent<SpriteRenderer>();
        renderer.sprite = craterSprite;
        renderer.color = new Color(craterColor.r, craterColor.g, craterColor.b, 0f);
        renderer.sortingOrder = -1;
        
        // Масштабируем
        float scale = craterScale;
        if (randomScale)
        {
            scale *= Random.Range(0.8f, 1.2f);
        }
        
        if (randomRotation)
        {
            craterObj.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
        }
        
        // Анимация появления
        StartCoroutine(AnimateCrater(craterObj, renderer, scale));
        
        // Уничтожаем через время
        Destroy(craterObj, craterDuration + craterFadeTime);
    }

    private IEnumerator AnimateCrater(GameObject crater, SpriteRenderer renderer, float targetScale)
    {
        // Появление
        float appearDuration = 0.3f;
        float timer = 0f;
        
        while (timer < appearDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / appearDuration;
            float scale = Mathf.Lerp(0f, targetScale, progress);
            crater.transform.localScale = Vector3.one * scale;
            
            Color color = renderer.color;
            color.a = Mathf.Lerp(0f, craterColor.a, progress);
            renderer.color = color;
            
            yield return null;
        }
        
        crater.transform.localScale = Vector3.one * targetScale;
        
        // Ждем
        yield return new WaitForSeconds(craterDuration - appearDuration - craterFadeTime);
        
        // Исчезновение
        timer = 0f;
        while (timer < craterFadeTime)
        {
            timer += Time.deltaTime;
            float progress = timer / craterFadeTime;
            
            Color color = renderer.color;
            color.a = Mathf.Lerp(craterColor.a, 0f, progress);
            renderer.color = color;
            
            // Слегка уменьшаемся при исчезновении
            float scale = Mathf.Lerp(targetScale, targetScale * 0.8f, progress);
            crater.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        Destroy(crater);
    }

    // █████████████████████████████████████████████████████████████████████████

    // Визуализация радиуса взрыва
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (rb != null && isInitialized)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
    }

    public void Detonate()
    {
        Explode();
    }

    public bool HasExploded()
    {
        return hasExploded;
    }
}