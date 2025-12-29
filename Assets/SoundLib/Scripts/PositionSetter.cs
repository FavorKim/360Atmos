using UnityEngine;

public class PositionSetter : MonoBehaviour
{
    [SerializeField] Transform headPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.position = headPos.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
