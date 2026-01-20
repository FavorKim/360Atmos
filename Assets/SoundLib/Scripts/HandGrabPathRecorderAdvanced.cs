using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if OCULUS_INTEGRATION_PRESENT
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
#endif

public enum SmoothingMode { None, Exponential, MovingAverage, OneEuro }

/// <summary>
/// Grab 중 Poke 버튼으로 녹화 토글 → 경로 라인 실시간 스무딩 표시 → Grab 해제 시 자동 재생.
/// - 손 떨림 억제(One Euro/MA/Exponential 선택)
/// - Catmull-Rom 곡선 보간 + 일정 간격 리샘플로 각짐 제거
/// - ClearPath()로 라인/이력 완전 초기화
/// </summary>
[RequireComponent(typeof(LineRenderer),typeof(AudioSource))]
public class HandGrabPathRecorderAdvanced : MonoBehaviour
{
    // ────────────── Recording / Playback 기본 ──────────────
    [Header("Record & Playback")]
    [Tooltip("녹화 당시 타이밍으로 재생할지(Off면 균일 속도)")]
    public bool useRecordedTiming = false;

    [Tooltip("useRecordedTiming이 false일 때 1초당 이동 거리(유닛/초)")]
    public float playbackSpeed = 0.6f;

    [Tooltip("재생 시 스무딩된 경로를 따를지(Off면 원시 포인트)")]
    public bool playbackSmoothedPath = true;

    // ────────────── 손 떨림 억제 & 라인 부드럽게 ──────────────
    [Header("Anti-Jitter & Curve")]
    public SmoothingMode smoothing = SmoothingMode.OneEuro;

    [Tooltip("연속 샘플 최소 시간 간격(초)")]
    public float minTimeStep = 0.01f;

    [Tooltip("연속 샘플 최소 공간 간격(미세 떨림 과밀 방지)")]
    public float minPointDistance = 0.03f;

    [Tooltip("라인 표시용 리샘플 간격(값이 작을수록 더 매끄럽지만 포인트 수 증가)")]
    public float resampleSpacing = 0.03f;

    // OneEuro 기본값(Quest/VR 권장)
    [Header("One Euro Settings")]
    public float oe_freq = 90f;
    public float oe_minCutoff = 1.2f;
    public float oe_beta = 0.4f;
    public float oe_dCutoff = 1.0f;

    // ────────────── 라인/머티리얼 ──────────────
    [Header("Line")]
    public LineRenderer line;          // 자동 할당
    public Material lineMaterial;      // 있으면 적용
    public float lineWidth = 0.01f;
    public bool showLineWhileRecording = true;
    public bool keepLineVisibleAfterRecording = true;

    // ────────────── Poke 버튼 UI ──────────────
    [Header("Poke Button (Grab 중에만 표시)")]
    public GameObject pokeButtonRoot;  // 녹화 토글용 PokeInteractable 루트(자식)
    public CanvasGroup pokeCanvasGroup; // 있으면 페이드 사용

    [Header("Optional")]
    public Rigidbody rb;

#if OCULUS_INTEGRATION_PRESENT
    HandGrabInteractable handGrab;
#endif

    // ────────────── 내부 상태 ──────────────
    bool isGrabbed = false;
    bool isRecording = false;
    bool hasPath = false;

    readonly List<Vector3> rawPoints = new();       // 원본 포인트(녹화 기준)
    readonly List<float> rawTimes = new();       // 녹화 시간(옵션)
    readonly List<Vector3> smoothDisplay = new();   // 라인 표시용 스무딩/리샘플 결과

    float recordStartTime;
    float lastSampleTime = -999f;

    Coroutine playbackCo;
    Coroutine pokeFadeCo;


    // ────────────── 스무더 구현 ──────────────
    HandPathSmoother smoother;

    // ────────────── Call Back  ──────────────
    public UnityEvent OnStartRecord;
    public UnityEvent OnStopRecord;
    public UnityEvent OnStartPlayBack;
    public UnityEvent OnClearPath;

