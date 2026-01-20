using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class DownloadManager : MonoBehaviour
{
    [SerializeField] List<PlaySphere> spheres;

    Slider curProgressBar;
    Slider entireProgressBar;

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
        }
    }



    // [TODO] 다운로드 일괄 시작 VS 다운로드 개별 시작
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
                Debug.Log($"{s.fileName}이미 다운로드됨 → 로컬에서 재생");
                s.PlayLocalVideo();
                continue;
            }

            // 폴더 생성
            Directory.CreateDirectory(s.LocalDirectory);

            string fullUrl = s.serverUrl + "/Video/" + s.fileName + ".mp4";
            Debug.Log("다운로드 시작: " + fullUrl);


            UnityWebRequest req = new UnityWebRequest(fullUrl, UnityWebRequest.kHttpVerbGET);
            req.downloadHandler = new DownloadHandlerFile(s.LocalVideoPath + ".mp4");

            var operation = req.SendWebRequest();

            while (!operation.isDone)
            {
                //Debug.Log($"{FileName} 다운로드 중: {req.downloadProgress * 100f}%");
                CurProgress = req.downloadProgress * 100f;
                yield return null;
            }
            CurProgress = 100.0f;


            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("다운로드 실패: " + req.error);
                continue;
            }
            Debug.Log($"{s.FileName} 다운로드 완료 → 재생: " + s.LocalVideoPath);
            s.PlayLocalVideo();
        }
    }
}
