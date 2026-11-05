using JabaSurvivol.Core;
using UnityEngine;
using UnityEngine.UI;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Runtime-built HUD and menu for the walking skeleton. All user-facing
    /// strings flow through <see cref="Localization"/> keys.
    /// </summary>
    public sealed class GameUi
    {
        private readonly RectTransform _safeArea;
        private readonly GameObject _mainMenu;
        private readonly GameObject _hud;
        private readonly GameObject _pausePanel;
        private readonly GameObject _gameOverPanel;
        private readonly Text _progressText;
        private readonly Text _healthText;
        private readonly Text _roundText;
        private readonly Text _killsText;
        private readonly Text _suppliesText;
        private readonly Text _levelText;
        private readonly Text _gameOverSummary;
        private readonly Image _healthFill;
        private readonly RectTransform _moveBase;
        private readonly RectTransform _aimBase;
        private readonly RectTransform _moveKnob;
        private readonly RectTransform _aimKnob;
        private readonly Image _moveBaseImage;
        private readonly Image _aimBaseImage;

        public GameUi(
            Transform parent,
            System.Action onStart,
            System.Action onRestart,
            System.Action onMenu,
            System.Action onPause,
            System.Action onResume)
        {
            var canvasObject = UiFactory.CreateCanvas("JabaSurvivol UI", parent);

            var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasObject.transform, false);
            _safeArea = safeAreaObject.GetComponent<RectTransform>();
            UiFactory.Stretch(_safeArea);
            safeAreaObject.AddComponent<SafeAreaFitter>();

            _mainMenu = BuildMainMenu(onStart);
            _hud = BuildHud(onPause, out _healthText, out _healthFill, out _roundText, out _killsText, out _suppliesText, out _levelText, out _moveBase, out _moveKnob, out _aimBase, out _aimKnob, out _moveBaseImage, out _aimBaseImage);
            _pausePanel = BuildPausePanel(onResume, onMenu);
            _gameOverPanel = BuildGameOverPanel(onRestart, onMenu, out _gameOverSummary);

            _progressText = FindText(_mainMenu, "Progress");
            ShowMainMenu(new Profile());
        }

        public void ShowMainMenu(Profile profile)
        {
            _mainMenu.SetActive(true);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
            _gameOverPanel.SetActive(false);
            _progressText.text = Localization.Format(
                "menu.progress",
                profile.Supplies,
                profile.Level,
                profile.HighestRound);
        }

        public void ShowHud()
        {
            _mainMenu.SetActive(false);
            _hud.SetActive(true);
            _pausePanel.SetActive(false);
            _gameOverPanel.SetActive(false);
        }

        public void UpdateHud(PlayerState player, int round, int kills, Profile profile)
        {
            var healthFraction = player.MaxHealth <= 0f ? 0f : Mathf.Clamp01(player.Health / player.MaxHealth);
            _healthFill.fillAmount = healthFraction;
            _healthFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.22f), new Color(0.3f, 0.92f, 0.42f), healthFraction);
            _healthText.text = Localization.Format("hud.health", Mathf.CeilToInt(player.Health), Mathf.CeilToInt(player.MaxHealth));
            _roundText.text = Localization.Format("hud.round", round);
            _killsText.text = Localization.Format("hud.kills", kills);
            _suppliesText.text = Localization.Format("hud.supplies", profile.Supplies);
            _levelText.text = Localization.Format("hud.level", profile.Level);
        }

        public void ShowPaused(bool visible)
        {
            _pausePanel.SetActive(visible);
        }

        public void ShowGameOver(int round, int kills, int suppliesEarned)
        {
            _mainMenu.SetActive(false);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
            _gameOverPanel.SetActive(true);
            _gameOverSummary.text = Localization.Format("gameover.summary", round, kills, suppliesEarned);
        }

        public void SetSticks(StickState move, StickState aim)
        {
            UpdateStick(_moveBase, _moveKnob, _moveBaseImage, move, false);
            UpdateStick(_aimBase, _aimKnob, _aimBaseImage, aim, true);
        }

        private void UpdateStick(
            RectTransform baseRect,
            RectTransform knob,
            Image baseImage,
            StickState state,
            bool right)
        {
            const float baseSize = 260f;
            const float edgePadding = 150f;

            if (state.Active)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _safeArea,
                        state.Origin,
                        null,
                        out var localPoint))
                {
                    baseRect.anchorMin = new Vector2(0.5f, 0.5f);
                    baseRect.anchorMax = new Vector2(0.5f, 0.5f);
                    baseRect.anchoredPosition = localPoint;
                }

                var delta = state.Current - state.Origin;
                var direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.zero;
                knob.anchoredPosition = direction * (baseSize * 0.32f);
                baseImage.color = new Color(0.45f, 0.9f, 1f, 0.34f);
            }
            else
            {
                baseRect.anchorMin = new Vector2(right ? 1f : 0f, 0f);
                baseRect.anchorMax = new Vector2(right ? 1f : 0f, 0f);
                baseRect.anchoredPosition = new Vector2(right ? -edgePadding : edgePadding, edgePadding);
                knob.anchoredPosition = Vector2.zero;
                baseImage.color = new Color(0.7f, 0.82f, 0.9f, 0.15f);
            }
        }

        private GameObject BuildMainMenu(System.Action onStart)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Main Menu", new Color(0.012f, 0.018f, 0.03f, 0.98f));
            var root = panel.gameObject;

            var title = UiFactory.CreateText(root.transform, "Title", Localization.Get("app.title"), 86, TextAnchor.MiddleCenter, new Color(0.4f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1300f, 120f));

            var subtitle = UiFactory.CreateText(root.transform, "Subtitle", Localization.Get("menu.subtitle"), 28, TextAnchor.MiddleCenter, new Color(0.78f, 0.84f, 0.92f));
            UiFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.67f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1300f, 50f));

            var progress = UiFactory.CreateText(root.transform, "Progress", string.Empty, 28, TextAnchor.MiddleCenter, new Color(0.55f, 0.92f, 0.7f));
            UiFactory.Place(progress.rectTransform, new Vector2(0.5f, 0.57f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1300f, 50f));

            var start = UiFactory.CreateButton(root.transform, "Start", Localization.Get("menu.start"), 38, onStart);
            UiFactory.Place(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 112f));

            var controls = UiFactory.CreateText(root.transform, "Controls", Localization.Get("menu.controls"), 24, TextAnchor.MiddleCenter, new Color(0.55f, 0.62f, 0.72f));
            UiFactory.Place(controls.rectTransform, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 50f));

            return root;
        }

        private GameObject BuildHud(
            System.Action onPause,
            out Text healthText,
            out Image healthFill,
            out Text roundText,
            out Text killsText,
            out Text suppliesText,
            out Text levelText,
            out RectTransform moveBase,
            out RectTransform moveKnob,
            out RectTransform aimBase,
            out RectTransform aimKnob,
            out Image moveBaseImage,
            out Image aimBaseImage)
        {
            var root = new GameObject("HUD", typeof(RectTransform));
            root.transform.SetParent(_safeArea, false);
            UiFactory.Stretch(root.GetComponent<RectTransform>());

            var healthBackground = UiFactory.CreateImage(root.transform, "Health", SpriteFactory.Square, new Color(0.04f, 0.06f, 0.09f, 0.9f));
            UiFactory.Place(healthBackground.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(440f, 52f));

            healthFill = UiFactory.CreateImage(healthBackground.transform, "Health Fill", SpriteFactory.Square, new Color(0.3f, 0.92f, 0.42f));
            UiFactory.Stretch(healthFill.rectTransform);
            healthFill.rectTransform.offsetMin = new Vector2(4f, 4f);
            healthFill.rectTransform.offsetMax = new Vector2(-4f, -4f);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            healthText = UiFactory.CreateText(healthBackground.transform, "Health Text", string.Empty, 24, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Stretch(healthText.rectTransform);
            healthText.fontStyle = FontStyle.Bold;

            roundText = UiFactory.CreateText(root.transform, "Round", string.Empty, 36, TextAnchor.MiddleCenter, new Color(0.9f, 0.95f, 1f));
            roundText.fontStyle = FontStyle.Bold;
            UiFactory.Place(roundText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(420f, 60f));

            killsText = UiFactory.CreateText(root.transform, "Kills", string.Empty, 22, TextAnchor.MiddleCenter, new Color(0.6f, 0.68f, 0.78f));
            UiFactory.Place(killsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(420f, 40f));

            suppliesText = UiFactory.CreateText(root.transform, "Supplies", string.Empty, 28, TextAnchor.MiddleRight, new Color(0.55f, 0.95f, 0.75f));
            UiFactory.Place(suppliesText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -30f), new Vector2(420f, 44f));

            levelText = UiFactory.CreateText(root.transform, "Level", string.Empty, 24, TextAnchor.MiddleRight, new Color(0.6f, 0.68f, 0.78f));
            UiFactory.Place(levelText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -78f), new Vector2(420f, 40f));

            var pause = UiFactory.CreateButton(root.transform, "Pause", Localization.Get("hud.pause"), 28, onPause);
            UiFactory.Place(pause.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(84f, 84f));

            var hint = UiFactory.CreateText(root.transform, "Controls Hint", Localization.Get("menu.controls"), 20, TextAnchor.MiddleCenter, new Color(0.48f, 0.56f, 0.66f));
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1400f, 40f));

            moveBaseImage = BuildStick(root.transform, "Move Stick", out moveBase, out moveKnob);
            aimBaseImage = BuildStick(root.transform, "Aim Stick", out aimBase, out aimKnob);
            moveBaseImage.color = new Color(0.7f, 0.82f, 0.9f, 0.15f);
            aimBaseImage.color = new Color(0.7f, 0.82f, 0.9f, 0.15f);
            moveBase.anchorMin = moveBase.anchorMax = new Vector2(0f, 0f);
            moveBase.anchoredPosition = new Vector2(150f, 150f);
            aimBase.anchorMin = aimBase.anchorMax = new Vector2(1f, 0f);
            aimBase.anchoredPosition = new Vector2(-150f, 150f);

            return root;
        }

        private static Image BuildStick(Transform parent, string name, out RectTransform baseRect, out RectTransform knobRect)
        {
            var baseImage = UiFactory.CreateImage(parent, name, SpriteFactory.Ring, new Color(0.7f, 0.82f, 0.9f, 0.15f));
            baseRect = baseImage.rectTransform;
            baseRect.anchorMin = new Vector2(0.5f, 0.5f);
            baseRect.anchorMax = new Vector2(0.5f, 0.5f);
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.sizeDelta = new Vector2(260f, 260f);

            var knobImage = UiFactory.CreateImage(baseImage.transform, "Knob", SpriteFactory.Circle, new Color(0.72f, 0.95f, 1f, 0.42f));
            knobRect = knobImage.rectTransform;
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(96f, 96f);
            knobRect.anchoredPosition = Vector2.zero;

            return baseImage;
        }

        private GameObject BuildPausePanel(System.Action onResume, System.Action onMenu)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Pause Panel", new Color(0.01f, 0.02f, 0.03f, 0.9f));
            var root = panel.gameObject;

            var title = UiFactory.CreateText(root.transform, "Title", Localization.Get("pause.title"), 64, TextAnchor.MiddleCenter, new Color(0.5f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 100f));

            var resume = UiFactory.CreateButton(root.transform, "Resume", Localization.Get("pause.resume"), 34, onResume);
            UiFactory.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 100f));

            var menu = UiFactory.CreateButton(root.transform, "Menu", Localization.Get("pause.menu"), 30, onMenu);
            UiFactory.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 88f));

            return root;
        }

        private GameObject BuildGameOverPanel(System.Action onRestart, System.Action onMenu, out Text summary)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Game Over Panel", new Color(0.02f, 0.008f, 0.012f, 0.96f));
            var root = panel.gameObject;

            var title = UiFactory.CreateText(root.transform, "Title", Localization.Get("gameover.title"), 72, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.38f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 110f));

            summary = UiFactory.CreateText(root.transform, "Summary", string.Empty, 30, TextAnchor.MiddleCenter, new Color(0.86f, 0.9f, 0.96f));
            UiFactory.Place(summary.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 120f));

            var restart = UiFactory.CreateButton(root.transform, "Restart", Localization.Get("gameover.restart"), 36, onRestart);
            UiFactory.Place(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 104f));

            var menu = UiFactory.CreateButton(root.transform, "Menu", Localization.Get("gameover.menu"), 30, onMenu);
            UiFactory.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 88f));

            return root;
        }

        private static Text FindText(GameObject root, string name)
        {
            var texts = root.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                if (text.name == name)
                {
                    return text;
                }
            }

            return null;
        }
    }
}
