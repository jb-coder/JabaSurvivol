using System;
using UnityEngine;
using UnityEngine.UI;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Keeps a static text label in sync with the active locale.
    /// Dynamic values continue to be formatted by their owning screen.
    /// </summary>
    public sealed class LocalizedText : MonoBehaviour
    {
        private Text _text;
        private Func<string> _provider;
        private bool _subscribed;

        public void Bind(string key)
        {
            Bind(() => Localization.Get(key));
        }

        public void Bind(Func<string> provider)
        {
            _text ??= GetComponent<Text>();
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            if (!_subscribed)
            {
                Localization.LocaleChanged += Apply;
                _subscribed = true;
            }

            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnDestroy()
        {
            if (_subscribed)
            {
                Localization.LocaleChanged -= Apply;
                _subscribed = false;
            }
        }

        private void Apply()
        {
            if (_text != null && _provider != null)
            {
                _text.text = _provider();
            }
        }
    }
}
