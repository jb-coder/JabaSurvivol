using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Small runtime UI factory so the walking skeleton can build a responsive
    /// canvas without authored prefab dependencies.
    /// </summary>
    public static class UiFactory
    {
        private static Font _font;

        public static GameObject CreateCanvas(string name, Transform parent)
        {
            var canvasObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemObject = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                eventSystemObject.transform.SetParent(parent, false);
                var module = eventSystemObject.GetComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }

            return canvasObject;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color, bool blocksRaycasts = true)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            Stretch(rect);
            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksRaycasts;
            return rect;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = GetFont();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            return text;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            int fontSize,
            Action onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var background = buttonObject.GetComponent<Image>();
            background.sprite = SpriteFactory.Square;
            background.type = Image.Type.Simple;
            background.color = new Color(0.12f, 0.32f, 0.42f, 0.95f);
            background.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(0.82f, 1f, 1f, 1f),
                pressedColor = new Color(0.62f, 0.85f, 0.9f, 1f),
                selectedColor = new Color(0.82f, 1f, 1f, 1f),
                disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            button.onClick.AddListener(() => onClick?.Invoke());

            var text = CreateText(buttonObject.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);

            return button;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Place(
            RectTransform rect,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static Font GetFont()
        {
            if (_font != null)
            {
                return _font;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            }

            return _font;
        }
    }
}
