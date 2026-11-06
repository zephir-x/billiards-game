namespace BilliardsGame.Interfaces
{
    /// <summary>
    /// Manages the registration, streaming, and playback of background music and sound effects.
    /// </summary>
    public interface IAudioManager
    {
        /// <summary>
        /// Initializes the audio device and context. Must be called before loading assets.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Closes the audio device and unloads all cached audio resources.
        /// </summary>
        void Deinitialize();

        /// <summary>
        /// Updates audio streams. Should be called continuously within the main game loop.
        /// </summary>
        void Update();

        /// <summary>
        /// Loads a background music stream into memory.
        /// </summary>
        /// <param name="name">The unique alias to refer to this music track.</param>
        /// <param name="relativePath">Path to the audio file.</param>
        void LoadBGM(string name, string relativePath);

        /// <summary>
        /// Loads a sound effect into memory.
        /// </summary>
        /// <param name="name">The unique alias to refer to this sound effect.</param>
        /// <param name="relativePath">Path to the audio file.</param>
        void LoadSFX(string name, string relativePath);

        /// <summary>
        /// Starts playing the specified background music stream.
        /// </summary>
        /// <param name="name">The alias of the loaded music track.</param>
        /// <param name="volume">Volume multiplier in range [0.0, 1.0].</param>
        void PlayBGM(string name, float volume = 0.5f);

        /// <summary>
        /// Stops the currently playing background music stream.
        /// </summary>
        /// <param name="name">The alias of the loaded music track.</param>
        void StopBGM(string name);

        /// <summary>
        /// Plays a sound effect once.
        /// </summary>
        /// <param name="name">The alias of the loaded sound effect.</param>
        /// <param name="volume">Volume multiplier in range [0.0, 1.0].</param>
        void PlaySFX(string name, float volume = 1.0f);

        /// <summary>
        /// Halts playback of the specified sound effect if it is currently playing.
        /// </summary>
        /// <param name="name">The alias of the loaded sound effect.</param>
        void StopSFX(string name);

        /// <summary>
        /// Checks whether the specified sound effect is actively playing.
        /// </summary>
        /// <param name="name">The alias of the loaded sound effect.</param>
        /// <returns>True if the sound is currently playing; otherwise false.</returns>
        bool IsSFXPlaying(string name);

        /// <summary>
        /// Plays a dynamic collision sound scaled by the impact force.
        /// </summary>
        /// <param name="impactForce">The magnitude of the physical impact.</param>
        void PlayCollision(float impactForce);

        /// <summary>
        /// Plays a dynamic cushion bounce sound scaled by the impact force.
        /// </summary>
        /// <param name="impactForce">The magnitude of the physical impact.</param>
        void PlayCushionHit(float impactForce);

        /// <summary>
        /// Plays the cue stick striking the cue ball sound, scaled by chosen power.
        /// </summary>
        /// <param name="power">The charged power magnitude.</param>
        void PlayCueHit(float power);

        /// <summary>
        /// Plays the UI click interaction sound.
        /// </summary>
        void PlayUIClick();

        /// <summary>
        /// Plays the UI hover interaction sound.
        /// </summary>
        void PlayUIHover();

        /// <summary>
        /// Plays the dramatic game over musical sting.
        /// </summary>
        void PlayGameOver();

        /// <summary>
        /// Plays the billiard ball pocketed (sunk) sound effect.
        /// </summary>
        void PlayPocketed();
    }
}
