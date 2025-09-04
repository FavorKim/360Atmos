using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [Header("대상 카메라(비워두면 자동 탐색)")]
    public Transform cameraTransform;

    [Header("옵션")]
    public bool yAxisOnly = false;            // Y축만 빌보드(표지판처럼)
    public Vector3 rotationOffsetEuler;       // 모델 정면 축 보정이 필요하면 여기서 조정(예: (0,180,0))

    void Awake()
    {
        if (cameraTransform == null)
        {
            // 1) MainCamera 우선
            if (Camera.main != null) cameraTransform = Camera.main.transform;

            // 2) (옵션) Oculus Integration을 쓰면 CenterEyeAnchor도 시도
            if (cameraTransform == null)
            {
                var rig = FindAnyObjectByType<OVRCameraRig>();
                if (rig != null) cameraTransform = rig.centerEyeAnchor;
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 toCam = transform.position - cameraTransform.position;

        if (yAxisOnly)
        {
            toCam.y = 0f; // 수평면에서만 바라보게
            if (toCam.sqrMagnitude < 1e-6f) return;
        }

        // 카메라를 바라보는 방향으로 회전
        Quaternion look = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        transform.rotation = look * Quaternion.Euler(rotationOffsetEuler);
    }
}

