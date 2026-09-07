using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;
using TMPro;
using System.Text;
using UnityEngine.Events;
using JetBrains.Annotations;
namespace SoundLibrary
{
    public class DownloadManager : MonoBehaviour
    {
        [SerializeField] List<PlaySphere> spheres;

        [SerializeField] Slider curProgressBar;
        [SerializeField] Slider entireProgressBar;
        [SerializeField] TMP_Text Txt_entirePercent;
        [SerializeField] TMP_Text Txt_currentPercent;
        [SerializeField] TMP_Text Txt_currentName;
        StringBuilder sb;

        [SerializeField] UnityEvent OnEndDownload;

        PlaySphere downloadingSphere;

        float curProgress;
        float CurProgress
        {
            get
            {
                return curProgress;
            }
            set
            {
                curProgress = value;

                curProgressBar.value = curProgress;

                sb.Clear();
                sb.Append(value.ToString("F0"));
                Txt_currentPercent.text = sb.ToString() + '%';

            }
        }
        float entireProgress;

        float EntireProgress
        {
            get { return entireProgress; }
            set
            {
                entireProgress = value;
                entireProgressBar.value = entireProgress;

                sb.Clear();
                sb.Append(value.ToString("F0"));
                Txt_entirePercent.text = sb.ToString() + '%';
            }
        }

        private void Awake()
        {
            sb = new StringBuilder();
        }

        private void Update()
        {

            if (Input.GetKeyDown(KeyCode.U))
                StartDownload();
        }

        public void StartDownload()
        {
            StartCoroutine(PrepareVideo());
        }

        IEnumerator PrepareVideo()
        {
            foreach (var s in spheres)
            {

                CurProgress = 0;
                if (string.IsNullOrWhiteSpace(s.FileName))
                {
                    Debug.LogError($"[{name}] fileName이 비어있습니다. 인스펙터에서 fileName에 'xxx.mp4'를 지정해주세요.");
                    continue;
                }

                // 이미 다운로드된 경우 바로 재생
                if (File.Exists(s.LocalVideoPath + ".mp4"))
                {
                    Debug.Log($"{s.fileName}이미 다운로드됨 로컬에서 재생");
                    EntireProgress = entireProgress + (100f / spheres.Count);
                    s.PlayLocalVideo();
                    continue;
                }

                // 폴더 생성
                Directory.CreateDirectory(s.LocalDirectory);



                string fullUrl = s.serverUrl + "/Video/" + s.fileName + ".mp4";
                Txt_currentName.text = "FileName : " + s.fileName;


                UnityWebRequest req = new UnityWebRequest(fullUrl, UnityWebRequest.kHttpVerbGET);
                req.downloadHandler = new DownloadHandlerFile(s.LocalVideoPath + ".mp4");

                var operation = req.SendWebRequest();

                while (!operation.isDone)
                {
                    downloadingSphere = s;
                    //Debug.Log($"{FileName} 다운로드 중: {req.downloadProgress * 100f}%");
                    CurProgress = req.downloadProgress * 100f;
                    yield return null;
                }
                CurProgress = 100.0f;
                EntireProgress = entireProgress + (CurProgress / spheres.Count);
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("다운로드 실패: " + req.error);
                    continue;
                }
                Debug.Log($"{s.FileName} 다운로드 완료. 재생: " + s.LocalVideoPath);
                s.PlayLocalVideo();
                downloadingSphere = null;
            }
            yield return new WaitForEndOfFrame();
            OnEndDownload.Invoke();
        }

        private void OnApplicationQuit()
        {
            if (downloadingSphere != null)
            {
                Directory.Delete(downloadingSphere.LocalVideoPath + ".mp4");
            }
        }

        public void SetNullParent()
        {
            RectTransform rect = GetComponent<RectTransform>();
            Vector3 pos = rect.position;
            Quaternion rot = rect.rotation;
            rot.z = 0;
            rot.x = 0;

            transform.SetParent(null, true);

            rect.position = pos;
            rect.rotation = rot;
        }
    }
}