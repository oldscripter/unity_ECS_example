using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Настройки пули")]
    public float speed = 50f;
    public float damage = 100f;
    public float hitForce = 500f;
    public float lifeTime = 5f;
    
    [Header("Режим прошивания")]
    public bool penetrateEnemies = true; // true - прошивает, false - отскакивает
    public int maxPenetrations = 3; // Максимум врагов, которых может прошить
    public float bounceForce = 200f; // Сила отскока (если penetrate = false)

    [Header("Эффекты")]
    public GameObject bloodEffect; // Префаб эффекта крови
    public GameObject hitEffect; // Префаб эффекта попадания (для стен)
    public float effectLifetime = 2f; // Время жизни эффекта
    
    private Rigidbody rb;
    private int penetrationCount = 0;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
        
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        
        // Проверяем, является ли объект врагом
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            // Получаем точку попадания и нормаль
            Vector3 hitPoint = collision.contacts[0].point;
            Vector3 hitNormal = -collision.contacts[0].normal;
            
            // Создаем эффект крови
            SpawnEffect(bloodEffect, hitPoint, hitNormal);
            
            // Наносим урон врагу
            enemy.TakeDamage(damage, hitNormal, hitForce);
            
            if (penetrateEnemies)
            {
                // Режим ПРОШИВАНИЯ
                penetrationCount++;
                
                // Если достигли лимита прошиваний - уничтожаем пулю
                if (penetrationCount >= maxPenetrations)
                {
                    DestroyBullet();
                }
                // Иначе пуля продолжает лететь
            }
            else
            {
                // Режим ОТСКОКА
                if (rb != null)
                {
                    Vector3 bounceDirection = Vector3.Reflect(rb.linearVelocity.normalized, hitNormal);
                    rb.linearVelocity = bounceDirection * speed * 0.7f;
                    rb.linearVelocity += Random.insideUnitSphere * 2f;
                }
                
                damage *= 0.5f;
            }
        }
        else
        {
            // Столкновение со стеной или другим объектом
            Vector3 hitPoint = collision.contacts[0].point;
            Vector3 hitNormal = collision.contacts[0].normal;
            
            // Создаем эффект попадания (для стен)
            SpawnEffect(hitEffect, hitPoint, hitNormal);
            
            if (!penetrateEnemies)
            {
                // Если режим отскока - отскакиваем от стен
                if (rb != null)
                {
                    Vector3 bounceDirection = Vector3.Reflect(rb.linearVelocity.normalized, hitNormal);
                    rb.linearVelocity = bounceDirection * speed * 0.5f;
                }
            }
            else
            {
                // Если режим прошивания - уничтожаем пулю при столкновении со стеной
                DestroyBullet();
            }
        }
    }

    /// <summary>
    /// Создать эффект в точке попадания
    /// </summary>
    private void SpawnEffect(GameObject effectPrefab, Vector3 position, Vector3 normal)
    {
        if (effectPrefab == null) return;
        
        // Создаем эффект
        GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
        
        // Поворачиваем эффект в сторону нормали (для спрайтов или decal)
        effect.transform.LookAt(position + normal);
        
        // Если есть ParticleSystem - автоматически уничтожится после завершения
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            // Иначе уничтожаем через заданное время
            Destroy(effect, effectLifetime);
        }
    }

    void DestroyBullet()
    {
        if (isDead) return;
        isDead = true;
        
        // Можно добавить эффект исчезновения
        // SpawnEffect(disappearEffect, transform.position, Vector3.up);
        
        Destroy(gameObject);
    }

    public void ForceDestroy()
    {
        DestroyBullet();
    }
}