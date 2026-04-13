using System;
using UnityEngine;

[Serializable]
public struct NoiseRule
{
    [Range(0.1f, 100)] public float NoiseSize;
    [Range(0, 1)] public float PositiveDelta;
    [Range(0, 1)] public float NegativeDelta;
    public float XOffset;
    public float YOffset;
}
