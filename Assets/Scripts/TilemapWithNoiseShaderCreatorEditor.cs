using UnityEditor;
using UnityEngine;

//[CustomEditor(typeof(TilemapWithNoiseShaderCreator))]
public class TilemapWithNoiseShaderCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        //TilemapWithNoiseShaderCreator creator = (TilemapWithNoiseShaderCreator)target;
        if (GUILayout.Button("Загрузить текстуры в шейдер"))
        {
            //creator.LoadTextures();
            Debug.Log("Textures Loaded");
        }
    }
}
