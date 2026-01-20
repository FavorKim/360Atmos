using Oculus.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;


public class VideoAudioStarter : MonoBehaviour
{
    [SerializeField] VideoPlayer VP;
    //[SerializeField] MultiChanelSpeakerSetter MS;
    [SerializeField] AudioSource AS;
    [SerializeField] PlaySphere sphere;
    [SerializeField] PokeInteractable playBtn;

    PlaySphere[] spheres;
    HandGrabPathRecorder[] recoders;

    private void Start()
    {
        spheres = FindObjectsByType<PlaySphere>(FindObjectsSortMode.InstanceID);
        recoders = FindObjectsByType<HandGrabPathRecorder>(FindObjectsSortMode.InstanceID);
    }

    public void SetSpheresOrigin()
    {
        if(spheres== null)
            spheres = FindObjectsByType<PlaySphere>(FindObjectsSortMode.InstanceID);


        foreach (var s in spheres)
            s.SetOriginPos();
    }

    private void Update()
    {
        playBtn.gameObject.SetActive(sphere != null);
    }
    public void ResetAllSpheresPos()
    {
        foreach (var s in spheres)
        {
            s.ResetPos();
        }
    }
    public void ResetAllRecoder()
    {
        foreach (var r in recoders)
        {
            r.ClearPath();
        }
    }

    public void PlayVA()
    {
        if (sphere != null)
        {
            string path = Path.Combine(Application.persistentDataPath, "Video", sphere.fileName+".mp4");
            VP.source = VideoSource.Url;
            VP.url = path;

            

            VP.Play();
            AS.clip = sphere.AudioClip;
            AS.Play();
            //MS.SetSpeakers(sphere.audioName);
        }
    }

    public void StopVA()
    {
        VP.Stop();
        AS.Stop();
        //MS.ResetSpeakers();

        VP.url = null;
        AS.clip = null;

        sphere = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlaySphere ps))
        {
            if (sphere != null)
            {
                sphere.ResetPos();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out PlaySphere ps))
        {
            sphere = ps;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        sphere = null;
    }

}
