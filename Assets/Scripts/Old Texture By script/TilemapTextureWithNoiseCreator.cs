using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapTextureWithNoiseCreator : MonoBehaviour
{
//     [SerializeField] private Tilemap _tilemap;
//     [SerializeField] private Texture2D _texture;
//     [SerializeField] private Texture2D _textureWithNoise;
//     [SerializeField] private SpriteRenderer _sprite;
//     [SerializeField] private List<Texture2D> _uniqueTextures;
//     [SerializeField] private Texture2D[,] _tilemapInTextures;
//     [SerializeField] private Texture2D _tilemapTexturesIdInTexture;
//     [SerializeField] private SpriteRenderer _spriteWithShader;
//     [Range(0.01f, 3)]
//     [SerializeField] private float _falloffPower;
//     [Range(1, 2)]
//     [SerializeField] private float _fadeDistance = 1.5f;
//     [SerializeField] private TextureNoiseRule[] _noiseRules;
//     [SerializeField] private int _tileResolution = 1024;

//     private int _widthInTiles;
//     private int _heightInTiles;
//     private int _widthInPixels;
//     private int _heightInPixels;
//     private int _xMin;
//     private int _xMax;
//     private int _yMin;
//     private int _yMax;

//     public void GenerateSprite()
//     {
//         InitData();
//         GenerateTextureOutOfTilemap();
//         _texture.Apply();
//         _texture.filterMode = FilterMode.Point;
//         GenerateTextureWithNoise();

//         CreateAndApplySprite();
//     }
//     public void ApplyNoise()
//     {
//         GenerateTextureWithNoise();
//         CreateAndApplySprite();
//     }
     public void LoadTextures()
     {
//         InitData();
     }
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
//     private void GenerateTextureOutOfTilemap()
//     {
//         InitTextureIfNecessary();
//         for (int tilemapColumn = 0; tilemapColumn < _widthInTiles; tilemapColumn++)
//         {
//             for (int tilemapRow = 0; tilemapRow < _heightInTiles; tilemapRow++)
//             {
//                 for (int textureColumn = 0; textureColumn < _tileResolution; textureColumn++)
//                 {
//                     for (int textureRow = 0; textureRow < _tileResolution; textureRow++)
//                     {
//                         Vector2Int texturePosition = new Vector2Int(tilemapColumn, tilemapRow);
//                         Vector2Int pixelPosition = new Vector2Int(textureColumn, textureRow);
//                         Color[,] colors = GetColorMatrix(texturePosition, pixelPosition);
//                         float[,] intencityMatrix = CalculateIntencityMatrix(texturePosition, pixelPosition);
//                         Color resultColor = BlendColors(colors, intencityMatrix);
//                         Vector2Int pixelGlobalPosition = new Vector2Int(tilemapColumn * _tileResolution + pixelPosition.x, tilemapRow * _tileResolution + pixelPosition.y);
//                         _texture.SetPixel(pixelGlobalPosition.x, pixelGlobalPosition.y, resultColor);
//                     }
//                 }
//             }
//         }
//     }
//     private float[,] CalculateIntencityMatrix(Vector2Int texturePos, Vector2Int localPixelPosition)
//     {
//         int x = texturePos.x;
//         int y = texturePos.y;
//         int maxDistance = (int)(_tileResolution * 1.5);
//         Vector2Int globalPixelPosition = localPixelPosition + new Vector2Int(_tileResolution, _tileResolution);
//         float[,] resultMatrix = new float[3, 3];
//         for (int tilemapColumn = x - 1, matrixColumn = 0; tilemapColumn <= x + 1; tilemapColumn++, matrixColumn++)
//         {
//             for (int tilemapRow = y - 1, matrixRow = 0; tilemapRow <= y + 1; tilemapRow++, matrixRow++)
//             {
//                 if (tilemapRow < 0 ||
//                     tilemapColumn < 0 ||
//                     tilemapRow >= _tilemapInTextures.GetLength(1) ||
//                     tilemapColumn >= _tilemapInTextures.GetLength(0)
//                     )
//                 {
//                     resultMatrix[matrixColumn, matrixRow] = 0;
//                     continue;
//                 }
//                 Vector2 textureCenterPosition = new Vector2Int(matrixColumn * _tileResolution, matrixRow * _tileResolution)
//                 + new Vector2Int(_tileResolution / 2, _tileResolution / 2);

//                 float distance = Vector2.Distance(textureCenterPosition, globalPixelPosition);
//                 float normalizedDistance = distance / maxDistance;

//                 // Используем степенную функцию для контроля затухания
//                 float value = 1 * (1f - Mathf.Pow(normalizedDistance, _falloffPower));
//                 resultMatrix[matrixColumn, matrixRow] = Mathf.Clamp01(value);
//             }
//         }
//         return resultMatrix;
//     }
//     private Color[,] GetColorMatrix(Vector2Int texturePos, Vector2Int localPixelPosition)
//     {
//         int x = texturePos.x;
//         int y = texturePos.y;
//         int pixelX = localPixelPosition.x;
//         int pixelY = localPixelPosition.y;
//         Color[,] resultMatrix = new Color[3, 3];
//         for (int tilemapColumn = x - 1, matrixColumn = 0; tilemapColumn <= x + 1; tilemapColumn++, matrixColumn++)
//         {
//             for (int tilemapRow = y - 1, matrixRow = 0; tilemapRow <= y + 1; tilemapRow++, matrixRow++)
//             {
//                 if (tilemapRow < 0 ||
//                     tilemapColumn < 0 ||
//                     tilemapRow >= _tilemapInTextures.GetLength(1) ||
//                     tilemapColumn >= _tilemapInTextures.GetLength(0)
//                     )
//                 {
//                     resultMatrix[matrixColumn, matrixRow] = Color.black;
//                     continue;
//                 }
//                 Texture2D texture = _tilemapInTextures[tilemapColumn, tilemapRow];
//                 Color pixel = texture.GetPixel(pixelX, pixelY);
//                 resultMatrix[matrixColumn, matrixRow] = pixel;
//             }
//         }
//         return resultMatrix;
//     }
//     private Color BlendColors(Color[,] colorMatrix, float[,] intencityMatrix)
//     {
//         float totalIntencity = 0f;
//         for (int x = 0; x < 3; x++)
//         {
//             for (int y = 0; y < 3; y++)
//                 totalIntencity += intencityMatrix[x, y];
//         }

//         if (totalIntencity <= Mathf.Epsilon)
//             return Color.black;

//         float r = 0f, g = 0f, b = 0f, a = 0f;

//         for (int x = 0; x < 3; x++)
//         {
//             for (int y = 0; y < 3; y++)
//             {
//                 float normalizedIntencity = intencityMatrix[x, y] / totalIntencity;
//                 r += colorMatrix[x, y].r * normalizedIntencity;
//                 g += colorMatrix[x, y].g * normalizedIntencity;
//                 b += colorMatrix[x, y].b * normalizedIntencity;
//                 a += colorMatrix[x, y].a * normalizedIntencity;
//             }
//         }

//         return new Color(r, g, b, a);
//     }
//     private void InitTextureIfNecessary()
//     {
//         if (_texture == null || _texture.width != _widthInPixels || _texture.height != _heightInPixels)
//         {
//             _texture = new Texture2D(_widthInPixels, _heightInPixels);
//             _texture.filterMode = FilterMode.Point;
//         }
//     }
//     private void GenerateTextureWithNoise()
//     {
//         _textureWithNoise = new Texture2D(_texture.width, _texture.height);

//         for (int x = 0; x < _widthInPixels; x++)
//         {
//             for (int y = 0; y < _heightInPixels; y++)
//             {
//                 Color color = _texture.GetPixel(x, y);
//                 float xPos = (float)x / _tileResolution;
//                 float yPos = (float)y / _tileResolution;
//                 Color newColor = ApplyNoiseToColor(color, xPos, yPos);
//                 _textureWithNoise.SetPixel(x, y, newColor);
//             }
//         }
//         _textureWithNoise.Apply();
//         _textureWithNoise.filterMode = FilterMode.Point;
//     }
//     private Color ApplyNoiseToColor(Color color, float x, float y)
//     {

//         float noiseValue = Mathf.PerlinNoise(x * _noiseRules[0].NoiseSize, y * _noiseRules[0].NoiseSize);
//         Color minColor = new Color(
//             color.r,
//             color.g,
//             color.b,
//             color.a
//         );
//         Color.RGBToHSV(minColor, out float minH, out float minS, out float minV);
//         minS += _noiseRules[0].ColorDelta;
//         minV += _noiseRules[0].ColorDelta;
//         minColor = Color.HSVToRGB(minH, minS, minV);
//         Color maxColor = new Color(
//              color.r,
//              color.g,
//              color.b,
//              color.a
//         );
//         Color.RGBToHSV(maxColor, out float maxH, out float maxS, out float maxV);
//         maxS -= _noiseRules[0].ColorDelta;
//         maxV -= _noiseRules[0].ColorDelta;
//         maxColor = Color.HSVToRGB(maxH, maxS, maxV);
//         return Color.Lerp(minColor, maxColor, noiseValue);
//     }
//     private void CreateAndApplySprite()
//     {
//         Sprite newSprite = Sprite.Create(
//             _textureWithNoise,
//             new Rect(0, 0, _widthInPixels, _heightInPixels),
//             new Vector2(0.5f, 0.5f),
//             _tileResolution 
//         );

//         _sprite.sprite = newSprite;
//     }
}