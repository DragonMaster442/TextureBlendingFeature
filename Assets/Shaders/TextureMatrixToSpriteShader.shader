Shader "Unlit/TilemapBlendingShaderWithNoise"
{
    Properties
    {
        _UniqueTextures("Unique Textures", 2DArray) = "" {}
        _UniqueTexturesCount("Unique Textures Count", Int) = 0
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
            sampler2D _TilemapInTexture;
            float4 _TilemapInTexture_TexelSize;
            int _NoiseRulesCount;
            int _UniqueTexturesCount;
            float _NoiseSizes[32];
            float _ColorDeltas[32];
            float _XOffsets[32];
            float _YOffsets[32];
            bool _IncludeHue[32];
            bool _IncludeSaturation[32];
            bool _IncludeBrightness[32];

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

            int _Rows;
            int _Columns;
            int _Octaves;
            float _Lacunarity;
            float _Persistence;
            int _TileResolution;
            float _FalloffPower;
            float _FadeDistance;
            
            UNITY_DECLARE_TEX2DARRAY(_Textures);
            UNITY_DECLARE_TEX2DARRAY(_UniqueTextures);

            float2 mod289(float2 x)
            {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float3 mod289(float3 x)
            {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float4 mod289(float4 x)
            {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float3 permute(float3 x)
            {
                return mod289(((x * 34.0) + 1.0) * x);
            }

            float4 permute(float4 x)
            {
                return mod289(((x * 34.0) + 1.0) * x);
            }

            float4 taylorInvSqrt(float4 r)
            {
                return 1.79284291400159 - 0.85373472095314 * r;
            }

            float perlinNoise(float2 v)
            {
                const float4 C = float4(0.211324865405187,  // (3.0-sqrt(3.0))/6.0
                                    0.366025403784439,  // 0.5*(sqrt(3.0)-1.0)
                                    -0.577350269189626,  // -1.0 + 2.0 * C.x
                                    0.024390243902439); // 1.0 / 41.0
                // First corner
                float2 i  = floor(v + dot(v, C.yy));
                float2 x0 = v -   i + dot(i, C.xx);

                // Other corners
                float2 i1;
                i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;

                // Permutations
                i = mod289(i); // Avoid truncation effects in permutation
                float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
                                + i.x + float3(0.0, i1.x, 1.0));

                float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
                m = m * m;
                m = m * m;

                // Gradients: 41 points uniformly over a line, mapped onto a diamond.
                // The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)
                float3 x = 2.0 * frac(p * C.www) - 1.0;
                float3 h = abs(x) - 0.5;
                float3 ox = floor(x + 0.5);
                float3 a0 = x - ox;

                // Normalise gradients implicitly by scaling m
                // Approximation of: m *= inversesqrt(a0*a0 + h*h);
                m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);

                // Compute final noise value at P
                float3 g;
                g.x = a0.x * x0.x + h.x * x0.y;
                g.yz = a0.yz * x12.xz + h.yz * x12.yw;
                return 130.0 * dot(m, g);
            }
            float2 hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                          dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }
            float snoise(float2 p)
            {
                const float K1 = 0.366025404; // (sqrt(3)-1)/2;
                const float K2 = 0.211324865; // (3-sqrt(3))/6;

                float2 i = floor(p + (p.x + p.y) * K1);
                float2 a = p - i + (i.x + i.y) * K2;
                float m = step(a.y, a.x);
                float2 o = float2(m, 1.0 - m);
                float2 b = a - o + K2;
                float2 c = a - 1.0 + 2.0 * K2;
                
                float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
                float3 n = h * h * h * h * float3(dot(a, hash(i)),
                                                  dot(b, hash(i + o)),
                                                  dot(c, hash(i + 1.0)));
                
                return dot(n, float3(70.0, 70.0, 70.0));
            }
            // Улучшенный шум с интерполяцией
            float smooth_noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep interpolation
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Основная функция фрактального шума
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                float max_value = 0.0; // для нормализации
                
                for (int i = 0; i < _Octaves; i++)
                {
                    value += amplitude * smooth_noise(p * frequency);
                    max_value += amplitude;
                    amplitude *= _Persistence;
                    frequency *= _Lacunarity;
                }
                
                return value / max_value; // нормализация к [0,1]
            }
            // Фрактальный шум с симплекс-шумом
            float fbm_simplex(float2 p)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                float max_value = 0.0;
                
                for (int i = 0; i < _Octaves; i++)
                {
                    value += amplitude * snoise(p * frequency);
                    max_value += amplitude;
                    amplitude *= _Persistence;
                    frequency *= _Lacunarity;
                }
                
                return value / max_value;
            }
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float GetRedChannel(float2 pixelCoord) {
                float2 uv = (pixelCoord + 0.5) * _TilemapInTexture_TexelSize.xy;
                float4 color = tex2D(_TilemapInTexture, uv);
                float redValue = color.r * 100.0f;
                
                // Используем floor вместо round
                return round(redValue);
            }
            float calculateIntensity(float2 textureCenterPos, float2 globalPixelPos, int offsetX, int offsetY)
            {
                float maxDistance = _TileResolution * _FadeDistance;
                float2 neighborCenter = textureCenterPos + float2(offsetX * _TileResolution, offsetY * _TileResolution);
                float distance = length(neighborCenter - globalPixelPos);
                float normalizedDistance = distance / maxDistance;
                float value = 1.0 * (1.0 - pow(normalizedDistance, _FalloffPower));
                return clamp(value, 0.0, 1.0);
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

            float4 applyNoiseToColor(float4 color, float2 uv, int textureId)
            {
                float2 offsetUV = uv + float2(_XOffsets[textureId], _YOffsets[textureId]);;
                float noiseValue = fbm_simplex(offsetUV * _NoiseSizes[textureId]);
                
                float3 hsv = rgb2hsv(color.rgb);
                
                float3 minHSV = hsv;
                float3 maxHSV = hsv;

                float colorDelta = _ColorDeltas[textureId];
                if(_IncludeBrightness[textureId])
                {
                    minHSV.z += colorDelta /2;
                    maxHSV.z -= colorDelta;
                }
                if(_IncludeHue[textureId])
                {
                    minHSV.x += colorDelta;
                    maxHSV.x -= colorDelta;
                }
                if(_IncludeSaturation[textureId])
                {
                    minHSV.y += colorDelta / 2;
                    maxHSV.y -= colorDelta;
                }
                
                minHSV.xyz = clamp(minHSV.xyz, 0.0, 1.0);
                maxHSV.xyz = clamp(maxHSV.xyz, 0.0, 1.0);
                
                float3 minColor = hsv2rgb(minHSV);
                float3 maxColor = hsv2rgb(maxHSV);
                
                float3 finalRGB = lerp(minColor, maxColor, noiseValue);
                
                
                return float4(finalRGB, color.a);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 scaledUV = i.uv * float2(_Columns, _Rows);
                
                float2 tileIndex = floor(scaledUV);
                float2 tileUV = frac(scaledUV);
                
                float2 globalPixelPos = i.uv * float2(_Columns * _TileResolution, _Rows * _TileResolution);
                
                float2 textureCenterPos = tileIndex * _TileResolution + float2(_TileResolution * 0.5, _TileResolution * 0.5);
                
                float totalIntensity = 0.0;
                float4 blendedColor = float4(0, 0, 0, 0);
                
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        int neighborTileX = (int)tileIndex.x + offsetX;
                        int neighborTileY = (int)tileIndex.y + offsetY;
                        
                        if (neighborTileX < 0 || neighborTileX >= _Columns || 
                            neighborTileY < 0 || neighborTileY >= _Rows)
                        {
                            continue;
                        }
                        
                        float intensity = calculateIntensity(textureCenterPos, globalPixelPos, offsetX, offsetY);
                        float2 pixelCoord = float2(neighborTileX, neighborTileY);

                        int textureId = GetRedChannel(pixelCoord);
                        float4 neighborColor = UNITY_SAMPLE_TEX2DARRAY(_UniqueTextures, float3(tileUV, textureId));
                        float4 neighborColorWithNoise = applyNoiseToColor(neighborColor, i.uv, textureId);
                        blendedColor += neighborColorWithNoise * intensity;
                        totalIntensity += intensity;
                    }
                }
                if (totalIntensity > 0.0)
                {
                    blendedColor /= totalIntensity;
                }
                else
                {
                    blendedColor = float4(0, 0, 0, 1);
                }
                
                return blendedColor;
            }
            ENDCG
        }
    }
}