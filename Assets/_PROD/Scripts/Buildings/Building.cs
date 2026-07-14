using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Building : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private string buildingName;
    [Header("Rendering")]
    [SerializeField] GameObject meshRootObject;
    [SerializeField] Color selectionColor;
    [Header("UI")]
    [SerializeField] GameObject uiRootPanel;


    // private:
    private bool isSelected = false;
    private List<Material> originalMaterials = new List<Material>(); //used for selection effect
    private List<Renderer> renderers = new List<Renderer>(); // used for selection effect
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("[BUILDING] [" + name + "] is created");
        if (uiRootPanel)
            uiRootPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FindAllRenderers(GameObject obj)
    {
        renderers.Clear();
        originalMaterials.Clear();
        
        Renderer[] allRenderers = obj.GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer rend in allRenderers)
        {
            if (!rend.enabled) continue;
            
            renderers.Add(rend);
            
            foreach (Material mat in rend.materials)
            {
                originalMaterials.Add(mat);
            }
        }
        
        Debug.Log($"Found {renderers.Count} renderers with {originalMaterials.Count} materials");
    }

    private void ApplyHighlight()
    {
        if (renderers.Count == 0)
        {
            Debug.LogWarning("No renderers found for highlighting!");
            return;
        }
        
        foreach (Renderer rend in renderers)
        {
            // Создаем копии материалов для выделения
            Material[] newMaterials = new Material[rend.materials.Length];
            
            for (int i = 0; i < rend.materials.Length; i++)
            {
                // Создаем новый материал на основе оригинального
                Material highlightMat = new Material(rend.materials[i]);
                
                // Применяем эффект подсветки
                if (highlightMat.HasProperty("_Color"))
                {
                    Color baseColor = highlightMat.color;
                    // Смешиваем с цветом выделения
                    highlightMat.color = Color.Lerp(baseColor, selectionColor, 0.5f);
                }
                               
                newMaterials[i] = highlightMat;
            }
            
            rend.materials = newMaterials;
        }
    }

    private IEnumerator AnimateUIScale(Transform target, Vector3 from, Vector3 to, float duration, System.Action onComplete = null)
    {
        if (target == null) yield break;
        
        float elapsed = 0f;
        target.localScale = from;
        
        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Используем плавную кривую
            float smoothT = Mathf.SmoothStep(0, 1, t);
            target.localScale = Vector3.Lerp(from, to, smoothT);
            yield return null;
        }
        
        if (target != null)
            target.localScale = to;
            
        onComplete?.Invoke();
    }

     private void RestoreOriginalMaterials()
    {
        if (renderers.Count == 0) return;
        
        int materialIndex = 0;
        
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            
            Material[] originalMats = new Material[rend.materials.Length];
            
            for (int i = 0; i < rend.materials.Length; i++)
            {
                if (materialIndex < originalMaterials.Count)
                {
                    originalMats[i] = originalMaterials[materialIndex];
                    materialIndex++;
                }
            }
            
            rend.materials = originalMats;
        }
    }

    public void Select()
    {
        isSelected = true;
        // Находим все Renderer в здании и его дочерних объектах
        FindAllRenderers(meshRootObject);
        
        // // Сохраняем оригинальные материалы и применяем подсветку
        ApplyHighlight();
        
        // // Показываем UI
        if (uiRootPanel)
        {
            uiRootPanel.SetActive(true);
            uiRootPanel.transform.localScale = Vector3.zero;
            StartCoroutine(AnimateUIScale(uiRootPanel.transform, Vector3.zero, Vector3.one, 0.3f));
        }
        
        Debug.Log("[BUILDING] [" + name + "] is selected");
    }
    
    public void Deselect()
    {
        isSelected = false;
        
        // Восстанавливаем оригинальные материалы
        RestoreOriginalMaterials();
               
        // Скрываем UI
        if (uiRootPanel)
        {
            StartCoroutine(AnimateUIScale(uiRootPanel.transform, Vector3.one, Vector3.zero, 0.2f, () => {
                if (uiRootPanel != null)
                    uiRootPanel.SetActive(false);
            }));
        }

        renderers.Clear();
        originalMaterials.Clear();
        Debug.Log("[BUILDING] [" + name + "] is deselected");
    }

    private void OnDestroy()
    {
        Debug.Log("[BUILDING] [" + name + "] is destroyed");
    }
}
