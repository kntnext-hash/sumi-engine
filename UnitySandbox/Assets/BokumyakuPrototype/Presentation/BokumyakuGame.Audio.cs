using System;
using System.IO;
using Bokumyaku.Simulation;
using UnityEngine;

namespace Bokumyaku.Presentation
{
    public sealed partial class BokumyakuGame
    {
        [Serializable]
        public sealed class AudioPreferences
        {
            public float Master = .8f, Music = .35f, Effects = .65f;
            public bool Muted;
        }
        AudioPreferences audioPreferences = new AudioPreferences();
        string audioSettingsPath;
        AudioListener ownedAudioListener;
        AudioSource musicSource, uiSource, coreSource, notificationSource;
        readonly AudioSource[] combatSources = new AudioSource[4];
        readonly AudioClip[] audioClips = new AudioClip[8];
        readonly System.Collections.Generic.Dictionary<AudioSource, float> audioGains = new System.Collections.Generic.Dictionary<AudioSource, float>();
        static readonly string[] AudioNames = { "sfx_click", "sfx_purchase", "sfx_attack", "sfx_kill", "sfx_core_hit", "sfx_defeat", "sfx_research_complete", "bgm_sumi_no_ma_80s" };
        RunSimulator audioRun;
        int audioKills, audioCoreHp;
        RunStatus audioRunState;
        float nextAttackAudio, nextKillAudio, nextCoreAudio, nextUiAudio;
        public int LoadedAudioCount { get; private set; }
        public int PlayedAudioEvents { get; private set; }
        public bool MusicPlaying => musicSource != null && musicSource.isPlaying;
        public AudioPreferences AudioSettings => new AudioPreferences { Master = audioPreferences.Master, Music = audioPreferences.Music, Effects = audioPreferences.Effects, Muted = audioPreferences.Muted };
        public string AudioSettingsWarning { get; private set; } = "";
        public int ActiveCombatVoices
        {
            get { int count = 0; foreach (var source in combatSources) if (source != null && source.isPlaying) count++; return count; }
        }
        AudioSource NewAudioSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = loop; source.spatialBlend = 0;
            source.dopplerLevel = 0; audioGains[source] = 1; return source;
        }
        void EnsureAudio()
        {
            if (musicSource == null)
            {
                bool hasListener = false;
                foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) if (listener.isActiveAndEnabled) hasListener = true;
                if (!hasListener) ownedAudioListener = gameObject.AddComponent<AudioListener>();
                musicSource = NewAudioSource(true); musicSource.volume = 0;
                uiSource = NewAudioSource(false); coreSource = NewAudioSource(false); notificationSource = NewAudioSource(false);
                for (int i = 0; i < combatSources.Length; i++) combatSources[i] = NewAudioSource(false);
                for (int i = 0; i < audioClips.Length; i++)
                {
                    audioClips[i] = Resources.Load<AudioClip>("BokumyakuAudio/" + AudioNames[i]);
                    if (audioClips[i] != null) LoadedAudioCount++;
                }
                musicSource.clip = audioClips[7];
                if (musicSource.clip != null) musicSource.Play();
            }
            string path = SavePath + ".audio.json";
            if (audioSettingsPath == path) return;
            // Switching to an isolated QA campaign must not retain another save's settings or sounds.
            foreach (var source in combatSources) source.Stop();
            uiSource.Stop(); coreSource.Stop(); notificationSource.Stop();
            audioSettingsPath = path; audioPreferences = new AudioPreferences(); AudioSettingsWarning = "";
            try
            {
                if (File.Exists(path)) audioPreferences = JsonUtility.FromJson<AudioPreferences>(File.ReadAllText(path)) ?? new AudioPreferences();
                ClampAudioSettings();
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { AudioSettingsWarning = "音量設定を読み込めません。初期音量で再生します。"; }
            audioRun = null;
        }
        static float SafeVolume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Clamp01(value);
        void ClampAudioSettings()
        {
            audioPreferences.Master = SafeVolume(audioPreferences.Master);
            audioPreferences.Music = SafeVolume(audioPreferences.Music);
            audioPreferences.Effects = SafeVolume(audioPreferences.Effects);
        }
        public bool SetAudioSettings(float master, float music, float effects, bool muted)
        {
            EnsureAudio();
            audioPreferences.Master = master; audioPreferences.Music = music; audioPreferences.Effects = effects; audioPreferences.Muted = muted;
            ClampAudioSettings(); ApplyAudioVolumes();
            try
            {
                string directory = Path.GetDirectoryName(audioSettingsPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                string temporary = audioSettingsPath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(audioPreferences));
                if (File.Exists(audioSettingsPath)) File.Replace(temporary, audioSettingsPath, null);
                else File.Move(temporary, audioSettingsPath);
                AudioSettingsWarning = ""; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { AudioSettingsWarning = "音量設定を保存できません。今回の再生には反映済みです。"; return false; }
        }
        public void ReloadAudioSettings() { audioSettingsPath = null; EnsureAudio(); ApplyAudioVolumes(); }
        void ApplyAudioVolumes()
        {
            float master = audioPreferences.Muted ? 0 : audioPreferences.Master;
            float effects = master * audioPreferences.Effects;
            uiSource.volume = effects * audioGains[uiSource]; coreSource.volume = effects * audioGains[coreSource];
            notificationSource.volume = effects * audioGains[notificationSource];
            foreach (var source in combatSources) source.volume = effects * audioGains[source];
            musicSource.mute = audioPreferences.Muted;
        }
        void LateUpdate()
        {
            EnsureAudio(); ApplyAudioVolumes();
            float target = audioPreferences.Master * audioPreferences.Music;
            if (AwaitingStart || IsPaused || Run == null || Run.State != RunStatus.Running) target *= .55f;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, Time.unscaledDeltaTime * .5f);
            if (audioRun != Run)
            {
                audioRun = Run;
                if (Run != null) { audioKills = Run.Kills; audioCoreHp = Run.CoreHp; audioRunState = Run.State; }
                return;
            }
            if (Run == null || AwaitingStart) return;
            if (Run.Kills > audioKills) CombatAudio(3, .5f, ref nextKillAudio, .10f);
            if (Run.CoreHp < audioCoreHp && Time.unscaledTime >= nextCoreAudio)
            { PlayAudio(coreSource, 4, .65f); nextCoreAudio = Time.unscaledTime + .25f; }
            if (Run.State == RunStatus.Defeated && audioRunState != RunStatus.Defeated) PlayAudio(notificationSource, 5, .65f);
            audioKills = Run.Kills; audioCoreHp = Run.CoreHp; audioRunState = Run.State;
        }
        void PlayAudio(AudioSource source, int clip, float gain)
        {
            if (source == null || audioClips[clip] == null || audioPreferences.Muted) return;
            source.clip = audioClips[clip]; audioGains[source] = gain;
            source.volume = audioPreferences.Master * audioPreferences.Effects * gain;
            source.Play(); PlayedAudioEvents++;
        }
        void CombatAudio(int clip, float gain, ref float next, float interval)
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + interval;
            foreach (var source in combatSources) if (!source.isPlaying) { PlayAudio(source, clip, gain); return; }
        }
        void AudioAttack()
        {
            EnsureAudio(); CombatAudio(2, .45f, ref nextAttackAudio, .08f);
        }
        void AudioClick()
        {
            EnsureAudio();
            if (Time.unscaledTime < nextUiAudio) return;
            nextUiAudio = Time.unscaledTime + .06f; PlayAudio(uiSource, 0, .65f);
        }
        void AudioPurchase() { EnsureAudio(); PlayAudio(uiSource, 1, .65f); }
        void AudioResearch() { EnsureAudio(); PlayAudio(notificationSource, 6, .65f); }
        static float NextVolume(float value) => value >= .99f ? 0 : Mathf.Min(1, (Mathf.Floor(value * 4) + 1) / 4);
        void ReleaseAudio()
        {
            foreach (var pair in audioGains)
                if (pair.Key != null) { pair.Key.Stop(); Destroy(pair.Key); }
            audioGains.Clear();
            if (ownedAudioListener != null) Destroy(ownedAudioListener);
        }
    }
}
