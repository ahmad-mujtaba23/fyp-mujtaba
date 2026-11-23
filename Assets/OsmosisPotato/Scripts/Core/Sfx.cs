using System;
using System.Collections.Generic;
using UnityEngine;

namespace Osmosis
{
    /// Port of the reference SfxHandler: one persistent sound bank, name -> AudioSource.
    /// Lives on the SFX prefab in the Menu scene; Sfx.Play is a safe no-op if it's missing
    /// (e.g. when a Lab scene is opened directly in the Editor).
    public class Sfx : MonoBehaviour
    {
        [Serializable]
        public class Sound
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.1f, 3f)] public float pitch = 1f;
            public bool loop;
        }

        // Names used by the reference practical, kept identical so the SFX prefab maps 1:1.
        public const string Click = "click_s", Inventory = "inventory_s", Snap = "snap_s",
            Correct = "correct_s", Wrong = "wrong_s", Typing = "typing_s",
            Notification = "notification_s", ClockTicking = "clockTicking_s",
            Pour = "tap_s", Music = "bg_s";

        public Sound[] sounds;

        static Sfx instance;
        readonly Dictionary<string, AudioSource> sources = new Dictionary<string, AudioSource>();

        void Awake()
        {
            if (instance != null) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var s in sounds)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.clip = s.clip;
                src.volume = s.volume;
                src.pitch = s.pitch;
                src.loop = s.loop;
                sources[s.name] = src;
            }
            ApplyMutes();
        }

        // Settings (task 2.2): persisted in PlayerPrefs, applied as AudioSource.mute so loops keep their place.
        // Prefixed: PlayerPrefs is shared by every practical in the combined app.
        const string MusicKey = "osmosis.music_on", SfxKey = "osmosis.sfx_on";

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); instance?.ApplyMutes(); }
        }

        public static bool EffectsOn
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
            set { PlayerPrefs.SetInt(SfxKey, value ? 1 : 0); PlayerPrefs.Save(); instance?.ApplyMutes(); }
        }

        void ApplyMutes()
        {
            bool music = MusicOn, fx = EffectsOn;
            foreach (var kv in sources) kv.Value.mute = kv.Key == Music ? !music : !fx;
        }

        public static bool IsMuted(string name) =>
            instance != null && instance.sources.TryGetValue(name, out var src) && src.mute;

        public static void Play(string name)
        {
            if (instance != null && instance.sources.TryGetValue(name, out var src)) src.Play();
        }

        public static void Stop(string name)
        {
            if (instance != null && instance.sources.TryGetValue(name, out var src)) src.Stop();
        }

        public static bool IsPlaying(string name) =>
            instance != null && instance.sources.TryGetValue(name, out var src) && src.isPlaying;
    }
}
