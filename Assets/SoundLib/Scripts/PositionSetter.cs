using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PositionSetter : MonoBehaviour
{
    [SerializeField] Transform headPos;
    [SerializeField] Transform nonplayer;
    [SerializeField] Image circle;

    public UnityEvent OnEndCalibration;

    [SerializeField] bool isCalibrating;

    [SerializeField] float calibrationDuration;


    private void Start()
    {
        nonplayer.gameObject.SetActive(false);
    }

    public void SetPosition()
    {
        nonplayer.transform.position = headPos.position;
        Vector3 dir = Vector3.ProjectOnPlane(headPos.forward, Vector3.up);

        if (dir.sqrMagnitude > 0.0001f)
            nonplayer.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private void Update()
    {
        circle.fillAmount += isCalibrating ? Time.deltaTime / calibrationDuration : -Time.deltaTime / calibrationDuration;
        if (circle.fillAmount >= 1)
            OnEndCalibration.Invoke();
    }

    public void EnableCalibration(bool enable)
    {
        isCalibrating = enable;
    }
}
