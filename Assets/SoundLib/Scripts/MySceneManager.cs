using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;


public class MySceneManager : MonoBehaviour
{
    private static MySceneManager instance;
    public static MySceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<MySceneManager>();
                if(instance == null)
                {
                    GameObject gobj = new GameObject("MySceneManager");
                    instance = gobj.AddComponent<MySceneManager>();
                    DontDestroyOnLoad(instance);
                }
            }
            return instance;
        }
    }


    [SerializeField] Material skyBoxMat;
    VideoAudioStarter VAS;
    PlaySphere[] spheres;
    [SerializeField] string state;
    [SerializeField] GameObject resetBtn;

    Material activatedMat;

    [SerializeField] VideoPlayer logo;
    [SerializeField] VideoPlayer VP;
    [SerializeField] float fifoTime;
    [SerializeField] GameObject uis;

    [SerializeField] List<GameObject> SFXList;

    HandGrabPathRecorderAdvanced[] records;

    public UnityEvent OnPlaySceneLoaded;
    public UnityEvent OnPlaySceneStartLoading;
    public UnityEvent OnLobbySceneLoaded;
    public UnityEvent OnLobbySceneStartLoading;

    private void Start()
    {
        instance = this;
        DontDestroyOnLoad(this.gameObject);
        VAS = FindAnyObjectByType<VideoAudioStarter>();
        spheres = FindObjectsByType<PlaySphere>(FindObjectsSortMode.None);
        records = FindObjectsByType<HandGrabPathRecorderAdvanced>(FindObjectsSortMode.None);
        state = "LOBBY";
        logo.prepareCompleted += OnPrepared;
        logo.gameObject.SetActive(false);
        records[0].transform.parent.gameObject.SetActive(false);
        activatedMat = skyBoxMat;
        skyBoxMat.SetFloat("_Exposure", 1);
        resetBtn.SetActive(true);
        foreach (var g in SFXList)
            g.SetActive(false);
    }


    void OnPrepared(VideoPlayer vp)
    {
        vp.gameObject.SetActive(true);
        var mat = vp.targetMaterialRenderer.material; // or sharedMaterial
        mat.mainTextureOffset = new Vector2(0f, 0.001f); // 아래 0.1% 잘라내기
        mat.mainTextureScale = new Vector2(1f, 0.998f); // 전체를 99.8%만 표시
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
            SetSceneState("PLAY");
        if (Input.GetKeyDown(KeyCode.Z))
            SetSceneState("LOBBY");
    }

    IEnumerator CorFI()
    {
        uis.gameObject.SetActive(true);
        logo.gameObject.SetActive(false);
        var mat = activatedMat;
        float t = 0;
        while (t < fifoTime)
        {
            mat.SetFloat("_Exposure", t / fifoTime);
            t += Time.deltaTime;
            yield return null;
        }
        mat.SetFloat("_Exposure", 1);
    }

    IEnumerator CorFO()
    {
        uis.gameObject.SetActive(false);
        var mat = activatedMat;
        float t = 0;
        while (t < fifoTime)
        {
            mat.SetFloat("_Exposure", 1 - t / fifoTime);
            t += Time.deltaTime;
            yield return null;
        }
        mat.SetFloat("_Exposure", 0);
        logo.gameObject.SetActive(true);
    }

    IEnumerator CorFOFI(string state)
    {
        switch (state)
        {
            case "LOBBY":
                OnLobbySceneStartLoading.Invoke();
                break;
            case "PLAY":
                OnPlaySceneStartLoading.Invoke();
                break;
            default:
                break;
        }
        StartCoroutine(CorFO());

        yield return new WaitWhile(() => !logo.isPlaying);


        yield return new WaitWhile(() => logo.isPlaying);

        this.state = state;

        activatedMat = state == "LOBBY" ? skyBoxMat : VP.GetComponent<MeshRenderer>().material;

        StartCoroutine(CorFI());

        
        switch (state)
        {
            case "LOBBY":
                OnLobbySceneLoaded.Invoke();
                VP.gameObject.SetActive(false);
                break;
            case "PLAY":
                OnPlaySceneLoaded.Invoke();
                VP.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }

    public void SetSceneState(string state)
    {
        StopAllCoroutines();
        StartCoroutine(CorFOFI(state));
    }

    public void ToggleSFX(bool isActive)
    {
        SFXList[LayerManager.CurIndex].SetActive(isActive);
    }


    public void OnPokePowerOff()
    {
        switch (state)
        {
            case "LOBBY":
#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
                break;
            case "PLAY":
                Instance.SetSceneState("LOBBY");
                break;

            default:
                break;
        }
    }

    public void ResetAll()
    {
        foreach (var s in spheres)
            s.ResetPos();

        foreach (var r in records)
            r.ClearPath();
    }

}
