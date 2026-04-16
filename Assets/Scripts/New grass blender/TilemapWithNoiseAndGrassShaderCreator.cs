using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapWithNoiseAndGrassShaderCreator : MonoBehaviour
{
    private const int MAX_TEXTURES = 32;
    private const int MAX_OVERLAYS = 4;

    [Header("Tilemap")]
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private SpriteRenderer _spriteWithShader;

    [Header("Tile Resolution")]
    [SerializeField] private int _tileResolution = 1024;

    [Header("Shader blending")]
    [Range(0.01f, 3)] [SerializeField] private float _falloffPower = 0.01f;
    [Range(1, 2)]     [SerializeField] private float _fadeDistance = 1.5f;

    [Header("Base Textures (Unique per tile type)")]
    [SerializeField] private TextureNoiseRule[] _noiseRules;

    private Texture2D[,] _tilemapInTextures;
    private Texture2D _tilemapTexturesIdInTexture;
    private Material _material;
    private MaterialPropertyBlock _propertyBlock;
    private int _widthInTiles, _heightInTiles;
    private int _xMin, _xMax, _yMin, _yMax;
    private List<Texture2D> _uniqueTextures = new List<Texture2D>();
    private List<Texture2D> _uniqueOverlayTextures = new List<Texture2D>();
    private Texture2DArray _overlayTexturesArray;

    private bool _isReordering = false;  // Защита от рекурсии

    public void LoadTextures()
    {
        InitData();
        int cols = _tilemapInTextures.GetLength(0);
        int rows = _tilemapInTextures.GetLength(1);

        Texture2DArray uniqueTexturesArray = CreateUniqueTexturesArray();

        _tilemapTexturesIdInTexture = new Texture2D(cols, rows, TextureFormat.RGBAFloat, false);
        _tilemapTexturesIdInTexture.filterMode = FilterMode.Point;
        _tilemapTexturesIdInTexture.wrapMode = TextureWrapMode.Clamp;

        // Заполняем карту идентификаторов
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Texture2D tex = _tilemapInTextures[x, y];
                if (tex != null)
                {
                    int id = _uniqueTextures.IndexOf(tex);
                    if (id >= 0)
                        _tilemapTexturesIdInTexture.SetPixel(x, y, new Color(id * 0.01f, 0, 0, 1));
                }
            }
        }
        _tilemapTexturesIdInTexture.Apply();

        BuildOverlayTextureArray();

        if (_material == null)
            _material = new Material(_spriteWithShader.sharedMaterial);
        _spriteWithShader.sharedMaterial = _material;
        _spriteWithShader.gameObject.transform.localScale = new Vector3(cols, rows, 1);

        _material.SetInt("_Rows", rows);
        _material.SetInt("_Columns", cols);
        _material.SetInt("_UniqueTexturesCount", _uniqueTextures.Count);
        _material.SetTexture("_UniqueTextures", uniqueTexturesArray);
        _material.SetTexture("_TilemapInTexture", _tilemapTexturesIdInTexture);
        _material.SetTexture("_OverlayTextures", _overlayTexturesArray);
        _material.SetInt("_MaxOverlays", MAX_OVERLAYS);

        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();
        else
            _propertyBlock.Clear();

        UpdateDynamicShaderParams();
        _spriteWithShader.SetPropertyBlock(_propertyBlock);
    }

    private void OnValidate()
    {
        if (_isReordering) return;
        if (_propertyBlock != null && _spriteWithShader != null)
        {
            // Если тайлмапа уже загружена и размеры совпадают, переупорядочиваем текстуры согласно правилам
            if (_uniqueTextures.Count > 0 && _noiseRules != null && _noiseRules.Length == _uniqueTextures.Count)
            {
                ReorderTexturesByRules();
            }
            UpdateDynamicShaderParams();
            _spriteWithShader.SetPropertyBlock(_propertyBlock);
        }
    }

    private void InitData()
    {
        CalculateTilemapBounds();
        CheckTileResolution();
        CollectAllTexturesAndOverlays();
        EnsureNoiseRulesLength();
    }

    private void CalculateTilemapBounds()
    {
        _xMin = _xMax = _yMin = _yMax = 0;
        bool first = true;
        foreach (var pos in _tilemap.cellBounds.allPositionsWithin)
        {
            if (_tilemap.HasTile(pos))
            {
                if (first)
                {
                    _xMin = _xMax = pos.x;
                    _yMin = _yMax = pos.y;
                    first = false;
                }
                else
                {
                    _xMin = Mathf.Min(_xMin, pos.x);
                    _xMax = Mathf.Max(_xMax, pos.x);
                    _yMin = Mathf.Min(_yMin, pos.y);
                    _yMax = Mathf.Max(_yMax, pos.y);
                }
            }
        }
        _widthInTiles = _xMax - _xMin + 1;
        _heightInTiles = _yMax - _yMin + 1;
    }

    private void CheckTileResolution()
    {
        Vector3Int firstPos = new Vector3Int(_xMin, _yMin, 0);
        Sprite firstSprite = _tilemap.GetSprite(firstPos);
        if (firstSprite != null && firstSprite.texture != null)
            _tileResolution = firstSprite.texture.width;
    }

    private void CollectAllTexturesAndOverlays()
    {
        // Не очищаем _uniqueTextures и _uniqueOverlayTextures, а только дополняем
        _tilemapInTextures = new Texture2D[_widthInTiles, _heightInTiles];

        for (int i = _xMin, col = 0; i <= _xMax; i++, col++)
        {
            for (int j = _yMin, row = 0; j <= _yMax; j++, row++)
            {
                Vector3Int pos = new Vector3Int(i, j, 0);
                Texture2D tex = _tilemap.GetSprite(pos)?.texture;
                if (tex == null) continue;

                if (!_uniqueTextures.Contains(tex))
                    _uniqueTextures.Add(tex);
                _tilemapInTextures[col, row] = tex;
            }
        }

        if (_noiseRules != null)
        {
            foreach (var rule in _noiseRules)
            {
                if (rule != null && rule.overlays != null)
                {
                    foreach (var ov in rule.overlays)
                    {
                        if (ov.overlayTexture != null && !_uniqueOverlayTextures.Contains(ov.overlayTexture))
                            _uniqueOverlayTextures.Add(ov.overlayTexture);
                    }
                }
            }
        }
    }

    private void EnsureNoiseRulesLength()
    {
        int neededLength = _uniqueTextures.Count;
        if (_noiseRules == null)
        {
            _noiseRules = new TextureNoiseRule[neededLength];
            for (int i = 0; i < neededLength; i++)
                _noiseRules[i] = new TextureNoiseRule();
        }
        else if (_noiseRules.Length != neededLength)
        {
            var newRules = new TextureNoiseRule[neededLength];
            for (int i = 0; i < Mathf.Min(_noiseRules.Length, neededLength); i++)
                newRules[i] = _noiseRules[i] ?? new TextureNoiseRule();
            for (int i = _noiseRules.Length; i < neededLength; i++)
                newRules[i] = new TextureNoiseRule();
            _noiseRules = newRules;
        }

        for (int i = 0; i < _uniqueTextures.Count; i++)
            _noiseRules[i].Texture = _uniqueTextures[i];
    }

    private Texture2DArray CreateUniqueTexturesArray()
    {
        var arr = new Texture2DArray(_tileResolution, _tileResolution, _uniqueTextures.Count, TextureFormat.RGBA32, false);
        for (int i = 0; i < _uniqueTextures.Count; i++)
            arr.SetPixels(_uniqueTextures[i].GetPixels(), i);
        arr.Apply();
        return arr;
    }

    private void BuildOverlayTextureArray()
    {
        if (_uniqueOverlayTextures.Count == 0)
        {
            _overlayTexturesArray = new Texture2DArray(1, 1, 1, TextureFormat.RGBA32, false);
            _overlayTexturesArray.SetPixels(new Color[] { Color.clear }, 0);
            _overlayTexturesArray.Apply();
            return;
        }

        _overlayTexturesArray = new Texture2DArray(_tileResolution, _tileResolution, _uniqueOverlayTextures.Count, TextureFormat.RGBA32, false);
        for (int i = 0; i < _uniqueOverlayTextures.Count; i++)
            _overlayTexturesArray.SetPixels(_uniqueOverlayTextures[i].GetPixels(), i);
        _overlayTexturesArray.Apply();
    }

    private void UpdateDynamicShaderParams()
    {
        if (_propertyBlock == null) return;

        _propertyBlock.SetFloat("_FalloffPower", _falloffPower);
        _propertyBlock.SetFloat("_FadeDistance", _fadeDistance);

        SetShaderNoiseRules();
        SetShaderOverlayRules();
    }

    private void SetShaderNoiseRules()
    {
        int count = Mathf.Min(_uniqueTextures.Count, MAX_TEXTURES);
        _propertyBlock.SetInt("_NoiseRulesCount", count);

        Vector4[] hueData0 = new Vector4[MAX_TEXTURES];
        Vector4[] hueData1 = new Vector4[MAX_TEXTURES];
        Vector4[] satData0 = new Vector4[MAX_TEXTURES];
        Vector4[] satData1 = new Vector4[MAX_TEXTURES];
        Vector4[] brightData0 = new Vector4[MAX_TEXTURES];
        Vector4[] brightData1 = new Vector4[MAX_TEXTURES];

        for (int i = 0; i < MAX_TEXTURES; i++)
        {
            if (i < count && _noiseRules[i] != null)
            {
                var rule = _noiseRules[i];
                hueData0[i] = new Vector4(rule.HueNoiseRule.NoiseSize, rule.HueNoiseRule.PositiveDelta, rule.HueNoiseRule.NegativeDelta, rule.HueNoiseRule.XOffset);
                hueData1[i] = new Vector4(rule.HueNoiseRule.YOffset, rule.IncludeHue ? 1f : 0f, 0, 0);
                satData0[i] = new Vector4(rule.SaturationNoiseRule.NoiseSize, rule.SaturationNoiseRule.PositiveDelta, rule.SaturationNoiseRule.NegativeDelta, rule.SaturationNoiseRule.XOffset);
                satData1[i] = new Vector4(rule.SaturationNoiseRule.YOffset, rule.IncludeSaturation ? 1f : 0f, 0, 0);
                brightData0[i] = new Vector4(rule.BrightnessNoiseRule.NoiseSize, rule.BrightnessNoiseRule.PositiveDelta, rule.BrightnessNoiseRule.NegativeDelta, rule.BrightnessNoiseRule.XOffset);
                brightData1[i] = new Vector4(rule.BrightnessNoiseRule.YOffset, rule.IncludeBrightness ? 1f : 0f, 0, 0);
            }
            else
            {
                hueData0[i] = Vector4.zero;
                hueData1[i] = Vector4.zero;
                satData0[i] = Vector4.zero;
                satData1[i] = Vector4.zero;
                brightData0[i] = Vector4.zero;
                brightData1[i] = Vector4.zero;
            }
        }

        _propertyBlock.SetVectorArray("_HueData", hueData0);
        _propertyBlock.SetVectorArray("_HueData2", hueData1);
        _propertyBlock.SetVectorArray("_SatData", satData0);
        _propertyBlock.SetVectorArray("_SatData2", satData1);
        _propertyBlock.SetVectorArray("_BrightData", brightData0);
        _propertyBlock.SetVectorArray("_BrightData2", brightData1);
    }

    private void SetShaderOverlayRules()
    {
        int totalEntries = MAX_TEXTURES * MAX_OVERLAYS;
        Vector4[] overlayThresholds = new Vector4[totalEntries];
        Vector4[] overlayNoiseData = new Vector4[totalEntries];
        int[] overlayCounts = new int[MAX_TEXTURES];
        int[] overlayTextureIndices = new int[totalEntries];

        Dictionary<Texture2D, int> texToIndex = new Dictionary<Texture2D, int>();
        for (int i = 0; i < _uniqueOverlayTextures.Count; i++)
            texToIndex[_uniqueOverlayTextures[i]] = i;

        for (int texId = 0; texId < MAX_TEXTURES; texId++)
        {
            if (texId >= _uniqueTextures.Count || _noiseRules[texId] == null)
            {
                overlayCounts[texId] = 0;
                continue;
            }

            var overlays = _noiseRules[texId].overlays;
            int cnt = (overlays != null) ? Mathf.Min(overlays.Length, MAX_OVERLAYS) : 0;
            overlayCounts[texId] = cnt;

            for (int l = 0; l < cnt; l++)
            {
                int idx = texId * MAX_OVERLAYS + l;
                var ov = overlays[l];
                overlayThresholds[idx] = new Vector4(ov.threshold, 0, 0, 0);
                overlayNoiseData[idx] = new Vector4(ov.noiseRule.NoiseSize, ov.noiseRule.Threshold, ov.noiseRule.XOffset, ov.noiseRule.YOffset);
                overlayTextureIndices[idx] = texToIndex.GetValueOrDefault(ov.overlayTexture, 0);
            }
        }

        float[] overlayCountsFloat = Array.ConvertAll(overlayCounts, v => (float)v);
        float[] overlayTextureIndicesFloat = Array.ConvertAll(overlayTextureIndices, v => (float)v);

        _propertyBlock.SetFloatArray("_OverlayCounts", overlayCountsFloat);
        _propertyBlock.SetVectorArray("_OverlayThresholds", overlayThresholds);
        _propertyBlock.SetVectorArray("_OverlayNoiseData", overlayNoiseData);
        _propertyBlock.SetFloatArray("_OverlayTextureIndices", overlayTextureIndicesFloat);
    }

    // ========== НОВЫЕ МЕТОДЫ ДЛЯ ПЕРЕУПОРЯДОЧИВАНИЯ ==========
    private void ReorderTexturesByRules()
    {
        // Проверяем, нужно ли переупорядочивать
        bool needReorder = false;
        for (int i = 0; i < _noiseRules.Length; i++)
        {
            if (_noiseRules[i].Texture != _uniqueTextures[i])
            {
                needReorder = true;
                break;
            }
        }
        if (!needReorder) return;

        _isReordering = true;

        // Строим новый список текстур в порядке правил
        List<Texture2D> newUniqueTextures = new List<Texture2D>();
        for (int i = 0; i < _noiseRules.Length; i++)
        {
            Texture2D tex = _noiseRules[i].Texture;
            if (tex != null && !newUniqueTextures.Contains(tex))
                newUniqueTextures.Add(tex);
            else if (tex == null)
                Debug.LogWarning("Texture is null in noise rule at index " + i);
        }
        // Добавляем недостающие текстуры (если какие-то из старого списка отсутствуют в правилах)
        foreach (var tex in _uniqueTextures)
        {
            if (!newUniqueTextures.Contains(tex))
                newUniqueTextures.Add(tex);
        }
        _uniqueTextures = newUniqueTextures;

        // Обновляем карту идентификаторов тайлов
        UpdateTextureIdMap();

        // Пересоздаём Texture2DArray для уникальных текстур
        Texture2DArray uniqueTexturesArray = CreateUniqueTexturesArray();
        _material.SetTexture("_UniqueTextures", uniqueTexturesArray);
        _material.SetInt("_UniqueTexturesCount", _uniqueTextures.Count);

        // Привязываем правильные текстуры к правилам
        for (int i = 0; i < _noiseRules.Length; i++)
        {
            _noiseRules[i].Texture = _uniqueTextures[i];
        }

        // Обновляем шейдерные данные
        SetShaderNoiseRules();
        SetShaderOverlayRules();

        _isReordering = false;
    }

    private void UpdateTextureIdMap()
    {
        if (_tilemapTexturesIdInTexture == null) return;
        int cols = _tilemapTexturesIdInTexture.width;
        int rows = _tilemapTexturesIdInTexture.height;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Texture2D tex = _tilemapInTextures[x, y];
                if (tex != null)
                {
                    int id = _uniqueTextures.IndexOf(tex);
                    if (id >= 0)
                        _tilemapTexturesIdInTexture.SetPixel(x, y, new Color(id * 0.01f, 0, 0, 1));
                }
            }
        }
        _tilemapTexturesIdInTexture.Apply();
    }
}