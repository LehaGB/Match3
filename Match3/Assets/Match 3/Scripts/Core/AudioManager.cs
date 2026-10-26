using UnityEngine;


namespace Match3.Core
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _swipeClip;
        [SerializeField] private AudioClip _matchClip;
        [SerializeField] private AudioClip _fallClip;

        public void PlaySwipe() => _source.PlayOneShot(_swipeClip, 0.5f);
        public void PlayMatch() => _source.PlayOneShot(_matchClip, 0.7f);
        public void PlayFall() => _source.PlayOneShot(_fallClip, 0.5f);
    }
}

