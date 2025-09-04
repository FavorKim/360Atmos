using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlaneFOFI : MonoBehaviour
{
    [SerializeField] float initialValue = 0; // 0
    [SerializeField] float targteValue = 1000; // 1000
    [SerializeField] float duration = 0.5f; // √ 

    [SerializeField] MeshRenderer Plane;
    Material mat;

    private void Start()
    {
        mat = Plane.material;
    }

    public void StartFade(bool isIn)
    {
        StartCoroutine(CorFade(isIn));
    }

    IEnumerator CorFade(bool isIn)
    {
        Plane.enabled = true;

        float elapsed = 0;
        float current = isIn? initialValue : targteValue;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (isIn == true)
                current = Mathf.Lerp(initialValue, targteValue, t);
            else
                current = Mathf.Lerp(targteValue, initialValue, t);

            mat.SetFloat("_FadeEnd", current);
            
            yield return null;
        }

        if (!isIn)
            Plane.enabled = false;
    }
}
