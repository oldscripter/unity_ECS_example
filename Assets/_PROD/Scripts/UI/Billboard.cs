using UnityEngine;

public class Billboard : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool lockXAxis = false;
    [SerializeField] private bool lockYAxis = false;
    [SerializeField] private bool lockZAxis = false;
    
    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }
    
    private void LateUpdate()
    {
        if (targetCamera == null) return;
        
        // Поворачиваем объект к камере
        Vector3 direction = transform.position - targetCamera.transform.position;
        
        // Если нужно зафиксировать оси
        if (lockXAxis) direction.x = 0;
        if (lockYAxis) direction.y = 0;
        if (lockZAxis) direction.z = 0;
        
        transform.rotation = Quaternion.LookRotation(direction);
    }
}