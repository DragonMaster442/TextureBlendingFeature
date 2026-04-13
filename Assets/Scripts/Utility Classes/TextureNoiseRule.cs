using System;
using UnityEngine;

[Serializable]
public class TextureNoiseRule
{
    public Texture2D Texture;
    public bool IncludeHue = false;
    public NoiseRule HueNoiseRule = new NoiseRule();
    public bool IncludeSaturation = false;
    public NoiseRule SaturationNoiseRule = new NoiseRule();
    public bool IncludeBrightness = false;
    public NoiseRule BrightnessNoiseRule = new NoiseRule();

    [Tooltip("Оверлейные слои для этого типа тайла (например, трава, грязь, снег)")]
    public OverlayLayer[] overlays = new OverlayLayer[0];
}
