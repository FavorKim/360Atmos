using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer),typeof(AudioSource),typeof(Animator))]
public class PlaySphere : MonoBehaviour
{
    VideoPlayer videoPlayer;
    AudioSource audioSource;
    HandGrabInteractable grab;
    Animator anim;
    Vector3 originPos;

    [SerializeField] VideoAudioStarter vas;
    [SerializeField] UnityEvent OnGrab;
    [SerializeField] UnityEvent OnReleased;

    public VideoClip VideoClip 
    {
        get 
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
            return videoPlayer.clip; 
        }
    }
    public AudioClip AudioClip 
    {
        get 
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            return audioSource.clip; 
        }
    }


    [SerializeField] bool alwaysPlay;
    private void Awake()
    {
        originPos = transform.position;
    }
    private void Start()
    {
        grab = GetComponentInChildren<HandGrabInteractable>();
        if (grab != null)
            grab.WhenStateChanged += Grab_WhenStateChanged;
        else
            Debug.LogError($"PlaySphere {gameObject.name}의 자식 객체에 HandGrabInteractable 컴포넌트가 없습니다.");

        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Grab_WhenStateChanged(InteractableStateChangeArgs obj)
    {
        // OnGrab
        if(obj.NewState == InteractableState.Select)
        {
            GuideManager.Instance.ProgressGuide(1);
            OnGrab?.Invoke();
            anim.enabled = false;
            audioSource.volume = 1;
        }
        // OnReleased
        else if(obj.NewState == InteractableState.Normal)
        {
            audioSource.volume = 0;
            anim.enabled = true;
            if (vas != null)
            {
                transform.position = vas.transform.position;
                GuideManager.Instance.ProgressGuide(2);
            }
            OnReleased?.Invoke();
        }
    }



    public void ResetPos() { transform.position = originPos; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("VideoAudioStarter"))
        {
            vas = other.GetComponent<VideoAudioStarter>();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("VideoAudioStarter"))
        {
            vas = null;
        }
    }

}
