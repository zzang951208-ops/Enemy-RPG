using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    public enum Sfx { Hit, Crit, Swing, Coin, LevelUp, Tick, Win, BigWin, Lose, Hurt, Click, Pickup, Skill, Quest, Dash, Slam, Card, Dice }

    /// <summary>오디오 파일 없이 코드로 합성한 효과음</summary>
    public static class SoundFX
    {
        static AudioSource src;
        static readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        static System.Random rng = new System.Random(1234);
        const int SR = 44100;
        public static float MasterVolume = 0.7f;

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            clips.Clear();

            clips[Sfx.Hit] = Make("hit", 0.14f, t => Noise() * Env(t, 0.14f, 0.002f) * 0.6f + Sq(140 - 400 * t, t) * Env(t, 0.1f) * 0.35f);
            clips[Sfx.Crit] = Make("crit", 0.25f, t => Noise() * Env(t, 0.1f) * 0.5f + Sin(900 - 1500 * t, t) * Env(t, 0.25f) * 0.4f);
            clips[Sfx.Swing] = Make("swing", 0.16f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.16f) * 0.25f);
            clips[Sfx.Coin] = Make("coin", 0.22f, t => Sin(t < 0.07f ? 1320 : 1760, t) * Env(t, 0.22f) * 0.35f);
            clips[Sfx.LevelUp] = Arp("lvl", new float[] { 523, 659, 784, 1047, 1319 }, 0.09f, 0.35f);
            clips[Sfx.Tick] = Make("tick", 0.03f, t => Sq(1800, t) * Env(t, 0.03f) * 0.18f);
            clips[Sfx.Win] = Arp("win", new float[] { 659, 784, 988 }, 0.08f, 0.3f);
            clips[Sfx.BigWin] = Arp("bigwin", new float[] { 523, 659, 784, 1047, 784, 1047, 1319, 1568 }, 0.08f, 0.35f);
            clips[Sfx.Lose] = Arp("lose", new float[] { 392, 330, 262 }, 0.13f, 0.3f);
            clips[Sfx.Hurt] = Make("hurt", 0.2f, t => Sq(220 - 300 * t, t) * Env(t, 0.2f) * 0.3f + Noise() * Env(t, 0.08f) * 0.2f);
            clips[Sfx.Click] = Make("click", 0.04f, t => Sin(1200, t) * Env(t, 0.04f) * 0.25f);
            clips[Sfx.Pickup] = Arp("pick", new float[] { 880, 1175 }, 0.06f, 0.3f);
            clips[Sfx.Skill] = Make("skill", 0.4f, t => (Noise() * 0.3f + Sin(300 + 600 * t, t) * 0.3f) * Mathf.Sin(Mathf.PI * t / 0.4f));
            clips[Sfx.Quest] = Arp("quest", new float[] { 784, 988, 1175, 1568 }, 0.1f, 0.3f);
            clips[Sfx.Dash] = Make("dash", 0.18f, t => Noise() * Env(t, 0.18f) * 0.25f);
            clips[Sfx.Slam] = Make("slam", 0.6f, t => (Noise() * 0.6f + Sin(55, t) * 0.8f) * Env(t, 0.6f, 0.005f) * 0.7f);
            clips[Sfx.Card] = Make("card", 0.07f, t => Noise() * Env(t, 0.07f) * 0.35f);
            clips[Sfx.Dice] = Make("dice", 0.05f, t => (Noise() * 0.5f + Sq(600, t) * 0.2f) * Env(t, 0.05f));
        }

        public static void Play(Sfx s, float vol = 1f)
        {
            if (src == null) return;
            AudioClip c;
            if (clips.TryGetValue(s, out c)) src.PlayOneShot(c, vol * MasterVolume);
        }

        // ── 합성 유틸 ──
        delegate float Wave(float t);

        static AudioClip Make(string name, float dur, Wave f)
        {
            int n = Mathf.Max(1, (int)(SR * dur));
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)SR), -1f, 1f);
            var c = AudioClip.Create(name, n, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip Arp(string name, float[] notes, float step, float vol)
        {
            float dur = notes.Length * step + 0.25f;
            return Make(name, dur, t =>
            {
                int idx = Mathf.Min((int)(t / step), notes.Length - 1);
                float local = t - idx * step;
                float tail = idx == notes.Length - 1 ? 0.3f : step;
                return (Sin(notes[idx], t) * 0.7f + Sq(notes[idx], t) * 0.15f) * Env(local, tail + 0.05f) * vol;
            });
        }

        static float Sin(float hz, float t) { return Mathf.Sin(2f * Mathf.PI * hz * t); }
        static float Sq(float hz, float t) { return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * Mathf.Max(20f, hz) * t)); }
        static float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
        static float Env(float t, float dur, float attack = 0.004f)
        {
            if (t < attack) return t / attack;
            return Mathf.Clamp01(1f - (t - attack) / Mathf.Max(0.001f, dur - attack));
        }
    }
}
