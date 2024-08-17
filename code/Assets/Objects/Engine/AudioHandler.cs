using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

namespace Objects.Engine
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioHandler: MonoBehaviour, IRelayStart
    {
        [SerializeField] private List<AudioClip> audioClips = new List<AudioClip>();
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool playOnStart = true;

        [SerializeField]


        public void RunRelayStart()
        {
            audioSource = GetComponent<AudioSource>();
            if (playOnStart)
            {
                PlayTrack();
            }
        }

        private void Update()
        {
            if (!audioSource.isPlaying)
            {
                PlayTrack();
            }
        }
int currentTrack = 0;
        private void PlayTrack()
        {
            if(currentTrack >= audioClips.Count)
            {
                currentTrack = 0;
            }
            audioSource.clip = audioClips[currentTrack];
            audioSource.Play();
            currentTrack++;
            
        }
    }
}