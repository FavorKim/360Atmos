using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace SoundLibrary
{
    public class PostProcessingToggle : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera; // 비우면 Camera.main 사용
        private UniversalAdditionalCameraData camData;
        private LayerMask cachedVolumeMask;

        void Awake()
        {
            if (!targetCamera) targetCamera = Camera.main;
            camData = targetCamera.GetComponent<UniversalAdditionalCameraData>();
            if (!camData) camData = targetCamera.GetUniversalAdditionalCameraData(); // 확장 메서드

            // 꺼졌을 때 볼륨 조회비용까지 줄이고 싶다면 마스크도 캐시
            cachedVolumeMask = camData ? camData.volumeLayerMask : default;
        }

        public void SetPostFX(bool enabled)
        {
            if (camData == null) return;
            camData.renderPostProcessing = enabled;

            // 선택: 완전히 끌 땐 볼륨 레이어 마스크도 0으로 (성능 최적화)
            camData.volumeLayerMask = enabled ? cachedVolumeMask : 0;
        }

        public void TogglePostFX()
        {
            if (camData == null) return;
            SetPostFX(!camData.renderPostProcessing);
        }
    }
}
