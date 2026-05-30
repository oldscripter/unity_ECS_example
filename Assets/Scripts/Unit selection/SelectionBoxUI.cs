using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SelectionBoxUI : MonoBehaviour
{
    public RectTransform selectionBox;
    
    private Vector2 startPosition;
    private bool isDragging;
    private Mouse mouse;
    
    void Start()
    {
        Debug.Log("Start");
        if (selectionBox != null)
        {
            selectionBox.gameObject.SetActive(false);
        }
        
        // Получаем устройство мыши
        mouse = Mouse.current;
    }
    
    void Update()
    {
        // Если мышь не найдена, выходим
        if (mouse == null) 
        {
            Debug.Log("Mouse not found");
            return;
        }
        
        // Начало выделения (левая кнопка мыши)
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Debug.Log("CLICK");
            startPosition = mouse.position.ReadValue();
            isDragging = true;
            
            if (selectionBox != null)
            {
                selectionBox.gameObject.SetActive(true);
                selectionBox.position = startPosition;
                selectionBox.sizeDelta = Vector2.zero;
            }
            
            Debug.Log("Selection started at: " + startPosition);
        }
        
        // Рисуем рамку
        if (isDragging && mouse.leftButton.isPressed)
        {
            Debug.Log("DRAW");
            if (selectionBox != null)
            {
                Vector2 currentPosition = mouse.position.ReadValue();
                Vector2 boxPosition = (startPosition + currentPosition) / 2;
                Vector2 boxSize = new Vector2(
                    Mathf.Abs(startPosition.x - currentPosition.x),
                    Mathf.Abs(startPosition.y - currentPosition.y)
                );
                
                selectionBox.position = boxPosition;
                selectionBox.sizeDelta = boxSize;
            }
        }
        
        // Конец выделения
        if (isDragging && mouse.leftButton.wasReleasedThisFrame)
        {
            Debug.Log("FINISH");
            isDragging = false;
            
            if (selectionBox != null)
            {
                selectionBox.gameObject.SetActive(false);
            }
            
            Debug.Log("Selection ended");
        }
    }
}