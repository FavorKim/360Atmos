using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

namespace SoundLibrary
{
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
                    if (instance == null)
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
        List<PlaySphere> spheres = new();
        [SerializeField] string state;
        [SerializeField] GameObject resetBtn;

        Material activatedMat;
        Texture skyTexture;

        [SerializeField] VideoPlayer logo;
        [SerializeField] VideoPlayer VP;
        [SerializeField] float fifoTime;
        [SerializeField] GameObject uis;

        [SerializeField] List<GameObject> SFXList;

        List<HandGrabPathRecorderAdvanced> records = new();

        public UnityEvent OnPlaySceneLoaded;
        public UnityEvent OnPlaySceneStartLoading;
        public UnityEvent OnLobbySceneLoaded;
        public UnityEvent OnLobbySceneStartLoading;

        private void Start()
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            VAS = FindAnyObjectByType<VideoAudioStarter>();

            state = "LOBBY";
            logo.prepareCompleted += OnPrepared;
            logo.gameObject.SetActive(false);

            EnableSFXs(false);

            activatedMat = skyBoxMat;
            skyBoxMat.SetFloat("_Exposure", 0);
            resetBtn.SetActive(true);

            skyTexture = skyBoxMat.mainTexture;
        }
        void EnableSFXs(bool enable)
        {
            foreach (var s in SFXList)
                s.SetActive(enable);
        }

        void OnPrepared(VideoPlayer vp)
        {
            VP.gameObject.SetActive(true);

            var mat = VP.targetMaterialRenderer.material; // or sharedMaterial
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

            //activatedMat = state == "LOBBY" ? skyBoxMat : VP.GetComponent<MeshRenderer>().material;
            Texture t = state == "LOBBY" ? skyBoxMat.mainTexture : VP.targetTexture;
            skyBoxMat.SetTexture("_MainTex", t);
            StartCoroutine(CorFI());


            switch (state)
            {
                case "LOBBY":
                    OnLobbySceneLoaded.Invoke();
                    //VP.gameObject.SetActive(false);
                    break;
                case "PLAY":
                    OnPlaySceneLoaded.Invoke();
                    //VP.gameObject.SetActive(true);
                    break;
                default:
                    break;
            }
        }

        public void FILobby()
        {
            activatedMat = skyBoxMat;
            StartCoroutine(CorFI());
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
            if (spheres != null)
                foreach (var s in spheres)
                    s.ResetPos();

            if (records != null)
                foreach (var r in records)
                    r.ClearPath();
        }

        public void AddPlaySphere(PlaySphere sphere)
        {
            spheres.Add(sphere);
        }

        public void AddRecord(HandGrabPathRecorderAdvanced record)
        {
            records.Add(record);
        }

        private void OnApplicationQuit()
        {
            skyBoxMat.mainTexture = skyTexture;
        }
    }
}