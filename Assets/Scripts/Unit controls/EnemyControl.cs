using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Настройки")]
    public float speed = 3f;
    public float stoppingDistance = 1.5f;
    public float health = 20f;
    public float deathDelay = 5f;
    public float physicsDelay = 0.1f;
    public float ragdollForceMultiplier = 1f;
    
    [Header("Компоненты")]
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    
    [Header("Эффекты")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource audioSource;
    
    // Состояние
    private bool isDead = false;
    private float deathTimer = 0f;
    private bool physicsEnabled = false;
    
    // Данные для физики
    private Vector3 pendingForce = Vector3.zero;
    private Vector3 pendingTorque = Vector3.zero;
    private bool hasPendingForce = false;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
        
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
            
        if (rb == null)
            rb = GetComponent<Rigidbody>();
            
        if (col == null)
            col = GetComponent<Collider>();
        
        // Настройка компонентов
        if (agent != null)
        {
            agent.speed = speed;
            agent.stoppingDistance = stoppingDistance;
        }
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (isDead)
        {
            deathTimer += Time.deltaTime;
            
            // Включаем физику после задержки
            if (!physicsEnabled && deathTimer >= physicsDelay)
            {
                EnablePhysics();
            }
            
            if (deathTimer >= deathDelay)
            {
                Destroy(gameObject);
            }
            return;
        }
        
        if (player == null) return;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }
    }

    // Упрощенный метод для нанесения урона
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        health -= damage;
        
        if (health <= 0)
        {
            Die(Vector3.up, 10f);
        }
    }

    // Метод с указанием направления и силы (для отбрасывания)
    public void TakeDamage(float damage, Vector3 hitDirection, float force)
    {
        if (isDead) return;
        
        health -= damage;
        
        if (health <= 0)
        {
            Die(hitDirection, force);
        }
        else
        {
            // Если не умер - просто отбрасываем (включаем физику временно)
            ApplyForceToRigidbody(hitDirection * force * 0.5f);
        }
    }

    // Метод для применения силы взрыва (улучшенный)
    public void ApplyExplosionForce(Vector3 force, Vector3 position, float radius)
    {
        if (isDead) return;
        
        // Применяем силу даже если враг жив
        if (rb != null)
        {
            // Если враг еще жив, но мы хотим его отбросить
            // Временно включаем физику
            if (rb.isKinematic)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                
                // Отключаем NavMeshAgent на время
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.enabled = false;
                }
            }
            
            // Применяем силу
            rb.AddForce(force, ForceMode.Impulse);
            
            // Добавляем вращение
            Vector3 torque = new Vector3(
                Random.Range(-50f, 50f),
                Random.Range(-50f, 50f),
                Random.Range(-50f, 50f)
            );
            rb.AddTorque(torque, ForceMode.Impulse);
            
            // Если враг умер, то он останется с физикой
            // Если жив, через некоторое время вернемся к кинематике
            if (health > 0)
            {
                StartCoroutine(ReturnToKinematic());
            }
        }
    }

    private System.Collections.IEnumerator ReturnToKinematic()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (!isDead && rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            if (agent != null)
            {
                agent.enabled = true;
            }
        }
    }

    private void ApplyForceToRigidbody(Vector3 force)
    {
        if (rb == null) return;
        
        // Если Rigidbody кинематический, делаем его динамическим
        if (rb.isKinematic)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            if (agent != null)
            {
                agent.enabled = false;
            }
        }
        
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 20f, ForceMode.Impulse);
    }

    public void Die(Vector3 direction, float force)
    {
        if (isDead) return;
        
        isDead = true;
        deathTimer = 0f;
        physicsEnabled = false;
        
        // Сохраняем силу для применения после включения физики
        pendingForce = direction * force * ragdollForceMultiplier;
        pendingTorque = Random.insideUnitSphere * 50f;
        hasPendingForce = true;
        
        // Отключаем NavMeshAgent сразу
        if (agent != null)
        {
            agent.enabled = false;
        }
        
        // Делаем коллайдер триггером, чтобы пули проходили насквозь
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        // Эффект смерти
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // Звук смерти
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        Debug.Log($"💀 Враг уничтожен! Сила: {pendingForce.magnitude}, Физика через {physicsDelay}с");
    }
    
    private void EnablePhysics()
    {
        physicsEnabled = true;
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Применяем сохраненную силу
            if (hasPendingForce)
            {
                rb.AddForce(pendingForce, ForceMode.Impulse);
                rb.AddTorque(pendingTorque, ForceMode.Impulse);
                
                Debug.Log($"💥 Применена сила к врагу: {pendingForce.magnitude}");
                hasPendingForce = false;
            }
        }
        
        // Возвращаем коллайдер в обычный режим
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetHealth()
    {
        return health;
    }

    public void ResetHealth(float newHealth)
    {
        health = newHealth;
        isDead = false;
        deathTimer = 0f;
        physicsEnabled = false;
        hasPendingForce = false;
        
        if (agent != null)
        {
            agent.enabled = true;
        }
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }

    // Метод для принудительного отбрасывания (вызывается извне)
    public void ApplyForceDirectly(Vector3 force, Vector3 torque)
    {
        if (rb == null) return;
        
        // Принудительно включаем физику
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Отключаем NavMeshAgent
        if (agent != null && agent.isOnNavMesh)
        {
            agent.enabled = false;
        }
        
        // Применяем силу
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);
        
        // Если враг еще жив, через некоторое время вернем кинематику
        if (!isDead)
        {
            StartCoroutine(ReturnToKinematicAfterDelay(0.5f));
        }
    }

    private System.Collections.IEnumerator ReturnToKinematicAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (!isDead && rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            if (agent != null)
            {
                agent.enabled = true;
            }
        }
    }
}