    private void Start()
    {

        MySceneManager.Instance.AddRecord(this);
    }
    void Awake()
    {
        if (!line) line = GetComponent<LineRenderer>();
        if (!rb) rb = GetComponent<Rigidbody>();

        if (lineMaterial) line.material = lineMaterial;
        line.widthMultiplier = lineWidth;
        line.useWorldSpace = true;
        line.textureMode = LineTextureMode.Tile;   // 스크롤/타일 전제
        line.alignment = LineAlignment.View;
        line.positionCount = 0;
        line.enabled = false;

        if (pokeButtonRoot) pokeButtonRoot.SetActive(false);
        if (pokeCanvasGroup) pokeCanvasGroup.alpha = 0f;

#if OCULUS_INTEGRATION_PRESENT
        handGrab = GetComponent<HandGrabInteractable>();
#endif

        smoother = new HandPathSmoother();
        smoother.ConfigureOneEuro(oe_freq, oe_minCutoff, oe_beta, oe_dCutoff);

    }

    void OnValidate()
    {
        if (line)
        {
            line.widthMultiplier = lineWidth;
            line.textureMode = LineTextureMode.Tile;
        }
    }

    void Update()
    {
        if (!isRecording || !isGrabbed) return;

        float now = Time.time;
        float dt = now - lastSampleTime;
        if (dt < minTimeStep) return;

        Vector3 current = transform.position;

        if (rawPoints.Count > 0 && Vector3.Distance(rawPoints[^1], current) < minPointDistance)
            return;

        lastSampleTime = now;

        // 1) 원본 기록
        rawPoints.Add(current);
        rawTimes.Add(now - recordStartTime);

        // 2) 스무딩 단계
        smoother.mode = smoothing;
        var smooth = smoother.Step(current, dt);

        // 3) Catmull-Rom + 일정 간격 리샘플(원본 리스트 기준)
        var display = CatmullRom.ResampleSmooth(rawPoints, Mathf.Max(0.005f, resampleSpacing));
        smoothDisplay.Clear();
        smoothDisplay.AddRange(display);

        // 4) 라인 업데이트(스무딩 결과로 표시)
        if (showLineWhileRecording)
        {
            line.enabled = true;
            line.positionCount = smoothDisplay.Count;
            if (smoothDisplay.Count > 0)
                line.SetPositions(smoothDisplay.ToArray());
        }
    }

    // ────────────── HandGrab 이벤트(인스펙터에서 연결) ──────────────
    public void OnGrabbed()
    {
        isGrabbed = true;
        ShowPokeButton(true);

        if (playbackCo != null)
        {
            OnStopRecord.Invoke();
            StopCoroutine(playbackCo);
            playbackCo = null;
            RestorePhysics();
#if OCULUS_INTEGRATION_PRESENT
            if (handGrab) handGrab.enabled = true;
#endif
        }
    }

    public void OnReleased()
    {
        StopRecording();
        isGrabbed = false;
        ShowPokeButton(false);
        
        // 녹화가 종료되었고 경로가 있으면 자동 재생
        if (!isRecording && hasPath && (playbackSmoothedPath ? smoothDisplay.Count : rawPoints.Count) >= 2)
        {
            playbackCo = StartCoroutine(PlaybackAlongPath());
            GuideManager.Instance.ProgressGuide(4);
        }
        else
            SuppressPhysics();
    }

    // ────────────── Poke 버튼 이벤트(인스펙터 연결) ──────────────
    public void OnPokeRecord() => ToggleRecord();
    public void OnPokeReset() => ClearPath();

    // ────────────── 녹화 토글 ──────────────
    public void ToggleRecord()
    {
        if (!isGrabbed) return; // Grab 중에만 허용
        if (!isRecording) 
        {
            GuideManager.Instance.ProgressGuide(3);
            OnStartRecord.Invoke();
            StartRecording(); 
        }
    }

