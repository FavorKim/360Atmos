using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;


public class VideoAudioStarter : MonoBehaviour
{
    [SerializeField] VideoPlayer VP;
    [SerializeField] AudioSource AS;
    [SerializeField] List<VideoAudioPair> pairs;

    public void PlayVA(string location)
    {
        foreach(var p in pairs)
        {
            if(p.location == location)
            {
                if(VP.isPlaying)
                VP.Stop();
                if(AS.isPlaying)
                AS.Stop();

                VP.clip = p.VP;
                AS.clip = p.AS;
                VP.Play();
                AS.Play();
            }
        }
    }
}

[Serializable]
public struct VideoAudioPair
{
    public string location;
    public VideoClip VP;
    public AudioClip AS;
}
