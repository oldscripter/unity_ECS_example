// oldscripter@gmail.com

using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class PerformanceDisplay : MonoBehaviour
{
    [Header("Settings for FPS")]
    [SerializeField] private float updateInterval = 0.5f;
    
    [Header("Coloring for FPS")]
    [SerializeField] private Color optimalColor = Color.green;   // >= 45 FPS
    [SerializeField] private Color warningColor = Color.yellow;  // 25-44 FPS
    [SerializeField] private Color criticalColor = Color.red;    // < 25 FPS
    
    [Header("Show additional metrics")]
    [SerializeField] private bool showDrawCalls = true;
    [SerializeField] private bool showSetPassCalls = true;
    [SerializeField] private bool showVertices = true;
    
    private TextMeshProUGUI _displayText;
    private int _framesCount;
    private float _framesTime;
    
    private ProfilerRecorder _drawCallsRecorder;
    private ProfilerRecorder _setPassCallsRecorder;
    private ProfilerRecorder _verticesRecorder;
    
    private void OnEnable()
    {
        if (showDrawCalls)
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        
        if (showSetPassCalls)
            _setPassCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        
        if (showVertices)
            _verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
    }
    
    private void OnDisable()
    {
        if (showDrawCalls && _drawCallsRecorder.Valid)
            _drawCallsRecorder.Dispose();
        
        if (showSetPassCalls && _setPassCallsRecorder.Valid)
            _setPassCallsRecorder.Dispose();
        
        if (showVertices && _verticesRecorder.Valid)
            _verticesRecorder.Dispose();
    }
    
    private void Awake()
    {
        _displayText = GetComponent<TextMeshProUGUI>();
    }
    
    private void Update()
    {
        _framesCount++;
        _framesTime += Time.unscaledDeltaTime;
        
        if (_framesTime >= updateInterval)
        {
            float fps = _framesCount / _framesTime;
            float ms = (_framesTime / _framesCount) * 1000f;
            
            Color fpsColor;
            if (fps >= 45) fpsColor = optimalColor;
            else if (fps >= 25) fpsColor = warningColor;
            else fpsColor = criticalColor;
            
            var sb = new StringBuilder();
            
            // FPS & MS
            sb.AppendLine($"<color=#{ColorToHex(fpsColor)}>FPS: {fps:0.}</color>");
            sb.AppendLine($"MS: {ms:0.0}");
            
            // Metrics render
            if (showDrawCalls && _drawCallsRecorder.Valid)
                sb.AppendLine($"Draw Calls: {_drawCallsRecorder.LastValue}");
            
            if (showSetPassCalls && _setPassCallsRecorder.Valid)
                sb.AppendLine($"SetPass Calls: {_setPassCallsRecorder.LastValue}");
            
            if (showVertices && _verticesRecorder.Valid)
                sb.AppendLine($"Vertices: {_verticesRecorder.LastValue:N0}");
            
            _displayText.text = sb.ToString();
            
            // Timing reset
            _framesCount = 0;
            _framesTime = 0;
        }
    }
    
    private string ColorToHex(Color color)
    {
        return $"{Mathf.RoundToInt(color.r * 255):X2}" +
               $"{Mathf.RoundToInt(color.g * 255):X2}" +
               $"{Mathf.RoundToInt(color.b * 255):X2}";
    }
}