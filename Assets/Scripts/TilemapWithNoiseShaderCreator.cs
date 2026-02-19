// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Tilemaps;

// public class TilemapWithNoiseShaderCreator : MonoBehaviour
// {
//     [SerializeField] private Tilemap _tilemap;
//     [SerializeField] private List<Texture2D> _uniqueTextures;
//     [SerializeField] private Texture2D[,] _tilemapInTextures;
//     [SerializeField] private Texture2D _tilemapTexturesIdInTexture;
//     [SerializeField] private SpriteRenderer _spriteWithShader;
//     [Range(0.01f, 3)]
//     [SerializeField] private float _falloffPower = 0.01f;
//     [Range(1, 2)]
//     [SerializeField] private float _fadeDistance = 1.5f;
//     [SerializeField] private TextureNoiseRule[] _noiseRules;
//     [SerializeField] private int _tileResolution = 1024;

//     [Range(0.1f, 10)]
//     [SerializeField] private float _grassNoiseSize = 1;
//     [Range(0, 1)]
//     [SerializeField] private float _grassNoiseIntencity = 0.3f;
//     [SerializeField] private float _grassNoiseXOffset = 0;
//     [SerializeField] private float _grassNoiseYOffset = 1;

//     private int _widthInTiles;
//     private int _heightInTiles;
//     private int _widthInPixels;
//     private int _heightInPixels;
//     private int _xMin;
//     private int _xMax;
//     private int _yMin;
//     private int _yMax;
//     private Material _material;

//     public void LoadTextures()
//     {
//         InitData();

//         int cols = _tilemapInTextures.GetLength(0);
//         int rows = _tilemapInTextures.GetLength(1);

//         Texture2DArray uniqueTexturesArray = CreateUniqueTexturesArray();
//         _tilemapTexturesIdInTexture = new Texture2D(
//             cols,
//             rows,
//             TextureFormat.RGBAFloat,
//             false
//         );
//         _tilemapTexturesIdInTexture.filterMode = FilterMode.Point;
//         _tilemapTexturesIdInTexture.wrapMode = TextureWrapMode.Clamp;

//         for (int y = 0; y < rows; y++)
//         {
//             for (int x = 0; x < cols; x++)
//             {
//                 Texture2D texture = _tilemapInTextures[x, y];
//                 if (texture != null)
//                 {
//                     for(int i = 0; i < _uniqueTextures.Count; i++)
//                     {
//                         if(texture == _uniqueTextures[i])
//                         {
//                             float textureIdInFloat = (float)i * 0.01f;
//                             Color pixel = new Color(textureIdInFloat, 0, 0, 1);
//                             _tilemapTexturesIdInTexture.SetPixel(x,y, pixel);
//                         }
//                     }
//                 }
//             }
//         }
//         _tilemapTexturesIdInTexture.Apply();

//         _material = _spriteWithShader.sharedMaterial;
//         _material = new Material(_material.shader);
//         _spriteWithShader.gameObject.transform.localScale = new Vector3(cols, rows, 1);
//         _material.SetInt("_Rows", rows);
//         _material.SetInt("_Columns", cols);
//         _material.SetInt("_UniqueTexturesCount", _uniqueTextures.Count);
//         _material.SetTexture("_UniqueTextures", uniqueTexturesArray);
//         _material.SetTexture("_TilemapInTexture", _tilemapTexturesIdInTexture);
//         _spriteWithShader.sharedMaterial = _material;
//         ChangeVolatileShaderParams();
//     }
//     void OnValidate()
//     {
//         if(_material == null)
//         {
//             _material = _spriteWithShader.sharedMaterial;
//         }
//         ChangeVolatileShaderParams();
//     }
//     private void InitData()
//     {
//         CalculateTilemapValues();

//         CheckTileResolution();

//         FindAllTextures();
//     }
//     private void CalculateTilemapValues()
//     {
//         _xMin = int.MaxValue;
//         _xMax = int.MinValue;
//         _yMin = int.MaxValue;
//         _yMax = int.MinValue;

//         foreach (var position in _tilemap.cellBounds.allPositionsWithin)
//         {
//             if (_tilemap.HasTile(position))
//             {
//                 _xMin = Mathf.Min(_xMin, position.x);
//                 _xMax = Mathf.Max(_xMax, position.x);
//                 _yMin = Mathf.Min(_yMin, position.y);
//                 _yMax = Mathf.Max(_yMax, position.y);
//             }
//         }

