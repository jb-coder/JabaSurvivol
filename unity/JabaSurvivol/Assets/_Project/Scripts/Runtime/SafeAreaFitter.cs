using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Keeps UI inside the device safe area on phones and tablets.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rectTransform == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var minimum = safeArea.position;
            var maximum = safeArea.position + safeArea.size;
            minimum.x /= Screen.width;
            minimum.y /= Screen.height;
            maximum.x /= Screen.width;
            maximum.y /= Screen.height;

            _rectTransform.anchorMin = minimum;
            _rectTransform.anchorMax = maximum;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
