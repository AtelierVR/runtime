using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.audio
{
    public enum AudioMenuTriggerMode
    {
        Sound,
        Resource,
        Clip,
    }

    /// <summary>
    /// Component that triggers a sound on the nearest <see cref="IAudioMenu"/> found in parents
    /// when <see cref="OnAction"/> is called. Designed to be attached to any UI element.
    /// </summary>
    public class AudioMenuTrigger : MonoBehaviour
    {
        [Header("Audio Settings")]
        public AudioMenuTriggerMode mode = AudioMenuTriggerMode.Sound;

        [Tooltip("Used when mode is Sound")]
        public MenuSound sound = MenuSound.Click;

        [Tooltip("Used when mode is Resource")]
        public ResourceIdentifier resource;

        [Tooltip("Used when mode is Clip")]
        public AudioClip clip;

        [Tooltip("Optional override source. Leave null to use the menu default.")]
        public AudioSource source;

        public float delay = 0f;

        /// <summary>
        /// Finds the nearest <see cref="IAudioMenu"/> in parents (including inactive)
        /// and plays the configured sound on it.
        /// </summary>
        /// <returns>The <see cref="IAudioPlay"/> handle, or null if no menu was found.</returns>
        public void OnAction()
        {
            var menu = GetComponentInParent<IAudioMenu>(includeInactive: true);
            if (menu == null)
                return;

            switch (mode)
            {
                case AudioMenuTriggerMode.Sound:
                    menu.Play(sound, source, delay);
                    break;
                case AudioMenuTriggerMode.Resource:
                    menu.Play(resource, source, delay);
                    break;
                case AudioMenuTriggerMode.Clip:
                    menu.Play(clip, source, delay);
                    break;
            }
        }
    }
}
