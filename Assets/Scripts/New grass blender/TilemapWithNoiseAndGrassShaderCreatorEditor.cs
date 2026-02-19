using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TilemapWithNoiseAndGrassShaderCreator))]
public class TilemapWithNoiseAndGrassShaderCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TilemapWithNoiseAndGrassShaderCreator creator = (TilemapWithNoiseAndGrassShaderCreator)target;
        if (GUILayout.Button("Загрузить текстуры в шейдер"))
        {
            creator.LoadTextures();
            Debug.Log("Textures Loaded");
        }
    }
}
