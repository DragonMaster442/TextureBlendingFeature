using System;
using UnityEngine;

[Serializable]
public struct NoiseRule
{
    [Range(0.1f, 10)]
    public float NoiseSize;
    [Range(0, 0.1f)]
    public float PositiveDelta;
    [Range(0, 0.1f)]
    public float NegativeDelta;
    public float XOffset;
    public float YOffset;
    public NoiseRule(float noiseSize = 1, float positiveDelta = 0.1f, float negativeDelta = 0.1f, float xOffset = 0, float yOffset = 0)
    {
        NoiseSize = noiseSize;
        PositiveDelta = positiveDelta;
        NegativeDelta = negativeDelta;
        XOffset = xOffset;
        YOffset = yOffset;
    }
}
