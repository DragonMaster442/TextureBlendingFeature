using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapWithNoiseAndGrassShaderCreator : MonoBehaviour
{
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private List<Texture2D> _uniqueTextures;
    [SerializeField] private Texture2D[,] _tilemapInTextures;
    [SerializeField] private Texture2D _tilemapTexturesIdInTexture;
    [SerializeField] private SpriteRenderer _spriteWithShader;
    [Range(0.01f, 3)]
    [SerializeField] private float _falloffPower = 0.01f;
    [Range(1, 2)]
    [SerializeField] private float _fadeDistance = 1.5f;    
    [SerializeField] private TextureNoiseRule[] _noiseRules;
    [SerializeField] private int _tileResolution = 1024;
    [SerializeField] private Texture2D _grassLayer1;
    [Range(0, 1)]
    [SerializeField] private float _grassLayer1Threshold = 0.75f;
    [SerializeField] private GrassNoiseRule _grassLayer1NoiseRule;
    [SerializeField] private Texture2D _grassLayer2;
    [Range(0, 1)]
    [SerializeField] private float _grassLayer2Threshold = 0.5f;
    [SerializeField] private GrassNoiseRule _grassLayer2NoiseRule;
    [SerializeField] private Texture2D _grassLayer3;
    [Range(0, 1)]
    [SerializeField] private float _grassLayer3Threshold = 0.25f;
    [SerializeField] private GrassNoiseRule _grassLayer3NoiseRule;
    [SerializeField] private int _grassBaseLayerID = 1;
    [Range(0.5f, 10)]
    [Tooltip("Меняет расположение заднего слоя травы относительно смежных тайлов")]
    [SerializeField] private float _grassBaseLayerDenominator = 2;
    private int _widthInTiles;
    private int _heightInTiles;
    private int _xMin;
    private int _xMax;
    private int _yMin;
    private int _yMax;
    private Material _material;

    public void LoadTextures()
    {
        InitData();

        int cols = _tilemapInTextures.GetLength(0);
        int rows = _tilemapInTextures.GetLength(1);

        Texture2DArray uniqueTexturesArray = CreateUniqueTexturesArray();
        _tilemapTexturesIdInTexture = new Texture2D(
            cols,
            rows,
            TextureFormat.RGBAFloat,
            false
        );
        _tilemapTexturesIdInTexture.filterMode = FilterMode.Point;
        _tilemapTexturesIdInTexture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Texture2D texture = _tilemapInTextures[x, y];
                if (texture != null)
                {
                    for(int i = 0; i < _uniqueTextures.Count; i++)
                    {
                        if(texture == _uniqueTextures[i])
                        {
                            float textureIdInFloat = (float)i * 0.01f;
                            Color pixel = new Color(textureIdInFloat, 0, 0, 1);
                            _tilemapTexturesIdInTexture.SetPixel(x,y, pixel);
                        }
                    }
                }
            }
        }
        _tilemapTexturesIdInTexture.Apply();

        _material = _spriteWithShader.sharedMaterial;
        _material = new Material(_material.shader);
        _spriteWithShader.gameObject.transform.localScale = new Vector3(cols, rows, 1);
        _material.SetInt("_Rows", rows);
        _material.SetInt("_Columns", cols);
        _material.SetInt("_UniqueTexturesCount", _uniqueTextures.Count);
        _material.SetTexture("_UniqueTextures", uniqueTexturesArray);
        _material.SetTexture("_TilemapInTexture", _tilemapTexturesIdInTexture);
        _spriteWithShader.sharedMaterial = _material;
        ChangeVolatileShaderParams();
    }
    void OnValidate()
    {
        if(_material == null)
        {
            _material = _spriteWithShader.sharedMaterial;
        }
        ChangeVolatileShaderParams();
    }
    private void InitData()
    {
        CalculateTilemapValues();

        CheckTileResolution();

        FindAllTextures();
    }
    private void CalculateTilemapValues()
    {
        _xMin = int.MaxValue;
        _xMax = int.MinValue;
        _yMin = int.MaxValue;
        _yMax = int.MinValue;

        foreach (var position in _tilemap.cellBounds.allPositionsWithin)
        {
            if (_tilemap.HasTile(position))
            {
                _xMin = Mathf.Min(_xMin, position.x);
                _xMax = Mathf.Max(_xMax, position.x);
                _yMin = Mathf.Min(_yMin, position.y);
                _yMax = Mathf.Max(_yMax, position.y);
            }
        }

        _widthInTiles = _xMax - _xMin + 1;
        _heightInTiles = _yMax - _yMin + 1;
        return;
    }
    private void CheckTileResolution()
    {
        Vector3Int firstTilePos = new Vector3Int(_xMin, _yMin, 0);
        Sprite firstSprite = _tilemap.GetSprite(firstTilePos);
        if (firstSprite != null && firstSprite.texture != null)
        {
            _tileResolution = firstSprite.texture.width;
        }
    }
    private void FindAllTextures()
    {
        _tilemapInTextures = new Texture2D[_widthInTiles, _heightInTiles];
        for (int i = _xMin, column = 0; i <= _xMax; i++, column++)
        {
            for (int j = _yMin, row = 0; j <= _yMax; j++, row++)
            {
                Vector3Int tilePos = new Vector3Int(i, j, 0);
                Texture2D texture = _tilemap.GetSprite(tilePos).texture;
                if (!_uniqueTextures.Contains(texture))
                {
                    _uniqueTextures.Add(texture);
                }
                _tilemapInTextures[column, row] = texture;
            }
        }
        InitNoiseRules();
    }
    private void InitNoiseRules()
    {
        int noiseRulesCount = _noiseRules.GetLength(0);
        if(noiseRulesCount < _uniqueTextures.Count)
        {
            TextureNoiseRule[] noiseRules = new TextureNoiseRule[_uniqueTextures.Count];
            for(int i = 0; i < noiseRulesCount; i++)
            {
                noiseRules[i] = _noiseRules[i];
            }
            for(int i = noiseRulesCount; i < _uniqueTextures.Count;i++)
            {
                TextureNoiseRule newNoiseRule = new TextureNoiseRule();
                noiseRules[i] = newNoiseRule;
            }
            _noiseRules = noiseRules;
        }

        for(int i = 0; i < _uniqueTextures.Count; i++)
        {
            _noiseRules[i].Texture = _uniqueTextures[i];
        }
    }
    private void ChangeVolatileShaderParams()
    {
        _material.SetFloat("_FalloffPower", _falloffPower);
        _material.SetFloat("_FadeDistance", _fadeDistance);
        _material.SetTexture("_GrassLayer1", _grassLayer1);
        _material.SetTexture("_GrassLayer2", _grassLayer2);
        _material.SetTexture("_GrassLayer3", _grassLayer3);
        _material.SetInt("_GrassBaseLayerID", _grassBaseLayerID);
        _material.SetFloat("_GrassBaseLayerDenominator", _grassBaseLayerDenominator);
        _material.SetFloat("_GrassLayer1Threshold", _grassLayer1Threshold);
        _material.SetFloat("_GrassLayer2Threshold", _grassLayer2Threshold);
        _material.SetFloat("_GrassLayer3Threshold", _grassLayer3Threshold);
        SetShaderNoiseRules();
        SetShaderGrassNoiseRules();
    }
    private void SetShaderNoiseRules()
    {
        int count = _noiseRules.Length;

        float[] hueNoiseSizes = new float[count];
        float[] huePositiveDeltas = new float[count];
        float[] hueNegativeDeltas = new float[count];
        float[] hueXOffsets = new float[count];
        float[] hueYOffsets = new float[count];
        float[] includeHue = new float[count];

        float[] saturationNoiseSizes = new float[count];
        float[] saturationPositiveDeltas = new float[count];
        float[] saturationNegativeDeltas = new float[count];
        float[] saturationXOffsets = new float[count];
        float[] saturationYOffsets = new float[count];
        float[] includeSaturation = new float[count];
        
        float[] brightnessNoiseSizes = new float[count];
        float[] brightnessPositiveDeltas = new float[count];
        float[] brightnessNegativeDeltas = new float[count];
        float[] brightnessXOffsets = new float[count];
        float[] brightnessYOffsets = new float[count];
        float[] includeBrightness = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            NoiseRule hueNoiseRule = _noiseRules[i].HueNoiseRule;
            hueNoiseSizes[i] = hueNoiseRule.NoiseSize;
            huePositiveDeltas[i] = hueNoiseRule.PositiveDelta;
            hueNegativeDeltas[i] = hueNoiseRule.NegativeDelta;
            hueXOffsets[i] = hueNoiseRule.XOffset;
            hueYOffsets[i] = hueNoiseRule.YOffset;
            includeHue[i] = _noiseRules[i].IncludeHue ? 1f : 0f;

            NoiseRule saturationNoiseRule = _noiseRules[i].SaturationNoiseRule;
            saturationNoiseSizes[i] = saturationNoiseRule.NoiseSize;
            saturationPositiveDeltas[i] = saturationNoiseRule.PositiveDelta;
            saturationNegativeDeltas[i] = saturationNoiseRule.NegativeDelta;
            saturationXOffsets[i] = saturationNoiseRule.XOffset;
            saturationYOffsets[i] = saturationNoiseRule.YOffset;
            includeSaturation[i] = _noiseRules[i].IncludeSaturation ? 1f : 0f;

            NoiseRule brightnessNoiseRule = _noiseRules[i].BrightnessNoiseRule;
            brightnessNoiseSizes[i] = brightnessNoiseRule.NoiseSize;
            brightnessPositiveDeltas[i] = brightnessNoiseRule.PositiveDelta;
            brightnessNegativeDeltas[i] = brightnessNoiseRule.NegativeDelta;
            brightnessXOffsets[i] = brightnessNoiseRule.XOffset;
            brightnessYOffsets[i] = brightnessNoiseRule.YOffset;
            includeBrightness[i] = _noiseRules[i].IncludeBrightness ? 1f : 0f;
        }

        _material.SetInt("_NoiseRulesCount", count);

        _material.SetFloatArray("_HueNoiseSizes", hueNoiseSizes);
        _material.SetFloatArray("_HuePositiveDeltas", huePositiveDeltas);
        _material.SetFloatArray("_HueNegativeDeltas", hueNegativeDeltas);
        _material.SetFloatArray("_HueXOffsets", hueXOffsets);
        _material.SetFloatArray("_HueYOffsets", hueYOffsets);
        _material.SetFloatArray("_IncludeHue", includeHue);

        _material.SetFloatArray("_SaturationNoiseSizes", saturationNoiseSizes);
        _material.SetFloatArray("_SaturationPositiveDeltas", saturationPositiveDeltas);
        _material.SetFloatArray("_SaturationNegativeDeltas", saturationNegativeDeltas);
        _material.SetFloatArray("_SaturationXOffsets", saturationXOffsets);
        _material.SetFloatArray("_SaturationYOffsets", saturationYOffsets);
        _material.SetFloatArray("_IncludeSaturation", includeSaturation);

        _material.SetFloatArray("_BrightnessNoiseSizes", brightnessNoiseSizes);
        _material.SetFloatArray("_BrightnessPositiveDeltas", brightnessPositiveDeltas);
        _material.SetFloatArray("_BrightnessNegativeDeltas", brightnessNegativeDeltas);
        _material.SetFloatArray("_BrightnessXOffsets", brightnessXOffsets);
        _material.SetFloatArray("_BrightnessYOffsets", brightnessYOffsets);
        _material.SetFloatArray("_IncludeBrightness", includeBrightness);
    }
    private void SetShaderGrassNoiseRules()
    {
        _material.SetFloat("_GrassLayer1NoiseSize", _grassLayer1NoiseRule.NoiseSize);
        _material.SetFloat("_GrassLayer1NoiseThreshold", _grassLayer1NoiseRule.Threshold);
        _material.SetFloat("_GrassLayer1NoiseXOffset", _grassLayer1NoiseRule.XOffset);
        _material.SetFloat("_GrassLayer1NoiseYOffset", _grassLayer1NoiseRule.YOffset);

        _material.SetFloat("_GrassLayer2NoiseSize", _grassLayer2NoiseRule.NoiseSize);
        _material.SetFloat("_GrassLayer2NoiseThreshold", _grassLayer2NoiseRule.Threshold);
        _material.SetFloat("_GrassLayer2NoiseXOffset", _grassLayer2NoiseRule.XOffset);
        _material.SetFloat("_GrassLayer2NoiseYOffset", _grassLayer2NoiseRule.YOffset);

        _material.SetFloat("_GrassLayer3NoiseSize", _grassLayer3NoiseRule.NoiseSize);
        _material.SetFloat("_GrassLayer3NoiseThreshold", _grassLayer3NoiseRule.Threshold);
        _material.SetFloat("_GrassLayer3NoiseXOffset", _grassLayer3NoiseRule.XOffset);
        _material.SetFloat("_GrassLayer3NoiseYOffset", _grassLayer3NoiseRule.YOffset);
    }
    private Texture2DArray CreateUniqueTexturesArray()
    {
        Texture2DArray uniqueTexturesArray = new Texture2DArray(
            _tileResolution, _tileResolution,
            _uniqueTextures.Count,
            TextureFormat.RGBA32,
            false
        );
        for(int i = 0; i < _uniqueTextures.Count;i++)
        {
            uniqueTexturesArray.SetPixels(_uniqueTextures[i].GetPixels(), i);
        }
        uniqueTexturesArray.Apply();
        return uniqueTexturesArray;
    }
}

[Serializable]
public struct GrassNoiseRule
{
    [Range(0.1f, 10)]
    public float NoiseSize;
    [Range(0, 1)]
    public float Threshold;
    public float XOffset;
    public float YOffset;
    public GrassNoiseRule(float noiseSize = 1, float threshold = 0.1f, float xOffset = 0, float yOffset = 0)
    {
        NoiseSize = noiseSize;
        Threshold = threshold;
        XOffset = xOffset;
        YOffset = yOffset;
    }
}
