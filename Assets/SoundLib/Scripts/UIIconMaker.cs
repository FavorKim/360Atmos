using UnityEditor;
using UnityEngine;
namespace SoundLibrary
{
    [RequireComponent(typeof(MeshRenderer))]
    public class UIIconMaker : MonoBehaviour
    {
        MeshRenderer mr;
        [SerializeField] Texture2D texture;

        public void ApplyUIUnlitTexture(MeshRenderer mr, Texture2D tex, bool useShared = false)
        {
            if (mr == null)
            {
                Debug.LogWarning("ApplyUIUnlitTexture: MeshRenderer가 null 입니다.");
                return;
            }

            // 1) 사용할 셰이더 탐색 (우선순위: UI/Unlit → URP Unlit → UI/Default → Sprites/Default)
            Shader shader =
                Shader.Find("UI/Unlit/Transparent") ??
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("UI/Default") ??
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                Debug.LogError("ApplyUIUnlitTexture: 적절한 Unlit 셰이더를 찾지 못했습니다.");
                return;
            }

            // 2) 머티리얼 생성/할당
            Material mat = useShared ? mr.sharedMaterial : mr.material;
            if (mat == null || mat.shader != shader)
            {
                mat = new Material(shader);
                if (useShared) mr.sharedMaterial = mat;
                else mr.material = mat;
            }

            // 3) 텍스처 적용 (파이프라인별 속성명 대응)
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);   // URP Unlit
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);   // UI/Unlit, UI/Default, Sprites/Default
            }

            // 4) 색/알파 기본값 보정 (흰색 = 원본 색 유지)
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);

            // 알파 블렌딩 필요한 경우(아이콘 등 투명 텍스처) 키워드/렌더 큐 보정
            // 대부분의 UI/Unlit/Transparent는 기본 세팅으로 충분하지만, 안전하게 처리
            //mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        public void SetTextureOnly(Texture2D tex)
        {
            var mat = mr.material;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }

        private void Start()
        {
            mr = GetComponent<MeshRenderer>();
            ApplyUIUnlitTexture(mr, texture);
        }
    }
}