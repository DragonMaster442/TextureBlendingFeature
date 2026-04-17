Shader "Unlit/TilemapWithOverlay"
{
    Properties
    {
        _UniqueTextures("Unique Textures", 2DArray) = "" {}
        _OverlayTextures("Overlay Textures", 2DArray) = "" {}
        _TilemapInTexture ("Tilemap In Texture", 2D) = "white" {}
        _Rows ("Rows", Int) = 2
        _Columns ("Columns", Int) = 2
        _TileResolution ("Tile Resolution", Int) = 1024
        _FalloffPower ("Falloff Power", Float) = 2.0
        _FadeDistance ("Fade Distance", Float) = 1.5
        _Octaves ("Octaves", Range(1, 8)) = 4
        _Lacunarity ("Lacunarity", Range(1.5, 4)) = 2.0
        _Persistence ("Persistence", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            // Текстуры
            sampler2D _TilemapInTexture;
            float4 _TilemapInTexture_TexelSize;
            UNITY_DECLARE_TEX2DARRAY(_UniqueTextures);
            UNITY_DECLARE_TEX2DARRAY(_OverlayTextures);

            // Размеры
            int _Rows, _Columns;
            int _TileResolution;
            float _FalloffPower, _FadeDistance;
            int _UniqueTexturesCount;

            // Шум
            int _Octaves;
            float _Lacunarity, _Persistence;

            // HSV-шумы для базовых текстур
            int _NoiseRulesCount;
            float4 _HueData[32];
            float4 _HueData2[32];
            float4 _SatData[32];
            float4 _SatData2[32];
            float4 _BrightData[32];
            float4 _BrightData2[32];

            // Оверлейные слои
            int _MaxOverlays;
            float _OverlayCounts[32];
            float4 _OverlayThresholds[128];
            float4 _OverlayNoiseData[128];
            float _OverlayTextureIndices[128];

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            // ========== Функции шума ==========
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 permute(float4 x) { return mod289(((x*34.0)+1.0)*x); }
            float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

            float2 hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float snoise(float2 p)
            {
                const float K1 = 0.366025404;
                const float K2 = 0.211324865;
                float2 i = floor(p + (p.x + p.y) * K1);
                float2 a = p - i + (i.x + i.y) * K2;
                float m = step(a.y, a.x);
                float2 o = float2(m, 1.0 - m);
                float2 b = a - o + K2;
                float2 c = a - 1.0 + 2.0 * K2;
                float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
                float3 n = h * h * h * h * float3(dot(a, hash(i)), dot(b, hash(i + o)), dot(c, hash(i + 1.0)));
                return dot(n, float3(70.0, 70.0, 70.0));
            }

            float fbm_simplex(float2 p)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                float max_value = 0.0;
                [loop] for (int i = 0; i < _Octaves; i++)
                {
                    value += amplitude * snoise(p * frequency);
                    max_value += amplitude;
                    amplitude *= _Persistence;
                    frequency *= _Lacunarity;
                }
                return value / max_value;
            }

            // ========== Вспомогательные функции ==========
            float GetRedChannel(float2 pixelCoord) {
                float2 uv = (pixelCoord + 0.5) * _TilemapInTexture_TexelSize.xy;
                float4 color = tex2D(_TilemapInTexture, uv);
                return round(color.r * 100.0f);
            }

            float calculateIntensity(float2 textureCenterPos, float2 globalPixelPos, int offsetX, int offsetY)
            {
                float maxDistance = _TileResolution * _FadeDistance;
                float2 neighborCenter = textureCenterPos + float2(offsetX * _TileResolution, offsetY * _TileResolution);
                float distance = length(neighborCenter - globalPixelPos);
                float normalized = distance / maxDistance;
                // exp(-k * normalized²), где k = _FalloffPower * 4 (чтобы на границе было ~0)
                float k = _FalloffPower * 4.0;
                return exp(-k * normalized * normalized);
            }

            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            float CalculateNoiseValue(float2 uv, float xOffset, float yOffset, float noiseSize)
            {
                float2 offsetUV = uv + float2(xOffset, yOffset);
                return fbm_simplex(offsetUV * noiseSize);
            }

            float4 applyNoiseToColor(float4 color, float2 uv, int textureId)
            {
                float3 hsv = rgb2hsv(color.rgb);
                // Hue
                float4 d0 = _HueData[textureId];
                float4 d1 = _HueData2[textureId];
                if (d1.y > 0.5)
                {
                    float noise = CalculateNoiseValue(uv, d0.w, d1.x, d0.x);
                    float minV = max(hsv.x - d0.z, 0.0);
                    float maxV = min(hsv.x + d0.y, 1.0);
                    hsv.x = lerp(maxV, minV, noise);
                }
                // Saturation
                d0 = _SatData[textureId];
                d1 = _SatData2[textureId];
                if (d1.y > 0.5)
                {
                    float noise = CalculateNoiseValue(uv, d0.w, d1.x, d0.x);
                    float minV = max(hsv.y - d0.z, 0.0);
                    float maxV = min(hsv.y + d0.y, 1.0);
                    hsv.y = lerp(maxV, minV, noise);
                }
                // Brightness
                d0 = _BrightData[textureId];
                d1 = _BrightData2[textureId];
                if (d1.y > 0.5)
                {
                    float noise = CalculateNoiseValue(uv, d0.w, d1.x, d0.x);
                    float minV = max(hsv.z - d0.z, 0.0);
                    float maxV = min(hsv.z + d0.y, 1.0);
                    hsv.z = lerp(maxV, minV, noise);
                }
                return float4(hsv2rgb(hsv), color.a);
            }

            // Наложение оверлейных слоёв на произвольный цвет
            float4 ApplyOverlays(float4 color, float2 tileUV, float2 worldUV, int textureId, float tileIntensity)
            {
                float4 result = color;
                int startIdx = textureId * _MaxOverlays;
                int count = (int)_OverlayCounts[textureId];
                [loop] for (int i = 0; i < count; i++)
                {
                    int idx = startIdx + i;
                    float4 threshData = _OverlayThresholds[idx];
                    float4 noiseData = _OverlayNoiseData[idx];
                    float noiseVal = (CalculateNoiseValue(worldUV, noiseData.z, noiseData.w, noiseData.x) + 1.0) / 2.0;

                    if (tileIntensity >= threshData.x && noiseVal >= noiseData.y)
                    {
                        int texIndex = (int)_OverlayTextureIndices[idx];
                        float4 overlayColor = UNITY_SAMPLE_TEX2DARRAY(_OverlayTextures, float3(tileUV, texIndex));
                        overlayColor = applyNoiseToColor(overlayColor, worldUV, textureId);
                        result.rgb = lerp(result.rgb, overlayColor.rgb, overlayColor.a);
                    }
                }
                return result;
            }

            // ========== Вершинный шейдер ==========
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // ========== Фрагментный шейдер ==========
            fixed4 frag(v2f i) : SV_Target
            {
                float2 scaledUV = i.uv * float2(_Columns, _Rows);
                float2 tileIndex = floor(scaledUV);
                float2 tileUV = frac(scaledUV);

                float2 globalPixelPos = i.uv * float2(_Columns * _TileResolution, _Rows * _TileResolution);
                float2 textureCenterPos = tileIndex * _TileResolution + float2(_TileResolution * 0.5, _TileResolution * 0.5);

                // Массивы для уникальных текстур (максимум 32)
                int uniqueTexIds[32];
                float uniqueIntensities[32];
                int uniqueCount = 0;

                // Первый проход: собираем уникальные textureId и суммируем интенсивности
                [unroll] for (int ox = -1; ox <= 1; ox++)
                {
                    [unroll] for (int oy = -1; oy <= 1; oy++)
                    {
                        int nx = (int)tileIndex.x + ox;
                        int ny = (int)tileIndex.y + oy;
                        if (nx < 0 || nx >= _Columns || ny < 0 || ny >= _Rows) continue;

                        float intensity = calculateIntensity(textureCenterPos, globalPixelPos, ox, oy);
                        int texId = (int)GetRedChannel(float2(nx, ny));
                        if (texId >= _UniqueTexturesCount) continue;

                        // Ищем, есть ли уже такой texId в массиве
                        int idx = -1;
                        for (int j = 0; j < uniqueCount; j++)
                        {
                            if (uniqueTexIds[j] == texId)
                            {
                                idx = j;
                                break;
                            }
                        }
                        if (idx >= 0)
                        {
                            uniqueIntensities[idx] += intensity;
                        }
                        else
                        {
                            uniqueTexIds[uniqueCount] = texId;
                            uniqueIntensities[uniqueCount] = intensity;
                            uniqueCount++;
                        }
                    }
                }

                // Сортировка уникальных текстур по убыванию texId (от большего к меньшему)
                // Это гарантирует, что оверлеи от текстур с меньшим индексом будут накладываться поверх.
                for (int index = 0; index < uniqueCount - 1; index++)
                {
                    for (int j = index + 1; j < uniqueCount; j++)
                    {
                        if (uniqueTexIds[index] < uniqueTexIds[j])
                        {
                            // swap texId
                            int tempId = uniqueTexIds[index];
                            uniqueTexIds[index] = uniqueTexIds[j];
                            uniqueTexIds[j] = tempId;
                            // swap intensity
                            float tempInt = uniqueIntensities[index];
                            uniqueIntensities[index] = uniqueIntensities[j];
                            uniqueIntensities[j] = tempInt;
                        }
                    }
                }

                // Смешиваем базовые цвета по суммарным интенсивностям
                float totalIntensity = 0.0;
                float4 blendedColor = float4(0, 0, 0, 0);
                for (int j = 0; j < uniqueCount; j++)
                {
                    float intens = uniqueIntensities[j];
                    totalIntensity += intens;
                    int texId = uniqueTexIds[j];
                    float4 tileColor = UNITY_SAMPLE_TEX2DARRAY(_UniqueTextures, float3(tileUV, texId));
                    tileColor = applyNoiseToColor(tileColor, i.uv, texId);
                    blendedColor += tileColor * intens;
                }

                if (totalIntensity > 0.0)
                    blendedColor /= totalIntensity;
                else
                    blendedColor = float4(0, 0, 0, 1);

                float4 finalColor = blendedColor;

                // Второй проход: накладываем оверлеи для каждой уникальной текстуры, используя суммарную интенсивность
                // Поскольку массив отсортирован по убыванию texId, сначала будут наложены оверлеи от текстур с большим индексом (нижние),
                // а затем от текстур с меньшим индексом (верхние), что соответствует требованию: текстура с индексом 0 поверх всех.
                for (int j = 0; j < uniqueCount; j++)
                {
                    int texId = uniqueTexIds[j];
                    float intensity = uniqueIntensities[j] / totalIntensity;
                    finalColor = ApplyOverlays(finalColor, tileUV, i.uv, texId, intensity);
                }

                return finalColor;
            }
            ENDCG
        }
    }
}