using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SoundLibrary
{
    public class PositionSetter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform nonplayer;
        [SerializeField] Image circle;

        [Tooltip("nonplayer를 trackingSpace 기준으로 정렬/추적할 트래킹 스페이스(예: XROrigin 아래 Camera Offset, 또는 OVRCameraRig의 TrackingSpace).")]
        [SerializeField] Transform trackingSpace;

        [Tooltip("HMD 카메라(Head) 트랜스폼. Y(높이) 보정에만 사용됩니다.")]
        [SerializeField] Transform head;

        [Tooltip("nonplayer의 Y를 head 높이로 맞춥니다. (XZ는 trackingSpace 기준 원점에 고정)")]
        [SerializeField] bool alignYToHead = true;

        [Header("Y Lerp Settings")]
        [Tooltip("head와 nonplayer의 Y 차이가 이 값 이상일 때 nonplayer를 head Y로 lerp합니다.")]
        [Min(0)]
        [SerializeField] float yLerpThreshold = 0.5f;

        [Tooltip("nonplayer Y가 head Y로 이동하는 속도(클수록 더 빠르게 따라옵니다).")]
        [Min(0.01f)]
        [SerializeField] float yLerpSpeed = 10f;

        // SmoothDamp 내부에서 쓰는 현재 속도(프레임 간 연속성을 위해 보관)
        float ySmoothVelocity;

        [Header("Calibration UI")]
        public UnityEvent OnEndCalibration;

        [SerializeField] bool isCalibrating;

        [SerializeField] float calibrationDuration;

        bool ended;

        private void Start()
        {
            nonplayer.gameObject.SetActive(false);
            TryAttachNonplayerToTrackingSpace();
            TryAutoAssignHead();

            if (circle == null)
                Debug.LogWarning("[PositionSetter] circle(Image) reference is null. Calibration progress won't work.");
        }

        private void OnEnable()
        {
            // Meta/Oculus/XR recenter 이벤트에 의존하지 않습니다.
        }

        private void OnDisable()
        {
            // Meta/Oculus/XR recenter 이벤트에 의존하지 않습니다.
        }

        public void RecenterAndAlignNonplayer()
        {
            Debug.Log($"[PositionSetter] RecenterAndAlignNonplayer() called. nonplayer={(nonplayer ? nonplayer.name : "null")}, trackingSpace={(trackingSpace ? trackingSpace.name : "null")}, head={(head ? head.name : "null")}");

            TryAttachNonplayerToTrackingSpace();
            TryAutoAssignHead();

            AlignNow();
        }

        float GetHeadYInTrackingSpace()
        {
            if (!alignYToHead || head == null || trackingSpace == null)
                return 0f;

            if (head.IsChildOf(trackingSpace))
                return head.localPosition.y;

            return trackingSpace.InverseTransformPoint(head.position).y;
        }

        void AlignNow()
        {
            // trackingSpace 기준 원점/정면에 고정
            if (nonplayer != null && trackingSpace != null && nonplayer.parent == trackingSpace)
            {
                float y = GetHeadYInTrackingSpace();
                nonplayer.localPosition = new Vector3(0f, y, 0f);
                nonplayer.localRotation = Quaternion.identity;
                ySmoothVelocity = 0f; // 스냅 이후 잔여 속도 제거
                Debug.Log($"[PositionSetter] aligned. localY={nonplayer.localPosition.y:0.000}, parentOK={ReferenceEquals(nonplayer.parent, trackingSpace)}");
            }
            else
            {
                Debug.LogWarning($"[PositionSetter] align skipped. nonplayerNull={nonplayer == null}, trackingSpaceNull={trackingSpace == null}, parent={(nonplayer ? nonplayer.parent?.name : "n/a")}");
                ySmoothVelocity = 0f;
            }
        }

        // 기존 씬/유니티 이벤트 호환용 엔트리 포인트
        public void SetPosition()
        {
            RecenterAndAlignNonplayer();
        }

        private void Update()
        {
            if (circle != null)
            {
                float denom = Mathf.Max(0.01f, calibrationDuration);
                circle.fillAmount += isCalibrating ? Time.deltaTime / denom : -Time.deltaTime / denom;
                if (!ended && circle.fillAmount >= 1f)
                {
                    ended = true;
                    Debug.Log($"[PositionSetter] calibration completed (fill={circle.fillAmount:0.###}) -> align");
                    RecenterAndAlignNonplayer();
                    OnEndCalibration.Invoke();
                }
                if (circle.fillAmount <= 0f && !isCalibrating)
                {
                    ended = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                RecenterAndAlignNonplayer();
                OnEndCalibration.Invoke();
            }

            // 리센터를 트리거하지 않고, head와의 Y 오차가 커질 때만 nonplayer Y를 따라오게 합니다.
            if (!isCalibrating && alignYToHead && nonplayer != null && trackingSpace != null && head != null)
            {
                // nonplayer를 trackingSpace 아래로 유지(부모가 깨졌을 때를 대비)
                TryAttachNonplayerToTrackingSpace();

                if (nonplayer.parent == trackingSpace)
                {
                    float currentY = nonplayer.localPosition.y;
                    float targetY = GetHeadYInTrackingSpace();
                    float diff = Mathf.Abs(targetY - currentY);

                    if (diff >= yLerpThreshold)
                    {
                        // SmoothDamp는 "느리게 시작 -> 가운데 속도 붙음 -> 목표 근처에서 감속"
                        // 같은 감쇠 곡선을 만들어 줍니다.
                        float smoothTime = Mathf.Max(0.0001f, 1f / Mathf.Max(0.01f, yLerpSpeed));
                        float newY = Mathf.SmoothDamp(currentY, targetY, ref ySmoothVelocity, smoothTime);

                        nonplayer.localPosition = new Vector3(0f, newY, 0f);
                        nonplayer.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        // 너무 작게 흔들리지 않도록, 임계값 미만이면 속도 상태를 리셋
                        ySmoothVelocity = 0f;
                    }
                }
            }
        }

        public void EnableCalibration(bool enable)
        {
            isCalibrating = enable;
            Debug.Log($"[PositionSetter] EnableCalibration({enable}) duration={calibrationDuration:0.###} fill={(circle ? circle.fillAmount.ToString("0.###") : "null")}");
        }

        void TryAttachNonplayerToTrackingSpace()
        {
            if (nonplayer == null || trackingSpace == null) return;
            if (nonplayer.parent == trackingSpace) return;

            // nonplayer가 trackingSpace 하위에 있어야 localPosition 기준으로 Y 비교/정렬이 가능합니다.
            nonplayer.SetParent(trackingSpace, worldPositionStays: false);
        }

        void TryAutoAssignHead()
        {
            if (head != null) return;

            if (Camera.main != null)
            {
                head = Camera.main.transform;
                return;
            }

            if (trackingSpace != null)
            {
                var cam = trackingSpace.GetComponentInChildren<Camera>(includeInactive: true);
                if (cam != null)
                    head = cam.transform;
            }
        }
    }
}