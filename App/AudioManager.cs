using System;
using System.Collections.Generic;
using Raylib_cs;
using BilliardsGame.Interfaces;

namespace BilliardsGame.App
{
    /// <summary>
    /// Implements Miniaudio bindings routing specific hardware audio channels through the Raylib subsystem.
    /// Manages file streaming, automated volume cross-fading, and dynamic spatial impulse scaling.
    /// </summary>
    public class AudioManager : IAudioManager
    {
        private readonly Dictionary<string, Music> _bgmTracks = new();
        private readonly Dictionary<string, Sound> _sfxTracks = new();
        
        private readonly Dictionary<string, float> _bgmCurrentVolumes = new();
        private string _targetBgmName = "";
        
        // Limits background tracks prioritizing explicit spatial sound effects (collisions).
        private const float MaxBgmVolume = 0.25f; 
        private const float FadeSpeed = 0.5f;     

        /// <inheritdoc />
        public void Initialize()
        {
            // Forces hardware-level allocation of Miniaudio channels. Must be called immediately after UI window spawns.
            Raylib.InitAudioDevice();
        }

        /// <inheritdoc />
        public void Deinitialize()
        {
            // Aggressively flush unmanaged pointers preventing C-level memory leaks.
            foreach (var music in _bgmTracks.Values)
            {
                Raylib.UnloadMusicStream(music);
            }
            _bgmTracks.Clear();

            foreach (var sound in _sfxTracks.Values)
            {
                Raylib.UnloadSound(sound);
            }
            _sfxTracks.Clear();

            Raylib.CloseAudioDevice();
        }

        /// <inheritdoc />
        public void Update()
        {
            float dt = Raylib.GetFrameTime();
            
            // Loop evaluates active audio streams scaling channel volumes over time to simulate cross-fades.
            foreach (var kvp in _bgmTracks)
            {
                string name = kvp.Key;
                Music music = kvp.Value;
                
                bool isTarget = (name == _targetBgmName);
                float targetVol = isTarget ? MaxBgmVolume : 0f;
                float currentVol = _bgmCurrentVolumes[name];

                if (currentVol < targetVol)
                {
                    currentVol += FadeSpeed * dt;
                    if (currentVol > targetVol) currentVol = targetVol;
                }
                else if (currentVol > targetVol)
                {
                    currentVol -= FadeSpeed * dt;
                    if (currentVol < 0f) currentVol = 0f;
                }

                _bgmCurrentVolumes[name] = currentVol;
                Raylib.SetMusicVolume(music, currentVol);

                // Actively poll and buffer data buffers preventing stream exhaustion during heavy rendering. 
                if (currentVol > 0f)
                {
                    if (!Raylib.IsMusicStreamPlaying(music))
                    {
                        Raylib.ResumeMusicStream(music);
                        if (!Raylib.IsMusicStreamPlaying(music))
                        {
                            Raylib.PlayMusicStream(music);
                        }
                    }
                    Raylib.UpdateMusicStream(music);
                }
                else
                {
                    // Freeze playback strictly preserving buffer indexes.
                    if (Raylib.IsMusicStreamPlaying(music))
                    {
                        Raylib.PauseMusicStream(music);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void LoadBGM(string name, string relativePath)
        {
            if (!_bgmTracks.ContainsKey(name))
            {
                string fullPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, relativePath);
                var music = Raylib.LoadMusicStream(fullPath);
                _bgmTracks[name] = music;
                _bgmCurrentVolumes[name] = 0f;
                Raylib.SetMusicVolume(music, 0f);
            }
        }

        /// <inheritdoc />
        public void LoadSFX(string name, string relativePath)
        {
            if (!_sfxTracks.ContainsKey(name))
            {
                string fullPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, relativePath);
                var sound = Raylib.LoadSound(fullPath);
                _sfxTracks[name] = sound;
            }
        }

        /// <inheritdoc />
        public void PlayBGM(string name, float volume = 0.5f) 
        {
            // Forces manual target overriding internal volume modifiers with the fade logic defined.
            _targetBgmName = name;
        }

        /// <inheritdoc />
        public void StopBGM(string name)
        {
            if (_targetBgmName == name) _targetBgmName = "";
        }

        /// <inheritdoc />
        public void PlaySFX(string name, float volume = 1.0f)
        {
            if (_sfxTracks.TryGetValue(name, out var sound))
            {
                Raylib.SetSoundVolume(sound, volume);
                Raylib.PlaySound(sound);
            }
        }

        /// <inheritdoc />
        public void StopSFX(string name)
        {
            if (_sfxTracks.TryGetValue(name, out var sound))
            {
                if (Raylib.IsSoundPlaying(sound))
                {
                    Raylib.StopSound(sound);
                }
            }
        }

        /// <inheritdoc />
        public bool IsSFXPlaying(string name)
        {
            if (_sfxTracks.TryGetValue(name, out var sound))
            {
                return Raylib.IsSoundPlaying(sound);
            }
            return false;
        }

        /// <inheritdoc />
        public void PlayCushionHit(float impactForce)
        {
            // Scales output bounds proportionately relative directly to engine mass/velocity impulses.
            float volume = Math.Clamp(impactForce / 300f, 0.1f, 1.0f);
            PlaySFX("cushion", volume);
        }

        /// <inheritdoc />
        public void PlayCueHit(float power)
        {
            float volume = Math.Clamp(power + 0.2f, 0.2f, 1.0f);
            PlaySFX("cue_hit", volume);
        }

        /// <inheritdoc />
        public void PlayUIClick()
        {
            PlaySFX("ui_click", 0.20f);
        }

        /// <inheritdoc />
        public void PlayUIHover()
        {
            PlaySFX("ui_click", 0.05f);
        }

        /// <inheritdoc />
        public void PlayGameOver()
        {
            PlaySFX("game_over", 0.3f);
        }

        /// <inheritdoc />
        public void PlayCollision(float impactForce)
        {
            float volume = Math.Clamp(impactForce / 300f, 0.1f, 1.0f);
            PlaySFX("collision", volume);
        }

        /// <inheritdoc />
        public void PlayPocketed()
        {
            PlaySFX("pocket", 1.0f);
        }
    }
}
