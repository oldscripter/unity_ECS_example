using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UnitCounterUI : MonoBehaviour
{
    [SerializeField] private SpawnerAuthoring spawner; // Перетащите спавнер в Inspector
    [SerializeField] private float updateInterval = 0.5f;
    
    private TextMeshProUGUI _text;
    private float _lastUpdate;
    
    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        
        if (spawner == null)
            spawner = FindFirstObjectByType<SpawnerAuthoring>();
    }
    
    private void Update()
    {
        if (Time.unscaledTime >= _lastUpdate + updateInterval)
        {
            if (spawner != null)
                _text.text = $"Units: {spawner.SpawnedCount:N0}";
            _lastUpdate = Time.unscaledTime;
        }
    }
}