using System.Collections;
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

    VideoAudioStarter VAS;
    PlaySphere[] spheres;
    [SerializeField] string state;

    [SerializeField] VideoPlayer logo;
    [SerializeField] VideoPlayer VP;
    [SerializeField] float fifoTime;
    [SerializeField] GameObject uis;

    public UnityEvent OnPlaySceneLoaded;
    public UnityEvent OnLobbySceneLoaded;

    private void Start()
    {
        instance = this;
        DontDestroyOnLoad(this.gameObject);
        VAS = FindAnyObjectByType<VideoAudioStarter>();
        spheres = FindObjectsByType<PlaySphere>(FindObjectsSortMode.None);
        state = "LOBBY";
        logo.gameObject.SetActive(false);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
            SetSceneState("PLAY");
    }

    IEnumerator CorFI()
    {
        uis.gameObject.SetActive(true);
        logo.gameObject.SetActive(false);
        var mat = VP.GetComponent<MeshRenderer>().material;
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
        var mat = VP.GetComponent<MeshRenderer>().material;
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
        StartCoroutine(CorFO());

        yield return new WaitWhile(() => !logo.isPlaying);
        yield return new WaitWhile(() => logo.isPlaying);

        StartCoroutine(CorFI());
        this.state = state;
        switch (state)
        {
            case "LOBBY":
                OnLobbySceneLoaded.Invoke();
                break;
            case "PLAY":
                OnPlaySceneLoaded.Invoke();
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
    
}
