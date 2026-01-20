using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer), typeof(AudioSource), typeof(Animator))]
public class PlaySphere : MonoBehaviour
{
    public string serverUrl = "http://192.168.0.31/";
    public string fileName;
    public string FileName => fileName;
    public string LocalDirectory =>
    Path.Combine(Application.persistentDataPath, "Video");

    public string LocalVideoPath =>
        Path.Combine(LocalDirectory ,FileName);

    static bool isDownloaded = true;

    VideoPlayer videoPlayer;
    AudioSource audioSource;
    HandGrabInteractable grab;
    Animator anim;
    Vector3 originPos;

    [SerializeField] VideoAudioStarter vas;
    [SerializeField] UnityEvent OnGrab;
    [SerializeField] UnityEvent OnReleased;
    public string audioName;


    bool isSetOrigin;

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
    public void SetOriginPos()
    {
        if (!isSetOrigin)
        {
            originPos = transform.position;
            isSetOrigin = true;
        }
    }
    private void Awake()
    {
        grab = GetComponentInChildren<HandGrabInteractable>();
        if (grab != null)
            grab.WhenStateChanged += Grab_WhenStateChanged;
        else
            Debug.LogError($"PlaySphere {gameObject.name}의 자식 객체에 HandGrabInteractable 컴포넌트가 없습니다.");

        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        videoPlayer = GetComponent<VideoPlayer>();




        MySceneManager.Instance.AddPlaySphere(this);
    }
    private void OnEnable()
    {
        StartCoroutine(PrepareVideo());

    }

    private void Grab_WhenStateChanged(InteractableStateChangeArgs obj)
    {
        // OnGrab
        if (obj.NewState == InteractableState.Select)
        {
            GuideManager.Instance.ProgressGuide(1);
            OnGrab?.Invoke();
            anim.enabled = false;
            audioSource.volume = 1;
        }
        // OnReleased
        else if (obj.NewState == InteractableState.Normal)
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
    IEnumerator PrepareVideo()
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogError($"[{name}] fileName이 비어있습니다. 인스펙터에서 fileName에 'xxx.mp4'를 지정해주세요.");
            yield break;
        }

        // 이미 다운로드된 경우 바로 재생
        if (File.Exists(LocalVideoPath + ".mp4"))
        {
            Debug.Log($"{fileName}이미 다운로드됨 → 로컬에서 재생");
            PlayLocalVideo();
            yield break;
        }

        // 폴더 생성
        Directory.CreateDirectory(LocalDirectory);

        string fullUrl = serverUrl + "/Video/" + fileName + ".mp4";
        Debug.Log("다운로드 시작: " + fullUrl);

        /*
        using (UnityWebRequest req = new UnityWebRequest(fullUrl, UnityWebRequest.kHttpVerbGET))
        {
            req.downloadHandler = new DownloadHandlerFile(LocalVideoPath + ".mp4");

            // 4-1. 요청 시작
            var op = req.SendWebRequest();

            // 4-2. 진행률 로그
            while (!req.isDone)
            {
                Debug.Log($"[{name}] 다운로드 중: {req.downloadProgress * 100f}%");
                // 여기까지 로그가 나오면, 코루틴이 살아있는 것
                yield return null;
            }

            // 4-3. 요청 완료까지 한 번 더 기다리기 (안전빵)
            yield return op;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[{name}] 다운로드 실패: {req.error}");
                yield break;
            }
        }
        */

        UnityWebRequest req = new UnityWebRequest(fullUrl, UnityWebRequest.kHttpVerbGET);
        req.downloadHandler = new DownloadHandlerFile(LocalVideoPath + ".mp4");

        var operation = req.SendWebRequest();

        while (!operation.isDone)
        {
            //Debug.Log($"{FileName} 다운로드 중: {req.downloadProgress * 100f}%");
            yield return null;
        }


        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("다운로드 실패: " + req.error);
            yield break;
        }
        Debug.Log($"{FileName} 다운로드 완료 → 재생: " + LocalVideoPath);
        PlayLocalVideo();
    }

    public void PlayLocalVideo()
    {
        string fileUrl = "file://" + LocalVideoPath + ".mp4";

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = fileUrl;
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnPrepared;
    }

    private void OnPrepared(VideoPlayer vp)
    {
        Debug.Log("비디오 준비 완료 → 재생 시작");
        vp.prepareCompleted -= OnPrepared;
        vp.Play();
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
        if (other.CompareTag("VideoAudioStarter"))
        {
            vas = null;
        }
    }

}
