using UnityEngine;

public class RadialMatrixGenerator : MonoBehaviour
{
    [Header("Matrix Settings")]
    public int width = 10;
    public int height = 10;
    public float centerValue = 1f;
    
    [Header("Visualization")]
    public bool printMatrix = false;
    public float cellSize = 1f;
    public GameObject cubePrefab; // Опционально: для визуализации
    
    private float[,] matrix;
    
    void Start()
    {
        GenerateRadialMatrix();
        
        if (printMatrix)
            PrintMatrix();
            
        if (cubePrefab != null)
            VisualizeMatrix();
    }
    
    float[,] GenerateRadialMatrix()
    {
        matrix = new float[width, height];
        
        // Находим центр матрицы
        Vector2 center = new Vector2((width - 1) / 2f, (height - 1) / 2f);
        
        // Находим максимальное расстояние от центра до угла
        float maxDistance = Mathf.Sqrt(center.x * center.x + center.y * center.y);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Вычисляем расстояние от текущей точки до центра
                float distance = Vector2.Distance(new Vector2(x, y), center);

                // Нормализуем расстояние (0 в центре, 1 на самом дальнем углу)
                float normalizedDistance = distance / maxDistance;

                // Вычисляем значение: от centerValue в центре до 0 на краях
                matrix[x, y] = Mathf.Lerp(centerValue, 0f, normalizedDistance);

                // Обеспечиваем, чтобы значения не были отрицательными
                matrix[x, y] = Mathf.Max(0f, matrix[x, y]);
            }
        }
        return matrix;
    }
    
    void PrintMatrix()
    {
        string matrixString = "Radial Matrix:\n";
        
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                matrixString += matrix[x, y].ToString("F2") + "\t";
            }
            matrixString += "\n";
        }
        
        Debug.Log(matrixString);
    }
    
    void VisualizeMatrix()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x * cellSize, 0, y * cellSize);
                GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity);
                
                // Масштабируем куб в зависимости от значения в матрице
                float scale = matrix[x, y];
                cube.transform.localScale = new Vector3(scale, scale, scale);
                
                // Изменяем цвет в зависимости от значения
                Renderer renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = Color.Lerp(Color.black, Color.white, matrix[x, y]);
                    renderer.material.color = color;
                }
                
                cube.transform.SetParent(this.transform);
            }
        }
    }
    
    // Метод для получения сгенерированной матрицы
    public float[,] GetMatrix()
    {
        return matrix;
    }
    
    // Метод для перегенерации матрицы с новыми параметрами
    public void RegenerateMatrix(int newWidth, int newHeight, float newCenterValue)
    {
        width = newWidth;
        height = newHeight;
        centerValue = newCenterValue;
        
        GenerateRadialMatrix();
    }
}