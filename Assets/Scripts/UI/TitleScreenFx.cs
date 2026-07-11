using UnityEngine;
using UnityEngine.UI;

namespace EL4S.UI
{
    // Idle motion for the title screen: fades the whole canvas in on load, floats
    // the logo text, and pulses a graphic's alpha (used for the "searching for a
    // match" status text so the wait doesn't read as a frozen screen).
    public class TitleScreenFx : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeInDuration = 0.6f;

        [SerializeField] private RectTransform floatingLogo;
        [SerializeField] private float floatAmplitude = 8f;
        [SerializeField] private float floatSpeed = 1.2f;

        [SerializeField] private Graphic pulsingGraphic;
        [SerializeField] private float pulseMinAlpha = 0.55f;
        [SerializeField] private float pulseSpeed = 1.8f;

        private Vector2 _logoBasePosition;
        private float _fadeElapsed;

        private void Start()
        {
            if (floatingLogo != null)
            {
                _logoBasePosition = floatingLogo.anchoredPosition;
            }

            if (fadeGroup != null)
            {
                fadeGroup.alpha = 0f;
            }
        }

        private void Update()
        {
            if (fadeGroup != null && fadeGroup.alpha < 1f)
            {
                _fadeElapsed += Time.deltaTime;
                fadeGroup.alpha = Mathf.Clamp01(_fadeElapsed / fadeInDuration);
            }

            if (floatingLogo != null)
            {
                var offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                floatingLogo.anchoredPosition = _logoBasePosition + new Vector2(0f, offset);
            }

            if (pulsingGraphic != null)
            {
                var t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                var color = pulsingGraphic.color;
                color.a = Mathf.Lerp(pulseMinAlpha, 1f, t);
                pulsingGraphic.color = color;
            }
        }
    }
}
