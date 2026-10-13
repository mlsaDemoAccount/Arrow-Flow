using UnityEngine;

namespace ArrowFlow
{
    // Sources and clips are authored in the scene; playback never creates assets.
    public sealed class GameAudio : MonoBehaviour
    {
        public AudioSource musicSource, effectsSource;
        public AudioClip correctMove, wrongMove;
        [Tooltip("Short jingle played as the level-complete dialog opens, in time with its stars.")]
        public AudioClip levelComplete;
        [Tooltip("Short, gentle \"you lose\" phrase played when the last heart is lost.")]
        public AudioClip levelFailed;
        bool soundEnabled, paused, unfocused, musicAllowed;
        public bool SoundEnabled => soundEnabled;

        public void SetSound(bool enabled)
        {
            soundEnabled=enabled;
            if(!enabled && effectsSource!=null) effectsSource.Stop();
            RefreshPlayback();
        }
        void Update() { RefreshPlayback(); }
        void OnApplicationPause(bool value) { paused=value; RefreshPlayback(); }
        void OnApplicationFocus(bool focused) { unfocused=!focused; RefreshPlayback(); }
        void RefreshPlayback()
        {
            bool allowed=soundEnabled && !paused && !unfocused && !Ads.Busy && isActiveAndEnabled;
            if(allowed==musicAllowed || musicSource==null) return;
            musicAllowed=allowed;
            if(allowed)
            {
                musicSource.UnPause();
                if(!musicSource.isPlaying) musicSource.Play();
            }
            else { musicSource.Pause(); if(effectsSource!=null) effectsSource.Stop(); }
        }
        public void PlayMove(bool correct)
        {
            if(!soundEnabled || paused || unfocused || Ads.Busy || effectsSource==null) return;
            // Avoid overlapping error buzzes when a blocked arrow is tapped rapidly.
            effectsSource.Stop();
            effectsSource.PlayOneShot(correct?correctMove:wrongMove);
        }
        public void PlayLevelComplete() { PlayJingle(levelComplete); }
        public void PlayLevelFailed() { PlayJingle(levelFailed); }
        void PlayJingle(AudioClip clip)
        {
            if(!soundEnabled || paused || unfocused || Ads.Busy || effectsSource==null || clip==null) return;
            effectsSource.Stop();
            effectsSource.PlayOneShot(clip);
        }
        void OnDisable()
        {
            musicAllowed=false;
            if(musicSource!=null)musicSource.Stop();
            if(effectsSource!=null)effectsSource.Stop();
        }
    }
}
