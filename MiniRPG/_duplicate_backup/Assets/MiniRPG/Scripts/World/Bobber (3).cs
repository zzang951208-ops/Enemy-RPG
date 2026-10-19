using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRPG
{
    /// <summary>둥실둥실 + 회전</summary>
    public class Bobber : MonoBehaviour
    {
        public float amp = 0.15f, speed = 2f, spin = 60f;
        Vector3 start; float seed;
        void Start() { start = transform.localPosition; seed = Random.value * 10f; }
        void Update()
        {
            transform.localPosition = start + Vector3.up * Mathf.Sin(Time.time * speed + seed) * amp;
            transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
        }
    }
}
