using JabaSurvivol.Core;
using System.Globalization;
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
        private readonly GameObject _roundEndPanel;
        private readonly GameObject _armoryPanel;
        private readonly Text _progressText;
        private readonly Text _healthText;
        private readonly Text _roundText;
        private readonly Text _enemiesText;
        private readonly Text _killsText;
        private readonly Text _suppliesText;
        private readonly Text _levelText;
        private readonly Text _gameOverSummary;
        private readonly Text _roundEndTitle;
        private readonly Text _roundEndSummary;
        private readonly Text _armorySuppliesText;
        private readonly Text _armoryStatusText;
        private readonly Text _armoryUpgradeTitle;
        private readonly Button[] _rewardButtons;
        private readonly Text[] _rewardLabels;
        private readonly ArmoryRow[] _armoryRows;
        private readonly ArmoryUpgradeRow[] _armoryUpgradeRows;
        private readonly Image _healthFill;
        private readonly RectTransform _moveBase;
        private readonly RectTransform _aimBase;
        private readonly RectTransform _moveKnob;
        private readonly RectTransform _aimKnob;
        private readonly Image _moveBaseImage;
        private readonly Image _aimBaseImage;
        private static readonly Color HealthLow = new(0.9f, 0.2f, 0.22f);
        private static readonly Color HealthHigh = new(0.3f, 0.92f, 0.42f);
        private int _lastHealth = int.MinValue;
        private int _lastMaxHealth = int.MinValue;
        private int _lastRound = int.MinValue;
        private int _lastEnemies = int.MinValue;
        private int _lastKills = int.MinValue;
        private int _lastSupplies = int.MinValue;
        private int _lastLevel = int.MinValue;

        public GameUi(
            Transform parent,
            System.Action onStart,
            System.Action onRestart,
            System.Action onMenu,
            System.Action onPause,
            System.Action onResume,
            System.Action<int> onRewardSelected,
            System.Action onArmory,
            System.Action<WeaponId> onArmoryAction,
            System.Action<WeaponUpgradeStat> onArmoryUpgrade,
            System.Action onArmoryClose)
        {
            var canvasObject = UiFactory.CreateCanvas("JabaSurvivol UI", parent);

            var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasObject.transform, false);
            _safeArea = safeAreaObject.GetComponent<RectTransform>();
            UiFactory.Stretch(_safeArea);
            safeAreaObject.AddComponent<SafeAreaFitter>();

            _mainMenu = BuildMainMenu(onStart, onArmory);
            _hud = BuildHud(onPause, out _healthText, out _healthFill, out _roundText, out _enemiesText, out _killsText, out _suppliesText, out _levelText, out _moveBase, out _moveKnob, out _aimBase, out _aimKnob, out _moveBaseImage, out _aimBaseImage);
            _pausePanel = BuildPausePanel(onResume, onMenu, onArmory);
            _gameOverPanel = BuildGameOverPanel(onRestart, onMenu, out _gameOverSummary);
            _roundEndPanel = BuildRoundEndPanel(
                onRewardSelected,
                out _roundEndTitle,
                out _roundEndSummary,
                out _rewardButtons,
                out _rewardLabels);
            _armoryPanel = BuildArmoryPanel(
                onArmoryAction,
                onArmoryUpgrade,
                onArmoryClose,
                out _armoryRows,
                out _armoryUpgradeRows,
                out _armorySuppliesText,
                out _armoryUpgradeTitle,
                out _armoryStatusText);

            _progressText = FindText(_mainMenu, "Progress");
            ShowMainMenu(new Profile());
        }

        public void ShowMainMenu(Profile profile)
        {
            _mainMenu.SetActive(true);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
            _gameOverPanel.SetActive(false);
            HideRoundEnd();
            HideArmory();
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
            HideArmory();
            _lastHealth = int.MinValue;
            _lastMaxHealth = int.MinValue;
            _lastRound = int.MinValue;
            _lastEnemies = int.MinValue;
            _lastKills = int.MinValue;
            _lastSupplies = int.MinValue;
            _lastLevel = int.MinValue;
            HideRoundEnd();
        }

        public void UpdateHud(PlayerState player, int round, int enemiesRemaining, int kills, Profile profile)
        {
            var health = Mathf.CeilToInt(player.Health);
            var maxHealth = Mathf.CeilToInt(player.MaxHealth);

            if (health != _lastHealth || maxHealth != _lastMaxHealth)
            {
                _lastHealth = health;
                _lastMaxHealth = maxHealth;
                var healthFraction = maxHealth <= 0 ? 0f : Mathf.Clamp01((float)health / maxHealth);
                _healthFill.fillAmount = healthFraction;
                _healthFill.color = Color.Lerp(HealthLow, HealthHigh, healthFraction);
                _healthText.text = Localization.Format("hud.health", health, maxHealth);
            }

            if (round != _lastRound)
            {
                _lastRound = round;
                _roundText.text = Localization.Format("hud.round", round, GameBalance.MaxRound);
            }

            if (enemiesRemaining != _lastEnemies)
            {
                _lastEnemies = enemiesRemaining;
                _enemiesText.text = Localization.Format("hud.enemies", enemiesRemaining);
            }

            if (kills != _lastKills)
            {
                _lastKills = kills;
                _killsText.text = Localization.Format("hud.kills", kills);
            }

            if (profile.Supplies != _lastSupplies)
            {
                _lastSupplies = profile.Supplies;
                _suppliesText.text = Localization.Format("hud.supplies", profile.Supplies);
            }

            if (profile.Level != _lastLevel)
            {
                _lastLevel = profile.Level;
                _levelText.text = Localization.Format("hud.level", profile.Level);
            }
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
            HideRoundEnd();
            HideArmory();
            _gameOverSummary.text = Localization.Format("gameover.summary", round, kills, suppliesEarned);
        }

        public bool ArmoryVisible => _armoryPanel != null && _armoryPanel.activeSelf;

        public bool HudVisible => _hud != null && _hud.activeSelf;

        public void ShowArmory(Profile profile)
        {
            _mainMenu.SetActive(false);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
            _gameOverPanel.SetActive(false);
            HideRoundEnd();
            _armoryPanel.SetActive(true);
            RefreshArmory(profile);
            SetArmoryStatus(string.Empty);
        }

        public void HideArmory()
        {
            if (_armoryPanel != null)
            {
                _armoryPanel.SetActive(false);
            }
        }

        public void RefreshArmory(Profile profile)
        {
            _armorySuppliesText.text = Localization.Format("armory.supplies", profile.Supplies);

            for (var i = 0; i < _armoryRows.Length; i++)
            {
                var row = _armoryRows[i];
                var owned = profile.HasWeapon(row.WeaponId);
                var equipped = profile.EquippedWeapon == row.WeaponId;
                var definition = WeaponCatalog.Get(row.WeaponId);

                row.Name.text = Localization.WeaponName(row.WeaponId);
                row.Name.color = equipped
                    ? new Color(0.45f, 0.95f, 0.7f)
                    : new Color(0.86f, 0.92f, 1f);
                var stats = owned
                    ? WeaponUpgradeService.GetEffectiveStats(profile, row.WeaponId)
                    : definition.Stats;
                row.Info.text = FormatWeaponStats(stats);

                if (!owned)
                {
                    row.Label.text = Localization.Format("armory.buy", definition.UnlockCost);
                    row.Button.interactable = true;
                }
                else if (equipped)
                {
                    row.Label.text = Localization.Get("armory.equipped");
                    row.Button.interactable = false;
                }
                else
                {
                    row.Label.text = Localization.Get("armory.equip");
                    row.Button.interactable = true;
                }
            }

            _armoryUpgradeTitle.text = Localization.Format(
                "armory.upgrade.title",
                Localization.WeaponName(profile.EquippedWeapon));

            for (var i = 0; i < _armoryUpgradeRows.Length; i++)
            {
                var row = _armoryUpgradeRows[i];
                var track = WeaponUpgradeCatalog.Get(profile.EquippedWeapon, row.Stat);
                var tier = profile.GetWeaponUpgradeTier(profile.EquippedWeapon, row.Stat);
                row.Label.text = FormatUpgradeCard(track, tier);
                row.Button.interactable = tier < track.MaxTier;
            }
        }

        private static string FormatUpgradeCard(WeaponUpgradeTrack track, int tier)
        {
            var statName = Localization.WeaponUpgradeStatName(track.Stat);
            if (tier >= track.MaxTier)
            {
                return Localization.Format("armory.upgrade.max", statName, track.MaxTier);
            }

            var percent = (track.EffectPerTier * 100f).ToString("0.#", CultureInfo.InvariantCulture);
            return Localization.Format(
                "armory.upgrade.card",
                statName,
                tier,
                track.MaxTier,
                track.GetCost(tier + 1),
                percent);
        }

        public void SetArmoryStatus(string status)
        {
            _armoryStatusText.text = status;
        }

        public static string FormatWeaponStats(WeaponStats stats)
        {
            var reserve = stats.HasInfiniteReserve
                ? Localization.Get("armory.reserve.infinite")
                : stats.ReserveAmmo.ToString(CultureInfo.InvariantCulture);
            return Localization.Format(
                "armory.stats",
                stats.Damage.ToString("0.#", CultureInfo.InvariantCulture),
                stats.ShotsPerSecond.ToString("0.0", CultureInfo.InvariantCulture),
                stats.MagazineSize,
                stats.Range.ToString("0.#", CultureInfo.InvariantCulture),
                reserve);
        }

        public bool RoundEndVisible => _roundEndPanel != null && _roundEndPanel.activeSelf;

        public void ShowRoundEnd(
            int round,
            int kills,
            int suppliesEarned,
            int xpEarned,
            RoundRewardOption[] options)
        {
            _roundEndTitle.text = Localization.Format("round.end.title", round);
            _roundEndSummary.text = Localization.Format("round.end.summary", kills, suppliesEarned, xpEarned);

            for (var i = 0; i < _rewardButtons.Length; i++)
            {
                var hasOption = options != null && i < options.Length;
                _rewardButtons[i].gameObject.SetActive(hasOption);
                if (!hasOption)
                {
                    continue;
                }

                _rewardLabels[i].text = FormatRewardCard(options[i]);
            }

            _roundEndPanel.SetActive(true);
        }

        public void HideRoundEnd()
        {
            _roundEndPanel.SetActive(false);
        }

        private static string FormatRewardCard(RoundRewardOption reward)
        {
            var key = reward.Type switch
            {
                RoundRewardType.Supplies => "reward.supplies.card",
                RoundRewardType.Xp => "reward.xp.card",
                RoundRewardType.AmmoCrate => "reward.ammo.card",
                _ => "reward.armor.card"
            };

            return Localization.Format(key, reward.Amount);
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

        private GameObject BuildMainMenu(System.Action onStart, System.Action onArmory)
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
            UiFactory.Place(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 100f));

            var armory = UiFactory.CreateButton(root.transform, "Armory", Localization.Get("menu.armory"), 34, onArmory);
            UiFactory.Place(armory.GetComponent<RectTransform>(), new Vector2(0.5f, 0.29f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 88f));

            var controls = UiFactory.CreateText(root.transform, "Controls", Localization.Get("menu.controls"), 24, TextAnchor.MiddleCenter, new Color(0.55f, 0.62f, 0.72f));
            UiFactory.Place(controls.rectTransform, new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 50f));

            return root;
        }

        private GameObject BuildHud(
            System.Action onPause,
            out Text healthText,
            out Image healthFill,
            out Text roundText,
            out Text enemiesText,
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

            enemiesText = UiFactory.CreateText(root.transform, "Enemies", string.Empty, 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.72f, 0.42f));
            UiFactory.Place(enemiesText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(420f, 40f));

            killsText = UiFactory.CreateText(root.transform, "Kills", string.Empty, 22, TextAnchor.MiddleCenter, new Color(0.6f, 0.68f, 0.78f));
            UiFactory.Place(killsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(420f, 40f));

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

        private GameObject BuildPausePanel(System.Action onResume, System.Action onMenu, System.Action onArmory)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Pause Panel", new Color(0.01f, 0.02f, 0.03f, 0.9f));
            var root = panel.gameObject;

            var title = UiFactory.CreateText(root.transform, "Title", Localization.Get("pause.title"), 64, TextAnchor.MiddleCenter, new Color(0.5f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 100f));

            var resume = UiFactory.CreateButton(root.transform, "Resume", Localization.Get("pause.resume"), 34, onResume);
            UiFactory.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 92f));

            var armory = UiFactory.CreateButton(root.transform, "Armory", Localization.Get("pause.armory"), 30, onArmory);
            UiFactory.Place(armory.GetComponent<RectTransform>(), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 82f));

            var menu = UiFactory.CreateButton(root.transform, "Menu", Localization.Get("pause.menu"), 30, onMenu);
            UiFactory.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 82f));

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

        private GameObject BuildRoundEndPanel(
            System.Action<int> onSelect,
            out Text title,
            out Text summary,
            out Button[] buttons,
            out Text[] labels)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Round End Panel", new Color(0.012f, 0.02f, 0.035f, 0.98f));
            var root = panel.gameObject;

            title = UiFactory.CreateText(root.transform, "Title", string.Empty, 64, TextAnchor.MiddleCenter, new Color(0.4f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 90f));

            var hint = UiFactory.CreateText(root.transform, "Hint", Localization.Get("round.end.hint"), 24, TextAnchor.MiddleCenter, new Color(0.55f, 0.62f, 0.72f));
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 40f));

            summary = UiFactory.CreateText(root.transform, "Summary", string.Empty, 28, TextAnchor.MiddleCenter, new Color(0.86f, 0.9f, 0.96f));
            UiFactory.Place(summary.rectTransform, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 50f));

            buttons = new Button[GameBalance.RoundRewardOptionCount];
            labels = new Text[GameBalance.RoundRewardOptionCount];
            var anchors = new[] { 0.22f, 0.5f, 0.78f };

            for (var i = 0; i < buttons.Length; i++)
            {
                var index = i;
                var button = UiFactory.CreateButton(
                    root.transform,
                    $"Reward {i + 1}",
                    string.Empty,
                    32,
                    () => onSelect?.Invoke(index));
                UiFactory.Place(
                    button.GetComponent<RectTransform>(),
                    new Vector2(anchors[i], 0.4f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(480f, 260f));
                labels[i] = button.GetComponentInChildren<Text>();
                buttons[i] = button;
            }

            root.SetActive(false);
            return root;
        }

        private GameObject BuildArmoryPanel(
            System.Action<WeaponId> onAction,
            System.Action<WeaponUpgradeStat> onUpgrade,
            System.Action onClose,
            out ArmoryRow[] rows,
            out ArmoryUpgradeRow[] upgradeRows,
            out Text supplies,
            out Text upgradeTitle,
            out Text status)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Armory Panel", new Color(0.008f, 0.014f, 0.024f, 0.99f));
            var root = panel.gameObject;

            var title = UiFactory.CreateText(root.transform, "Title", Localization.Get("armory.title"), 64, TextAnchor.MiddleCenter, new Color(0.4f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 72f));

            supplies = UiFactory.CreateText(root.transform, "Supplies", string.Empty, 30, TextAnchor.MiddleLeft, new Color(0.55f, 0.95f, 0.75f));
            UiFactory.Place(supplies.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(620f, 52f));

            var close = UiFactory.CreateButton(root.transform, "Close", Localization.Get("armory.close"), 28, onClose);
            UiFactory.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -24f), new Vector2(220f, 76f));

            var weapons = WeaponCatalog.All;
            rows = new ArmoryRow[weapons.Count];
            for (var i = 0; i < weapons.Count; i++)
            {
                var weapon = weapons[i];
                var rowImage = UiFactory.CreateImage(
                    root.transform,
                    $"Weapon {weapon.Id}",
                    SpriteFactory.Square,
                    new Color(0.05f, 0.08f, 0.12f, 0.95f));
                UiFactory.Place(
                    rowImage.rectTransform,
                    new Vector2(0.5f, 0.82f - (i * 0.105f)),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(1660f, 84f));

                var name = UiFactory.CreateText(
                    rowImage.transform,
                    "Name",
                    string.Empty,
                    26,
                    TextAnchor.MiddleLeft,
                    Color.white);
                name.fontStyle = FontStyle.Bold;
                UiFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(280f, 78f));

                var info = UiFactory.CreateText(
                    rowImage.transform,
                    "Info",
                    string.Empty,
                    20,
                    TextAnchor.MiddleLeft,
                    new Color(0.72f, 0.8f, 0.9f));
                UiFactory.Place(info.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(310f, 0f), new Vector2(920f, 78f));

                var button = UiFactory.CreateButton(
                    rowImage.transform,
                    "Action",
                    string.Empty,
                    26,
                    () => onAction?.Invoke(weapon.Id));
                UiFactory.Place(
                    button.GetComponent<RectTransform>(),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(-20f, 0f),
                    new Vector2(300f, 66f));

                rows[i] = new ArmoryRow
                {
                    WeaponId = weapon.Id,
                    Button = button,
                    Label = button.GetComponentInChildren<Text>(),
                    Name = name,
                    Info = info
                };
            }

            upgradeTitle = UiFactory.CreateText(
                root.transform,
                "Upgrade Title",
                string.Empty,
                28,
                TextAnchor.MiddleCenter,
                new Color(0.4f, 0.9f, 1f));
            UiFactory.Place(
                upgradeTitle.rectTransform,
                new Vector2(0.5f, 0.31f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1400f, 40f));

            var upgradeStats = new[]
            {
                WeaponUpgradeStat.Damage,
                WeaponUpgradeStat.FireRate,
                WeaponUpgradeStat.MagazineSize,
                WeaponUpgradeStat.ReloadSpeed
            };
            var upgradeAnchors = new[] { 0.14f, 0.38f, 0.62f, 0.86f };
            upgradeRows = new ArmoryUpgradeRow[upgradeStats.Length];
            for (var i = 0; i < upgradeStats.Length; i++)
            {
                var stat = upgradeStats[i];
                var button = UiFactory.CreateButton(
                    root.transform,
                    $"Upgrade {stat}",
                    string.Empty,
                    22,
                    () => onUpgrade?.Invoke(stat));
                UiFactory.Place(
                    button.GetComponent<RectTransform>(),
                    new Vector2(upgradeAnchors[i], 0.18f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(400f, 145f));

                upgradeRows[i] = new ArmoryUpgradeRow
                {
                    Stat = stat,
                    Button = button,
                    Label = button.GetComponentInChildren<Text>()
                };
            }

            status = UiFactory.CreateText(
                root.transform,
                "Status",
                string.Empty,
                24,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.75f, 0.42f));
            UiFactory.Place(status.rectTransform, new Vector2(0.5f, 0.045f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 40f));

            root.SetActive(false);
            return root;
        }

        private sealed class ArmoryRow
        {
            public WeaponId WeaponId;
            public Button Button;
            public Text Label;
            public Text Name;
            public Text Info;
        }

        private sealed class ArmoryUpgradeRow
        {
            public WeaponUpgradeStat Stat;
            public Button Button;
            public Text Label;
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
