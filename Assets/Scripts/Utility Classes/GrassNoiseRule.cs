using System;
using UnityEngine;

[Serializable]
public struct GrassNoiseRule
{
    [Range(0.1f, 100)] public float NoiseSize;
    [Range(0, 1)] public float Threshold;
    public float XOffset;
    public float YOffset;
}