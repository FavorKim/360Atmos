using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;
namespace SoundLibrary
{
    [RequireComponent(typeof(PokeInteractable))]
    public class MyPokeObject : MonoBehaviour
    {
        PokeInteractable poke;
        [SerializeField] UnityEvent onPokeCallBack;

        void Start()
        {
            poke = GetComponent<PokeInteractable>();
            poke.WhenStateChanged += Poke_WhenStateChanged;
        }

        private void Poke_WhenStateChanged(InteractableStateChangeArgs obj)
        {
            if (obj.NewState == InteractableState.Select)
            {
                onPokeCallBack?.Invoke();
            }
        }

    }
}