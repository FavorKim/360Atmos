using Oculus.Interaction;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;


public class VideoAudioStarter : MonoBehaviour
{
    [SerializeField] VideoPlayer VP;
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
            VP.clip = sphere.VideoClip;
            AS.clip = sphere.AudioClip;

            VP.Play();
            AS.Play();
        }
    }

    public void StopVA()
    {
        VP.Stop();
        AS.Stop();

        VP.clip = null;
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