    void StartRecording()
    {
        // 완전 초기화 후 시작
        rawPoints.Clear();
        rawTimes.Clear();
        smoothDisplay.Clear();
        line.positionCount = 0;

        isRecording = true;
        hasPath = false;
        recordStartTime = Time.time;
        lastSampleTime = recordStartTime;

        if (showLineWhileRecording) line.enabled = true;

        // One Euro 초기화 재설정
        smoother.ConfigureOneEuro(oe_freq, oe_minCutoff, oe_beta, oe_dCutoff);
    }

    void StopRecording()
    {
        isRecording = false;
        hasPath = (playbackSmoothedPath ? smoothDisplay.Count : rawPoints.Count) >= 2;

        if (!keepLineVisibleAfterRecording)
            line.enabled = false;
        else
        {
            // 녹화 종료 시 마지막 스무딩된 경로로 고정
            line.enabled = true;
            line.positionCount = smoothDisplay.Count;
            if (smoothDisplay.Count > 0)
                line.SetPositions(smoothDisplay.ToArray());
        }
    }

    // ────────────── 재생 ──────────────
    IEnumerator PlaybackAlongPath()
    {
        var path = playbackSmoothedPath ? smoothDisplay : rawPoints;
        if (path.Count < 2) yield break;

        SuppressPhysics();

        // 콜백호출
        OnStartPlayBack.Invoke();

        // 시작점 스냅
        transform.position = path[0];

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 a = path[i];
            Vector3 b = path[i + 1];

            float segDuration;
            if (useRecordedTiming && !playbackSmoothedPath && rawTimes.Count == rawPoints.Count)
            {
                segDuration = Mathf.Max(0.0001f, rawTimes[i + 1] - rawTimes[i]);
            }
            else
            {
                float dist = Vector3.Distance(a, b);
                segDuration = Mathf.Max(0.0001f, dist / Mathf.Max(0.0001f, playbackSpeed));
            }

            float t = 0f;
            while (t < segDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / segDuration);
                transform.position = Vector3.Lerp(a, b, u);
                yield return null;
            }
        }

        RestorePhysics();
        playbackCo = null;

#if OCULUS_INTEGRATION_PRESENT
        if (handGrab) handGrab.enabled = true; // 다시 잡기 허용
