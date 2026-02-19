using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TilemapTextureWithNoiseCreator))]
public class TilemapTextureWithNoiseCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TilemapTextureWithNoiseCreator creator = (TilemapTextureWithNoiseCreator)target;
        if (GUILayout.Button("Загрузить текстуру в шейдер"))
        {
            creator.LoadTextures();
            Debug.Log("Textures Loaded");
        }
    }
}