//         _widthInTiles = _xMax - _xMin + 1;
//         _heightInTiles = _yMax - _yMin + 1;
//         _widthInPixels = _widthInTiles * _tileResolution;
//         _heightInPixels = _heightInTiles * _tileResolution;
//         return;
//     }
//     private void CheckTileResolution()
//     {
//         Vector3Int firstTilePos = new Vector3Int(_xMin, _yMin, 0);
//         Sprite firstSprite = _tilemap.GetSprite(firstTilePos);
//         if (firstSprite != null && firstSprite.texture != null)
//         {
//             _tileResolution = firstSprite.texture.width;
//         }
//     }
//     private void FindAllTextures()
//     {
//         _tilemapInTextures = new Texture2D[_widthInTiles, _heightInTiles];
//         for (int i = _xMin, column = 0; i <= _xMax; i++, column++)
//         {
//             for (int j = _yMin, row = 0; j <= _yMax; j++, row++)
//             {
//                 Vector3Int tilePos = new Vector3Int(i, j, 0);
//                 Texture2D texture = _tilemap.GetSprite(tilePos).texture;
//                 if (!_uniqueTextures.Contains(texture))
//                 {
//                     _uniqueTextures.Add(texture);
//                 }
//                 _tilemapInTextures[column, row] = texture;
//             }
//         }
//         InitNoiseRules();
//     }
//     private void InitNoiseRules()
//     {
//         int noiseRulesCount = _noiseRules.GetLength(0);
//         if(noiseRulesCount < _uniqueTextures.Count)
//         {
//             TextureNoiseRule[] noiseRules = new TextureNoiseRule[_uniqueTextures.Count];
//             for(int i = 0; i < noiseRulesCount; i++)
//             {
//                 noiseRules[i] = _noiseRules[i];
//             }
//             for(int i = noiseRulesCount; i < _uniqueTextures.Count;i++)
//             {
//                 TextureNoiseRule newNoiseRule = new TextureNoiseRule();
//                 noiseRules[i] = newNoiseRule;
//             }
//             _noiseRules = noiseRules;
//         }

//         for(int i = 0; i < _uniqueTextures.Count; i++)
//         {
//             _noiseRules[i].Texture = _uniqueTextures[i];
//         }
//     }
//     private void ChangeVolatileShaderParams()
//     {
//         _material.SetFloat("_FalloffPower", _falloffPower);
//         _material.SetFloat("_FadeDistance", _fadeDistance);
//         SetShaderNoiseRules();
//         SetShaderGrassNoiseRules();
//     }
//     private void SetShaderNoiseRules()
//     {
//         int count = _noiseRules.Length;
//         float[] noiseSizes = new float[count];
//         float[] colorDeltas = new float[count];
//         float[] xOffsets = new float[count];
//         float[] yOffsets = new float[count];
//         float[] includeHue = new float[count];
//         float[] includeSaturation = new float[count];
//         float[] includeBrightness = new float[count];
        
//         for (int i = 0; i < count; i++)
//         {
//             noiseSizes[i] = _noiseRules[i].NoiseSize;
//             colorDeltas[i] = _noiseRules[i].ColorDelta;
//             xOffsets[i] = _noiseRules[i].XOffset;
//             yOffsets[i] = _noiseRules[i].YOffset;
//             includeHue[i] = _noiseRules[i].IncludeHue ? 1f : 0f;
//             includeSaturation[i] = _noiseRules[i].IncludeSaturation ? 1f : 0f;
//             includeBrightness[i] = _noiseRules[i].IncludeBrightness ? 1f : 0f;
//         }

//         _material.SetInt("_NoiseRulesCount", count);
//         _material.SetFloatArray("_NoiseSizes", noiseSizes);
//         _material.SetFloatArray("_ColorDeltas", colorDeltas);
//         _material.SetFloatArray("_XOffsets", xOffsets);
//         _material.SetFloatArray("_YOffsets", yOffsets);
//         _material.SetFloatArray("_IncludeHue", includeHue);
//         _material.SetFloatArray("_IncludeSaturation", includeSaturation);
//         _material.SetFloatArray("_IncludeBrightness", includeBrightness);
//     }
//     private void SetShaderGrassNoiseRules()
//     {
//         _material.SetFloat("_GrassNoiseSize", _grassNoiseSize);
//         _material.SetFloat("_GrassNoiseIntencity", _grassNoiseIntencity);
//         _material.SetFloat("_GrassNoiseXOffset", _grassNoiseXOffset);
//         _material.SetFloat("_GrassNoiseYOffset", _grassNoiseYOffset);
//     }
//     private Texture2DArray CreateUniqueTexturesArray()
//     {
//         Texture2DArray uniqueTexturesArray = new Texture2DArray(
//             _tileResolution, _tileResolution,
//             _uniqueTextures.Count,
//             TextureFormat.RGBA32,
//             false
//         );
//         for(int i = 0; i < _uniqueTextures.Count;i++)
//         {
//             uniqueTexturesArray.SetPixels(_uniqueTextures[i].GetPixels(), i);
//         }
//         uniqueTexturesArray.Apply();
//         return uniqueTexturesArray;
//     }
// }
