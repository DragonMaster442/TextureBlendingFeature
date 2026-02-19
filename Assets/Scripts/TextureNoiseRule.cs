using System;
using UnityEngine;

[Serializable]
public struct TextureNoiseRule
{
    [ReadOnly]
    public Texture2D Texture;
    public bool IncludeHue;
    public NoiseRule HueNoiseRule;
    public bool IncludeSaturation;
    public NoiseRule SaturationNoiseRule;
    public bool IncludeBrightness;
    public NoiseRule BrightnessNoiseRule;
    public TextureNoiseRule(Texture2D texture,
        bool includeHue = false,
        NoiseRule hueNoiseRule = new NoiseRule(),
        bool includeSaturation = false,
        NoiseRule saturationNoiseRule = new NoiseRule(),
        bool includeBrightness = false,
        NoiseRule brightnessNoiseRule = new NoiseRule())
    {
        Texture = texture;
        IncludeHue = includeHue;
        HueNoiseRule = hueNoiseRule;
        IncludeSaturation = includeSaturation;
        SaturationNoiseRule = saturationNoiseRule;
        IncludeBrightness = includeBrightness;
        BrightnessNoiseRule = brightnessNoiseRule;
    }
}
