using UnityEngine;
namespace SoundLibrary
{
    public class OnOffToggler : MonoBehaviour
    {
        public void SetActiveToggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}