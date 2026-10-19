using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRPG
{
    /// <summary>불빛 깜빡임</summary>
    public class Flicker : MonoBehaviour
    {
        Light l; float baseI; float seed;
        void Awake() { l = GetComponent<Light>(); baseI = l != null ? l.intensity : 1f; seed = Random.value * 100f; }
        void Update() { if (l != null) l.intensity = baseI * (0.8f + 0.35f * Mathf.PerlinNoise(seed, Time.time * 3f)); }
    }
}
