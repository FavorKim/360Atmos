using Oculus.Interaction.HandGrab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
#if OCULUS_INTEGRATION_PRESENT
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using static OVRInput;
#endif

[RequireComponent(typeof(LineRenderer))]
public class HandGrabPathRecorder : MonoBehaviour
{

#if OCULUS_INTEGRATION_PRESENT
    public Button ovrButton = Button.One;
    public Controller ovrController = Controller.RTouch;
    HandGrabInteractable handGrab;
#endif

    [Header("Record Settings")]
    public float minPointDistance = 0.02f;
    public bool useRecordedTiming = false;
    public float playbackSpeed = 0.6f;

    [Header("Line Settings")]
    public LineRenderer line;
    public Material lineMaterial;
    public float lineWidth = 0.01f;
    public bool showLineWhileRecording = true;
    public bool keepLineVisibleAfterRecording = true;

    [Header("Poke Button (shown only while grabbed)")]
    public GameObject pokeButtonRoot;     // Grab 중에만 활성화
    public CanvasGroup pokeCanvasGroup;   // 있으면 부드럽게 페이드

    [Header("Optional")]
    public Rigidbody rb;
    public UnityEvent OnStartRecord;
    public UnityEvent OnStopRecord;

    // state
    bool isGrabbed = false;
    bool isRecording = false;
    bool hasPath = false;

    readonly List<Vector3> points = new();
    readonly List<float> times = new();
    float recordStartTime;

    Coroutine playbackCo;
    Coroutine pokeFadeCo;

    void Awake()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (lineMaterial != null) line.material = lineMaterial;
        line.positionCount = 0;
        line.widthMultiplier = lineWidth;
        line.useWorldSpace = true;
        line.enabled = false;

        // 처음엔 버튼 숨김
        if (pokeButtonRoot != null) pokeButtonRoot.SetActive(false);
        if (pokeCanvasGroup != null) pokeCanvasGroup.alpha = 0f;

        
#if OCULUS_INTEGRATION_PRESENT
        handGrab = GetComponent<HandGrabInteractable>();
#endif
    }


    void Update()
    {
#if OCULUS_INTEGRATION_PRESENT
        if (useOVRInput && isGrabbed && OVRInput.GetDown(ovrButton, ovrController))
            ToggleRecord();
#endif
        if (isRecording && isGrabbed)
        {
            Vector3 current = transform.position;
            if (points.Count == 0 || Vector3.Distance(points[^1], current) >= minPointDistance)
                AddPoint(current);
        }
    }

    // ===== HandGrabInteractable 이벤트에 연결 =====
    public void OnGrabbed()
    {
        isGrabbed = true;
        ShowPokeButton(true);

        // 재생 중 집으면 정지
        if (playbackCo != null)
        {
            OnStopRecord.Invoke();
            StopCoroutine(playbackCo);
            playbackCo = null;
            RestorePhysics();
#if OCULUS_INTEGRATION_PRESENT
            if (handGrab != null) handGrab.enabled = true;
#endif
        }
    }

    public void OnReleased()
    {
        StopRecording();
        isGrabbed = false;
        ShowPokeButton(false);
        if (!isRecording && hasPath && points.Count >= 2)
            playbackCo = StartCoroutine(PlaybackAlongPath());
    }

    // ===== PokeInteractable → WhenSelect에 연결할 함수 =====
    public void OnPokePressed() => ToggleRecord();

    // ===== 녹화 토글 =====
    void OnRecordPerformed(InputAction.CallbackContext _) => ToggleRecord();

    public void ToggleRecord()
    {
        if (!isGrabbed) return; // Grab 중에만 허용

        if (!isRecording)
        {
            OnStartRecord.Invoke();
            StartRecording();
        }
    }

    void StartRecording()
    {
        points.Clear();
        times.Clear();
        line.positionCount = 0;

        isRecording = true;
        hasPath = false;
        recordStartTime = Time.time;

        if (showLineWhileRecording) line.enabled = true;

        AddPoint(transform.position);

        // 녹화 시작 시 버튼 살짝 피드백 주고 싶으면 깜빡임 등 추가 가능
    }

    void StopRecording()
    {
        isRecording = false;
        hasPath = points.Count >= 2;

        if (!keepLineVisibleAfterRecording) line.enabled = false;
        else line.enabled = true;
    }

    void AddPoint(Vector3 pos)
    {
        points.Add(pos);
        times.Add(Time.time - recordStartTime);
        line.positionCount = points.Count;
        line.SetPosition(points.Count - 1, pos);
    }

    IEnumerator PlaybackAlongPath()
    {
        SuppressPhysics();

        if (points.Count > 0) transform.position = points[0];

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];

            float segmentDuration;
            if (useRecordedTiming && times.Count == points.Count)
                segmentDuration = Mathf.Max(0.0001f, times[i + 1] - times[i]);
            else
                segmentDuration = Mathf.Max(0.0001f, Vector3.Distance(a, b) / Mathf.Max(0.0001f, playbackSpeed));

            float t = 0f;
            while (t < segmentDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / segmentDuration);
                transform.position = Vector3.Lerp(a, b, u);
                yield return null;
            }
        }

        RestorePhysics();
        playbackCo = null;
#if OCULUS_INTEGRATION_PRESENT
        if (handGrab != null) handGrab.enabled = true;
#endif
    }

    void SuppressPhysics()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
#if OCULUS_INTEGRATION_PRESENT
        if (handGrab != null) handGrab.enabled = false; // 재집기 방지
#endif
    }

    void RestorePhysics()
    {
        if (rb != null) rb.isKinematic = false;
    }

    public void ClearPath()
    {
        points.Clear();
        times.Clear();
        line.positionCount = 0;
        isRecording = false;
        hasPath = false;
        line.enabled = false;
    }

    // ===== Poke 버튼 표시/숨김 =====
    void ShowPokeButton(bool show)
    {
        if (pokeButtonRoot == null)
            return;

        if (pokeFadeCo != null) StopCoroutine(pokeFadeCo);

        if (pokeCanvasGroup == null)
        {
            pokeButtonRoot.SetActive(show);
        }
        else
        {
            pokeButtonRoot.SetActive(true); // 페이드 위해 우선 켬
            pokeFadeCo = StartCoroutine(FadePoke(show ? 1f : 0f, 0.15f, onDone: () =>
            {
                if (!show) pokeButtonRoot.SetActive(false);
            }));
        }
    }

    IEnumerator FadePoke(float target, float dur, System.Action onDone)
    {
        float start = pokeCanvasGroup.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            pokeCanvasGroup.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        pokeCanvasGroup.alpha = target;
        onDone?.Invoke();
    }
}
