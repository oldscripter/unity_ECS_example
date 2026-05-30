// oldscripter@gmail.com

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SelectionBoxUI : MonoBehaviour
{
    public GameObject selectionBoxObject;
    public RectTransform selectionBox;
    
    private Vector2 startPosition;
    private Vector2 endPosition;
    private bool isDragging;
    
    private static Vector2 _startPos;
    private static Vector2 _endPos;
    
    public static Vector2 GetStartPosition() { return _startPos; }
    public static Vector2 GetEndPosition() { return _endPos; }
    
    void Start()
    {
        selectionBox = selectionBoxObject.GetComponent<RectTransform>();
        if (selectionBox != null)
        {
            selectionBox.gameObject.SetActive(false);
        }
        _startPos = Vector2.zero;
        _endPos = Vector2.zero;
    }
    
    void Update()
    {
        // Selection start
        if (Input.GetMouseButtonDown(0))
        {
            startPosition = Input.mousePosition;
            endPosition = startPosition;
            _startPos = startPosition;
            _endPos = endPosition;
            isDragging = true;
            
            if (selectionBox != null)
            {
                selectionBox.gameObject.SetActive(true);
                selectionBox.position = startPosition;
                selectionBox.sizeDelta = Vector2.zero;
            }
        }
        
        // Draw the rect
        if (isDragging && Input.GetMouseButton(0))
        {
            endPosition = Input.mousePosition;
            _endPos = endPosition;
            
            if (selectionBox != null)
            {
                Vector2 boxPosition = (startPosition + endPosition) / 2;
                Vector2 boxSize = new Vector2(
                    Mathf.Abs(startPosition.x - endPosition.x),
                    Mathf.Abs(startPosition.y - endPosition.y)
                );
                
                selectionBox.position = boxPosition;
                selectionBox.sizeDelta = boxSize;
            }
        }
        
        // Selection end
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            endPosition = Input.mousePosition;
            _endPos = endPosition;
            isDragging = false;
            
            if (selectionBox != null)
            {
                selectionBox.gameObject.SetActive(false);
            }
        }
    }
}