#endif
    }

    // ────────────── 초기화(완전 리셋) ──────────────
    public void ClearPath()
    {
        OnClearPath?.Invoke();

        rawPoints.Clear();
        rawTimes.Clear();
        smoothDisplay.Clear();

        line.positionCount = 0;
        line.enabled = false;

        hasPath = false;
        isRecording = false;

        SuppressPhysics();
    }

    // ────────────── 표시용 Poke 버튼 페이드 ──────────────
    void ShowPokeButton(bool show)
    {
        if (!pokeButtonRoot) return;

        if (pokeFadeCo != null) StopCoroutine(pokeFadeCo);

        if (!pokeCanvasGroup)
        {
            pokeButtonRoot.SetActive(show);
        }
        else
        {
            pokeButtonRoot.SetActive(true);
            pokeFadeCo = StartCoroutine(FadePoke(show ? 1f : 0f, 0.15f, () =>
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

    // ────────────── 물리 억제/복구 ──────────────
    void SuppressPhysics()
    {
        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
#if OCULUS_INTEGRATION_PRESENT
        if (handGrab) handGrab.enabled = false; // 재집기 방지
#endif
    }

    void RestorePhysics()
    {
        if (rb) rb.isKinematic = false;
    }
}

// ───────────────────── 유틸(스무더/스플라인) ─────────────────────
public class HandPathSmoother
{
    public SmoothingMode mode = SmoothingMode.OneEuro;

    // Exponential
    public float expAlpha = 0.25f;
    Vector3 expPrev;
    bool expFirst = true;

    // Moving Average
    public int maWindow = 5;
    readonly Queue<Vector3> maQueue = new();

    // One Euro
    class OneEuroFilter
    {
        float freq, minCutoff, beta, dCutoff;
        Vector3 xPrev, dxPrev;
        bool first = true;

        public OneEuroFilter(float freq, float minCutoff, float beta, float dCutoff)
        {
            this.freq = Mathf.Max(1e-3f, freq);
            this.minCutoff = Mathf.Max(1e-3f, minCutoff);
            this.beta = Mathf.Max(0f, beta);
            this.dCutoff = Mathf.Max(1e-3f, dCutoff);
        }

        float Alpha(float cutoff, float dt)
        {
            float tau = 1f / (2f * Mathf.PI * cutoff);
            return 1f / (1f + tau / Mathf.Max(1e-4f, dt));
        }

        public Vector3 Filter(Vector3 x, float dt)
        {
            if (first)
            {
                first = false;
                xPrev = x;
                dxPrev = Vector3.zero;
                return x;
            }

            Vector3 dx = (x - xPrev) / Mathf.Max(1e-4f, dt);
            float aD = Alpha(dCutoff, dt);
            Vector3 dxHat = Vector3.Lerp(dxPrev, dx, aD);

            float cutoff = minCutoff + beta * dxHat.magnitude;
            float a = Alpha(cutoff, dt);

            Vector3 xHat = Vector3.Lerp(xPrev, x, a);
            xPrev = xHat;
            dxPrev = dxHat;
            return xHat;
        }
    }

    OneEuroFilter oneEuro = new OneEuroFilter(90f, 1.2f, 0.4f, 1.0f);

    public void ConfigureOneEuro(float freq, float minCutoff, float beta, float dCutoff)
    {
        oneEuro = new OneEuroFilter(freq, minCutoff, beta, dCutoff);
    }

    public Vector3 Step(Vector3 raw, float dt)
    {
        switch (mode)
        {
            case SmoothingMode.None:
                return raw;

            case SmoothingMode.Exponential:
                if (expFirst) { expFirst = false; expPrev = raw; return raw; }
                expPrev = Vector3.Lerp(expPrev, raw, Mathf.Clamp01(expAlpha));
                return expPrev;

            case SmoothingMode.MovingAverage:
                maQueue.Enqueue(raw);
                while (maQueue.Count > Mathf.Max(1, maWindow)) maQueue.Dequeue();
                Vector3 sum = Vector3.zero;
                foreach (var v in maQueue) sum += v;
                return sum / maQueue.Count;

            case SmoothingMode.OneEuro:
            default:
                return oneEuro.Filter(raw, Mathf.Max(1e-3f, dt));
        }
    }
}

public static class CatmullRom
{
    public static Vector3 CR(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2f * p1 +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    /// <summary>
    /// 원본 포인트를 Catmull-Rom으로 보간 후, 일정 거리 간격으로 리샘플(arc-length 근사).
    /// </summary>
    public static List<Vector3> ResampleSmooth(IList<Vector3> pts, float spacing)
    {
        var res = new List<Vector3>();
        if (pts == null || pts.Count < 2) return res;

        var p = new List<Vector3>(pts.Count + 2);
        p.Add(pts[0] + (pts[0] - pts[1]));     // 앞 가짜 포인트
        for (int i = 0; i < pts.Count; i++) p.Add(pts[i]);
        p.Add(pts[^1] + (pts[^1] - pts[^2]));  // 뒤 가짜 포인트

        // 구간당 샘플
        const int SUB = 8;
        var dense = new List<Vector3>();
        for (int i = 0; i < p.Count - 3; i++)
        {
            for (int s = 0; s <= SUB; s++)
            {
                float t = s / (float)SUB;
                dense.Add(CR(p[i], p[i + 1], p[i + 2], p[i + 3], t));
            }
        }

        res.Add(dense[0]);
        float acc = 0f;
        for (int i = 1; i < dense.Count; i++)
        {
            float d = Vector3.Distance(dense[i - 1], dense[i]);
            acc += d;
            if (acc >= spacing)
            {
                res.Add(dense[i]);
                acc = 0f;
            }
        }
        if (res[^1] != dense[^1]) res.Add(dense[^1]);
        return res;
    }
}
