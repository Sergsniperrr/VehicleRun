using System;
using _Project.Core.Audio;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace _Project.Core.Audio
{
    [CreateAssetMenu(fileName = "AudioAsset", menuName = "_Project/Audio/Audio Asset", order = 0)]
    public class AudioAsset : ScriptableObject
    {
        [SerializeField] private AudioCategories _category;
        [SerializeField] private AudioMixerGroup _outputMixerGroup;
        [SerializeField] private AudioClip[] _audioClips;
        [SerializeField] [Range(0f, 1f)] private float _volume = 1f;
        [SerializeField] [Range(0.1f, 3f)] private float _pitch = 1f;

        public AudioCategories Category => _category;
        public AudioMixerGroup OutputMixerGroup => _outputMixerGroup;
        public float Volume => _volume;
        public float Pitch => _pitch;

        public AudioClip GetRandomClip()
        {
            if (_audioClips == null || _audioClips.Length == 0)
                throw new InvalidOperationException();

            int index = Random.Range(0, _audioClips.Length);

            AudioClip clip = _audioClips[index] ?? throw new ArgumentNullException();

            return clip;
        }
    }
}
