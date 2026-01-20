using NUnit.Framework;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
[RequireComponent (typeof(HandGrabInteractable))]
public class OnGrabEventer : MonoBehaviour
{
    public UnityEvent OnGrab;
    public UnityEvent OnReleased;

    void Start()
    {
        var grab = GetComponent<HandGrabInteractable>();

        grab.WhenStateChanged += Grab_WhenStateChanged;
    }
    private void Grab_WhenStateChanged(InteractableStateChangeArgs obj)
    {
        // OnGrab
        if (obj.NewState == InteractableState.Select)
        {
            OnGrab?.Invoke();
        }
        // OnReleased
        else if (obj.NewState == InteractableState.Normal)
        {
            OnReleased?.Invoke();
        }
    }
}
