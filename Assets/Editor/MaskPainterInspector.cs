using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(Material))]
public class MaskPainterInspector : Editor
{
    private bool isPainting = false;
    private bool isErasing = false;
    private Texture2D maskTexture;
    private RenderTexture renderTexture;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        Material material = (Material)target;
        
        if (material.shader.name != "Unlit/TestShader")
        {
            return;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mask Painting Tools", EditorStyles.boldLabel);
        
        // Получаем текстуру маски
        Texture2D currentMask = (Texture2D)material.GetTexture("_BlendMask");
        
        if (currentMask != null)
        {
            if (maskTexture == null || maskTexture != currentMask)
            {
                InitializeMaskTexture(currentMask);
            }
        }
        
        // Инструменты
        EditorGUILayout.BeginVertical("box");
        {
            material.SetColor("_MaskColor", EditorGUILayout.ColorField("Paint Color", material.GetColor("_MaskColor")));
            material.SetFloat("_BrushSize", EditorGUILayout.Slider("Brush Size", material.GetFloat("_BrushSize"), 0.01f, 0.5f));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(isPainting ? "Stop Painting" : "Start Painting"))
                {
                    isPainting = !isPainting;
                    isErasing = false;
                }
                
                if (GUILayout.Button(isErasing ? "Stop Erasing" : "Start Erasing"))
                {
                    isErasing = !isErasing;
                    isPainting = false;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Clear Mask"))
            {
                ClearMask();
            }
            
            if (GUILayout.Button("Save Mask"))
            {
                SaveMaskTexture();
            }
        }
        EditorGUILayout.EndVertical();
        
        // Предпросмотр маски
        if (currentMask != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mask Preview:");
            Rect rect = GUILayoutUtility.GetAspectRect(1f);
            EditorGUI.DrawPreviewTexture(rect, currentMask);
        }
        
        // Обработка событий мыши для рисования
        HandlePaintEvents();
    }
    
    private void InitializeMaskTexture(Texture2D source)
    {
        maskTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        maskTexture.wrapMode = TextureWrapMode.Clamp;
        maskTexture.filterMode = FilterMode.Bilinear;
        
        // Копируем существующую текстуру
        Color[] pixels = source.GetPixels();
        maskTexture.SetPixels(pixels);
        maskTexture.Apply();
    }
    
    private void HandlePaintEvents()
    {
        if (!isPainting && !isErasing) return;
        
        Event currentEvent = Event.current;
        
        if (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag)
        {
            if (currentEvent.button == 0) // Левая кнопка мыши
            {
                Vector2 mousePos = currentEvent.mousePosition;
                EditorWindow window = EditorWindow.mouseOverWindow;
                
                if (window != null)
                {
                    // Получаем координаты UV из позиции мыши в инспекторе
                    // Это упрощенная реализация - в реальном проекте нужно более точное преобразование координат
                    PaintAtPosition(mousePos);
                    currentEvent.Use();
                }
            }
        }
    }
    
    private void PaintAtPosition(Vector2 position)
    {
        if (maskTexture == null) return;
        
        Material material = (Material)target;
        Color paintColor = isPainting ? material.GetColor("_MaskColor") : Color.black;
        float brushSize = material.GetFloat("_BrushSize") * 100f;
        
        // Преобразуем координаты мыши в UV координаты текстуры
        // Это упрощенная версия - в реальном проекте нужно точное преобразование
        int x = Mathf.Clamp((int)(position.x / Screen.width * maskTexture.width), 0, maskTexture.width - 1);
        int y = Mathf.Clamp((int)(position.y / Screen.height * maskTexture.height), 0, maskTexture.height - 1);
        
        // Рисуем кистью
        int brushRadius = Mathf.Max(1, (int)(brushSize * maskTexture.width / 100f));
        
        for (int i = -brushRadius; i <= brushRadius; i++)
        {
            for (int j = -brushRadius; j <= brushRadius; j++)
            {
                int px = x + i;
                int py = y + j;
                
                if (px >= 0 && px < maskTexture.width && py >= 0 && py < maskTexture.height)
                {
                    float distance = Mathf.Sqrt(i * i + j * j) / brushRadius;
                    if (distance <= 1f)
                    {
                        float alpha = 1f - distance;
                        Color currentColor = maskTexture.GetPixel(px, py);
                        Color newColor = Color.Lerp(currentColor, paintColor, alpha * 0.5f);
                        maskTexture.SetPixel(px, py, newColor);
                    }
                }
            }
        }
        
        maskTexture.Apply();
        material.SetTexture("_BlendMask", maskTexture);
        
        // Принудительно обновляем сцену
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }
    
    private void ClearMask()
    {
        if (maskTexture != null)
        {
            Color[] clearPixels = new Color[maskTexture.width * maskTexture.height];
            for (int i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = Color.black;
            }
            maskTexture.SetPixels(clearPixels);
            maskTexture.Apply();
            
            Material material = (Material)target;
            material.SetTexture("_BlendMask", maskTexture);
            EditorUtility.SetDirty(target);
        }
    }
    
    private void SaveMaskTexture()
    {
        if (maskTexture == null) return;
        
        string path = EditorUtility.SaveFilePanel("Save Mask Texture", "Assets", "BlendMask", "png");
        if (!string.IsNullOrEmpty(path))
        {
            byte[] pngData = maskTexture.EncodeToPNG();
            File.WriteAllBytes(path, pngData);
            
            // Обновляем ассеты
            AssetDatabase.Refresh();
            
            Debug.Log("Mask saved to: " + path);
        }
    }
    
    private void OnDisable()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            DestroyImmediate(renderTexture);
        }
    }
}