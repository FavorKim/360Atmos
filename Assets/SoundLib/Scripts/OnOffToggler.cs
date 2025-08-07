using UnityEngine;

public class OnOffToggler : MonoBehaviour
{
    public void SetActiveToggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
