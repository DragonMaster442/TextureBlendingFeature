using System;
using UnityEngine;

[Serializable]
public struct OverlayLayer
{
    public Texture2D overlayTexture;
    [Range(0, 1)] public float threshold;
    public GrassNoiseRule noiseRule;
}