using JabaSurvivol.Core;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private readonly Text _buildLabel;
        private readonly GameObject _hud;
        private readonly GameObject _pausePanel;
        private readonly Button _pauseMenuButton;
        private readonly GameObject _confirmationPanel;
        private readonly Text _confirmationTitle;
        private readonly Text _confirmationBody;
        private readonly Button _confirmationConfirmButton;
        private readonly Text _confirmationConfirmLabel;
        private readonly Button _confirmationCancelButton;
        private readonly Text _confirmationCancelLabel;
        private readonly GameObject _helpPanel;
        private readonly Text _helpTitle;
        private readonly Text _helpBody;
        private readonly Button _helpCloseButton;
        private readonly Button _helpButtonMain;
        private readonly Button _helpButtonSettings;
        private System.Action _confirmationConfirmAction;
        private System.Action _confirmationCancelAction;
        private readonly GameObject _settingsPanel;
        private readonly Button _settingsButtonMain;
        private readonly Button _settingsButtonPause;
        private readonly Button _settingsCloseButton;
        private readonly Button _settingsResetButton;
        private readonly Button _settingsMuteButton;
        private readonly Text _settingsMuteLabel;
        private readonly Button _settingsMasterMinusButton;
        private readonly Button _settingsMasterPlusButton;
        private readonly Text _settingsMasterLabel;
        private readonly Button _settingsEffectsMinusButton;
        private readonly Button _settingsEffectsPlusButton;
        private readonly Text _settingsEffectsLabel;
        private readonly Button _settingsLanguageSystemButton;
        private readonly Button _settingsLanguageEnglishButton;
        private readonly Button _settingsLanguageSpanishButton;
        private readonly Text _settingsLanguageCurrentLabel;
        private readonly Button _settingsLeftHandedButton;
        private readonly Text _settingsLeftHandedLabel;
        private readonly Button _settingsJoystickSizeMinusButton;
        private readonly Button _settingsJoystickSizePlusButton;
        private readonly Text _settingsJoystickSizeLabel;
        private readonly Button _settingsJoystickOpacityMinusButton;
        private readonly Button _settingsJoystickOpacityPlusButton;
        private readonly Text _settingsJoystickOpacityLabel;
        private readonly Button _settingsAimAssistButton;
        private readonly Text _settingsAimAssistLabel;
        private readonly Button _settingsAutoFireButton;
        private readonly Text _settingsAutoFireLabel;
        private readonly Button _settingsReducedMotionButton;
        private readonly Text _settingsReducedMotionLabel;
        private readonly Button _settingsHapticsButton;
        private readonly Text _settingsHapticsLabel;
        private readonly Button _settingsColorblindButton;
        private readonly Text _settingsColorblindLabel;
        private readonly System.Action<string> _onLocaleSelected;
        private readonly System.Action _onResetSettings;
        private readonly System.Action<bool> _onFireHeld;
        private bool _muted;
        private float _masterVolume = GameAudio.DefaultMasterVolume;
        private float _effectsVolume = GameAudio.DefaultEffectsVolume;
        private readonly GameObject _gameOverPanel;
        private readonly GameObject _victoryPanel;
        private readonly GameObject _bossPanel;
        private readonly GameObject _roundEndPanel;
        private readonly GameObject _armoryPanel;
        private readonly GameObject _armoryWeaponsPanel;
        private readonly GameObject _armoryArmorPanel;
        private readonly GameObject _characterSheet;
        private readonly Text _progressText;
        private readonly Text _healthText;
        private readonly Text _armorText;
        private readonly Text _roundText;
        private readonly Text _roundModifierText;
        private readonly Text _enemiesText;
        private readonly Text _killsText;
        private readonly Text _suppliesText;
        private readonly Text _levelText;
        private readonly Text _levelUpText;
        private readonly Text _weaponText;
        private readonly Image _weaponIcon;
        private readonly Text _ammoText;
        private readonly Text _reloadText;
        private readonly Image _reloadFill;
        private readonly Button _activeSkillButton;
        private readonly Image _activeSkillCooldown;
        private readonly Image _activeSkillIcon;
        private readonly Text _activeSkillLabel;
        private readonly Button[] _selectActiveButtons;
        private readonly Text[] _selectActiveLabels;
        private readonly Image[] _selectActiveIcons;
        private readonly Text _gameOverSummary;
        private readonly Text _victorySummary;
        private readonly Text _bossNameText;
        private readonly Text _bossHealthText;
        private readonly Image _bossHealthFill;
        private readonly Text _roundEndTitle;
        private readonly Text _roundEndSummary;
        private readonly Text _armorySuppliesText;
        private readonly Text _armoryStatusText;
        private readonly Text _armoryUpgradeTitle;
        private readonly Text _armoryArmorUpgradeTitle;
        private readonly Button _armoryArmorUpgradeButton;
        private readonly Text _armoryArmorUpgradeLabel;
        private readonly Text _characterLevelText;
        private readonly Text _characterPointsText;
        private readonly Text _characterStatsText;
        private readonly Text _characterSynergyText;
        private readonly Text _characterStatusText;
        private readonly GameObject _accountPanel;
        private readonly Text _accountStateText;
        private readonly Text _accountStatusText;
        private readonly InputField _accountUsername;
        private readonly InputField _accountPassword;
        private readonly Button _accountRegisterButton;
        private readonly Button _accountLoginButton;
        private readonly Button _accountLogoutButton;
        private readonly Text _cloudStatusText;
        private readonly Button _cloudBackupButton;
        private readonly Button _cloudRestoreButton;
        private readonly GameObject _cloudConflictPanel;
        private readonly Text _cloudConflictText;
        private readonly GameObject _leaderboardPanel;
        private readonly Text _leaderboardStatusText;
        private readonly Text _leaderboardText;
        private readonly RectTransform _leaderboardViewport;
        private readonly Button _telemetryButton;
        private readonly Text _telemetryLabel;
        private Text _mainMenuAccountState;
        private readonly Button _characterRespecButton;
        private readonly Text _characterRespecLabel;
        private readonly Button[] _characterTabs;
        private readonly Text[] _characterTabLabels;
        private readonly GameObject[] _characterBranchPanels;
        private readonly CharacterNodeRow[] _characterNodeRows;
        private readonly Button[] _rewardButtons;
        private readonly Text[] _rewardLabels;
        private readonly Image[] _rewardIcons;
        private readonly ArmoryRow[] _armoryRows;
        private readonly ArmoryUpgradeRow[] _armoryUpgradeRows;
        private readonly ArmorRow[] _armoryArmorRows;
        private readonly Image _healthFill;
        private readonly RectTransform _moveBase;
        private readonly RectTransform _aimBase;
        private readonly RectTransform _moveKnob;
        private readonly RectTransform _aimKnob;
        private readonly Image _moveBaseImage;
        private readonly Image _aimBaseImage;
        private readonly Image _moveKnobImage;
        private readonly Image _aimKnobImage;
        private readonly Button _reloadButton;
        private readonly Button _switchWeaponButton;
        private readonly Button _pauseButton;
        private readonly Button _fireButton;
        private readonly Text _controlsHint;
        private readonly Text _mainMenuControls;
        private bool _moveStickOnRight;
        private bool _aimStickOnRight;
        private float _joystickScale = 1f;
        private float _joystickOpacity = 1f;
        private const float StickBaseSize = 260f;
        private const float StickKnobSize = 96f;
        private const float StickEdgeMargin = 20f;
        private const float FireButtonSize = 130f;
        private const float AudioVolumeStep = 0.1f;
        private static readonly Color HealthLow = new(0.9f, 0.2f, 0.22f);
        private static readonly Color HealthHigh = new(0.3f, 0.92f, 0.42f);
        private int _lastHealth = int.MinValue;
        private int _lastMaxHealth = int.MinValue;
        private int _lastArmor = int.MinValue;
        private int _lastMaxArmor = int.MinValue;
        private int _lastRound = int.MinValue;
        private int _lastEnemies = int.MinValue;
        private int _lastKills = int.MinValue;
        private int _lastSupplies = int.MinValue;
        private int _lastLevel = int.MinValue;
        private int _lastXp = int.MinValue;
        private int _lastSkillPoints = int.MinValue;
        private int _lastBossHealth = int.MinValue;
        private int _lastBossMaxHealth = int.MinValue;
        private bool _lastBossEnraged;
        private string _lastWeaponName = string.Empty;
        private WeaponId _lastWeaponId = (WeaponId)(-1);
        private int _lastMagazine = int.MinValue;
        private int _lastMagazineSize = int.MinValue;
        private int _lastReserve = int.MinValue;
        private bool _lastInfiniteReserve;
        private bool _lastReloading;
        private SkillNode _lastSkillNode = SkillNode.Turret;
        private int _lastSkillRank = int.MinValue;
        private bool _lastSkillReady;
        private int _lastSkillCooldownPercent = int.MinValue;
        private int _lastSkillActivePercent = int.MinValue;
        private StickState _lastMoveStick;
        private StickState _lastAimStick;
        private bool _sticksInitialized;
        private bool _characterSheetTablet;
        private int _activeCharacterBranch;

        public GameUi(
            Transform parent,
            System.Action onStart,
            System.Action onRestart,
            System.Action onMenu,
            System.Action onPauseMenu,
            System.Action onPause,
            System.Action onResume,
            System.Action onToggleSound,
            System.Action<float> onMasterVolumeChanged,
            System.Action<float> onEffectsVolumeChanged,
            System.Action onReload,
            System.Action onSwitchWeapon,
            System.Action onActiveSkill,
            System.Action<bool> onFireHeld,
            System.Action<SkillNode> onSelectActiveSkill,
            System.Action<int> onRewardSelected,
            System.Action onArmory,
            System.Action<WeaponId> onArmoryAction,
            System.Action<WeaponUpgradeStat> onArmoryUpgrade,
            System.Action<ArmorTier> onArmorAction,
            System.Action onArmorUpgrade,
            System.Action onCharacterSheet,
            System.Action<SkillNode> onAllocateSkill,
            System.Action onRespecSkills,
            System.Action onCharacterSheetClose,
            System.Action onArmoryClose,
            System.Action onAccount,
            System.Action<string, string> onRegister,
            System.Action<string, string> onLogin,
            System.Action onLogout,
            System.Action onAccountClose,
            System.Action onCloudBackup,
            System.Action onCloudRestore,
            System.Action onCloudUseCloud,
            System.Action onCloudKeepLocal,
            System.Action onLeaderboard,
            System.Action onTelemetryToggle,
            System.Action<string> onLocaleSelected,
            System.Action onResetSettings,
            System.Action onLeaderboardClose)
        {
            _onLocaleSelected = onLocaleSelected;
            _onResetSettings = onResetSettings;
            _onFireHeld = onFireHeld;
            var canvasObject = UiFactory.CreateCanvas("JabaSurvivol UI", parent);

            var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasObject.transform, false);
            _safeArea = safeAreaObject.GetComponent<RectTransform>();
            UiFactory.Stretch(_safeArea);
            safeAreaObject.AddComponent<SafeAreaFitter>();

            _mainMenu = BuildMainMenu(
                onStart,
                onArmory,
                onCharacterSheet,
                onAccount,
                onLeaderboard,
                onTelemetryToggle,
                ShowSettingsPanel,
                ShowHelp,
                out _mainMenuAccountState,
                out _telemetryButton,
                out _telemetryLabel,
                out _buildLabel,
                out _settingsButtonMain,
                out _helpButtonMain);
            _hud = BuildHud(onPause, onReload, onSwitchWeapon, onActiveSkill, onFireHeld, out _healthText, out _armorText, out _healthFill, out _roundText, out _enemiesText, out _killsText, out _suppliesText, out _levelText, out _moveBase, out _moveKnob, out _aimBase, out _aimKnob, out _moveBaseImage, out _aimBaseImage, out _moveKnobImage, out _aimKnobImage);
            _reloadButton = FindGameObject(_hud, "Reload Button")?.GetComponent<Button>();
            _switchWeaponButton = FindGameObject(_hud, "Switch Weapon")?.GetComponent<Button>();
            _pauseButton = FindGameObject(_hud, "Pause")?.GetComponent<Button>();
            _fireButton = FindGameObject(_hud, "Fire Button")?.GetComponent<Button>();
            _controlsHint = FindText(_hud, "Controls Hint");
            _mainMenuControls = FindText(_mainMenu, "Controls");
            _roundModifierText = FindText(_hud, "Round Modifier");
            _weaponText = FindText(_hud, "Weapon");
            _weaponIcon = FindImage(_hud, "Weapon Icon");
            _ammoText = FindText(_hud, "Ammo");
            _reloadText = FindText(_hud, "Reload Status");
            _reloadFill = FindImage(_hud, "Reload Fill");
            _activeSkillButton = FindGameObject(_hud, "Active Skill Button")?.GetComponent<Button>();
            _activeSkillCooldown = FindImage(_hud, "Active Skill Cooldown");
            _activeSkillIcon = FindImage(_hud, "Active Skill Icon");
            _activeSkillLabel = FindText(_hud, "Active Skill Label");
            _bossPanel = FindGameObject(_hud, "Boss Panel");
            _bossNameText = FindText(_hud, "Boss Name");
            _bossHealthText = FindText(_hud, "Boss Health Text");
            _bossHealthFill = FindImage(_hud, "Boss Health Fill");
            _pausePanel = BuildPausePanel(
                onResume,
                onPauseMenu,
                onArmory,
                onCharacterSheet,
                ShowSettingsPanel,
                out _settingsButtonPause,
                out _pauseMenuButton);
            _settingsPanel = BuildSettingsPanel(
                onToggleSound,
                onMasterVolumeChanged,
                onEffectsVolumeChanged,
                out _settingsMuteButton,
                out _settingsMuteLabel,
                out _settingsMasterMinusButton,
                out _settingsMasterPlusButton,
                out _settingsMasterLabel,
                out _settingsEffectsMinusButton,
                out _settingsEffectsPlusButton,
                out _settingsEffectsLabel,
                out _settingsLanguageSystemButton,
                out _settingsLanguageEnglishButton,
                out _settingsLanguageSpanishButton,
                out _settingsLanguageCurrentLabel,
                out _settingsLeftHandedButton,
                out _settingsLeftHandedLabel,
                out _settingsJoystickSizeMinusButton,
                out _settingsJoystickSizePlusButton,
                out _settingsJoystickSizeLabel,
                out _settingsJoystickOpacityMinusButton,
                out _settingsJoystickOpacityPlusButton,
                out _settingsJoystickOpacityLabel,
                out _settingsAimAssistButton,
                out _settingsAimAssistLabel,
                out _settingsAutoFireButton,
                out _settingsAutoFireLabel,
                out _settingsReducedMotionButton,
                out _settingsReducedMotionLabel,
                out _settingsHapticsButton,
                out _settingsHapticsLabel,
                out _settingsColorblindButton,
                out _settingsColorblindLabel,
                out _settingsResetButton,
                out _settingsCloseButton,
                out _helpButtonSettings);
            _gameOverPanel = BuildGameOverPanel(onRestart, onMenu, out _gameOverSummary);
            _victoryPanel = BuildVictoryPanel(onRestart, onMenu, onLeaderboard, out _victorySummary);
            _roundEndPanel = BuildRoundEndPanel(
                onRewardSelected,
                onCharacterSheet,
                out _roundEndTitle,
                out _roundEndSummary,
                out _rewardButtons,
                out _rewardLabels,
                out _rewardIcons);
            _armoryPanel = BuildArmoryPanel(
                onArmoryAction,
                onArmoryUpgrade,
                onArmorAction,
                onArmorUpgrade,
                onCharacterSheet,
                onArmoryClose,
                out _armoryRows,
                out _armoryUpgradeRows,
                out _armoryArmorRows,
                out _armoryWeaponsPanel,
                out _armoryArmorPanel,
                out _armorySuppliesText,
                out _armoryUpgradeTitle,
                out _armoryArmorUpgradeTitle,
                out _armoryArmorUpgradeButton,
                out _armoryArmorUpgradeLabel,
                out _armoryStatusText);
            _characterSheet = BuildCharacterSheet(
                onAllocateSkill,
                onRespecSkills,
                onSelectActiveSkill,
                onCharacterSheetClose,
                out _characterLevelText,
                out _characterPointsText,
                out _characterStatsText,
                out _characterStatusText,
                out _characterRespecButton,
                out _characterRespecLabel,
                out _characterTabs,
                out _characterTabLabels,
                out _characterBranchPanels,
                out _characterNodeRows);
            _characterSynergyText = FindText(_characterSheet, "Synergies");
            _accountPanel = BuildAccountPanel(
                onRegister,
                onLogin,
                onLogout,
                onAccountClose,
                onCloudBackup,
                onCloudRestore,
                out _accountStateText,
                out _accountStatusText,
                out _accountUsername,
                out _accountPassword,
                out _accountRegisterButton,
                out _accountLoginButton,
                out _accountLogoutButton,
                out _cloudStatusText,
                out _cloudBackupButton,
                out _cloudRestoreButton);
            _cloudConflictPanel = BuildCloudConflictPanel(
                onCloudUseCloud,
                onCloudKeepLocal,
                out _cloudConflictText);
            _leaderboardPanel = BuildLeaderboardPanel(
                onLeaderboardClose,
                out _leaderboardStatusText,
                out _leaderboardText,
                out _leaderboardViewport);
            _confirmationPanel = BuildConfirmationPanel(
                out _confirmationTitle,
                out _confirmationBody,
                out _confirmationConfirmButton,
                out _confirmationConfirmLabel,
                out _confirmationCancelButton,
                out _confirmationCancelLabel);
            _helpPanel = BuildHelpPanel(
                out _helpTitle,
                out _helpBody,
                out _helpCloseButton);

            _selectActiveButtons = new Button[3];
            _selectActiveLabels = new Text[3];
            _selectActiveIcons = new Image[3];
            var activeNodes = new[] { SkillNode.Shockwave, SkillNode.Dash, SkillNode.Turret };
            for (var i = 0; i < activeNodes.Length; i++)
            {
                var selectButton = FindGameObject(_characterSheet, $"Select {activeNodes[i]}");
                _selectActiveButtons[i] = selectButton?.GetComponent<Button>();
                _selectActiveLabels[i] = selectButton?.GetComponentInChildren<Text>();
                _selectActiveIcons[i] = FindImage(_characterSheet, $"Select {activeNodes[i]} Icon");
            }

            _levelUpText = UiFactory.CreateText(
                _safeArea,
                "Level Up",
                string.Empty,
                34,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.85f, 0.35f));
            _levelUpText.fontStyle = FontStyle.Bold;
            UiFactory.Place(
                _levelUpText.rectTransform,
                new Vector2(0.5f, 0.72f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 60f));
            _levelUpText.gameObject.SetActive(false);

            _progressText = FindText(_mainMenu, "Progress");
            BindStaticLocalization();
            ShowMainMenu(new Profile());
        }

        public void ShowMainMenu(Profile profile)
        {
            HideHelpSilently();
            _mainMenu.SetActive(true);
            _hud.SetActive(false);
            _onFireHeld?.Invoke(false);
            _pausePanel.SetActive(false);
            CloseSettingsPanelSilently();
            HideConfirmation();
            _gameOverPanel.SetActive(false);
            HideVictory();
            HideBoss();
            HideRoundEnd();
            HideArmory();
            HideCharacterSheet();
            HideAccount();
            HideLeaderboard();
            _progressText.text = Localization.Format(
                "menu.progress",
                profile.Supplies,
                profile.Level,
                profile.HighestRound);
        }

        public void ShowHud()
        {
            HideHelpSilently();
            _mainMenu.SetActive(false);
            _hud.SetActive(true);
            _pausePanel.SetActive(false);
            CloseSettingsPanelSilently();
            HideConfirmation();
            _gameOverPanel.SetActive(false);
            HideVictory();
            HideBoss();
            HideArmory();
            HideCharacterSheet();
            HideAccount();
            HideLeaderboard();
            InvalidateHudCache();
            HideLevelUp();
            HideRoundEnd();
            ApplyControlLayout();
        }

        /// <summary>
        /// Applies the device-local touch layout: stick side, stick size,
        /// opacity, action-button mirroring, the manual fire control, and the
        /// controls hint text.
        /// </summary>
        public void ApplyControlLayout()
        {
            var leftHanded = GameSettings.LeftHanded;
            _joystickScale = GameSettings.JoystickSize;
            _joystickOpacity = GameSettings.JoystickOpacity;
            _moveStickOnRight = leftHanded;
            _aimStickOnRight = !leftHanded;

            var baseSize = StickBaseSize * _joystickScale;
            var knobSize = StickKnobSize * _joystickScale;
            var edgePadding = (baseSize * 0.5f) + StickEdgeMargin;

            if (_moveBase != null)
            {
                _moveBase.sizeDelta = new Vector2(baseSize, baseSize);
                _moveBase.anchorMin = _moveBase.anchorMax = new Vector2(_moveStickOnRight ? 1f : 0f, 0f);
                _moveBase.anchoredPosition = new Vector2(_moveStickOnRight ? -edgePadding : edgePadding, edgePadding);
            }

            if (_aimBase != null)
            {
                _aimBase.sizeDelta = new Vector2(baseSize, baseSize);
                _aimBase.anchorMin = _aimBase.anchorMax = new Vector2(_aimStickOnRight ? 1f : 0f, 0f);
                _aimBase.anchoredPosition = new Vector2(_aimStickOnRight ? -edgePadding : edgePadding, edgePadding);
            }

            if (_moveKnob != null)
            {
                _moveKnob.sizeDelta = new Vector2(knobSize, knobSize);
                _moveKnob.anchoredPosition = Vector2.zero;
            }

            if (_aimKnob != null)
            {
                _aimKnob.sizeDelta = new Vector2(knobSize, knobSize);
                _aimKnob.anchoredPosition = Vector2.zero;
            }

            if (_moveBaseImage != null)
            {
                _moveBaseImage.color = new Color(0.7f, 0.82f, 0.9f, 0.15f * _joystickOpacity);
            }

            if (_aimBaseImage != null)
            {
                _aimBaseImage.color = new Color(0.7f, 0.82f, 0.9f, 0.15f * _joystickOpacity);
            }

            if (_moveKnobImage != null)
            {
                _moveKnobImage.color = new Color(0.72f, 0.95f, 1f, 0.42f * _joystickOpacity);
            }

            if (_aimKnobImage != null)
            {
                _aimKnobImage.color = new Color(0.72f, 0.95f, 1f, 0.42f * _joystickOpacity);
            }

            PlaceActionButtons(leftHanded);

            var controlsKey = leftHanded ? "menu.controls.leftHanded" : "menu.controls";
            if (_controlsHint != null)
            {
                _controlsHint.text = Localization.Get(controlsKey);
            }

            if (_mainMenuControls != null)
            {
                _mainMenuControls.text = Localization.Get(controlsKey);
            }

            if (_fireButton != null)
            {
                _fireButton.gameObject.SetActive(!GameSettings.AutoFire);
            }

            if (GameSettings.AutoFire)
            {
                _onFireHeld?.Invoke(false);
            }

            _sticksInitialized = false;
        }

        private void PlaceActionButtons(bool leftHanded)
        {
            var baseSize = StickBaseSize * _joystickScale;
            var edgePadding = (baseSize * 0.5f) + StickEdgeMargin;
            var skillOffset = baseSize + 80f;

            if (_reloadButton != null)
            {
                UiFactory.Place(
                    _reloadButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(leftHanded ? 110f : -110f, 84f),
                    new Vector2(190f, 66f));
            }

            if (_switchWeaponButton != null)
            {
                UiFactory.Place(
                    _switchWeaponButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(leftHanded ? -100f : 100f, 84f),
                    new Vector2(190f, 66f));
            }

            if (_activeSkillButton != null)
            {
                var anchor = new Vector2(leftHanded ? 0f : 1f, 0f);
                UiFactory.Place(
                    _activeSkillButton.GetComponent<RectTransform>(),
                    anchor,
                    anchor,
                    new Vector2(leftHanded ? 170f : -170f, skillOffset),
                    new Vector2(130f, 130f));
            }

            if (_pauseButton != null)
            {
                var anchor = new Vector2(leftHanded ? 0f : 1f, 1f);
                UiFactory.Place(
                    _pauseButton.GetComponent<RectTransform>(),
                    anchor,
                    anchor,
                    new Vector2(leftHanded ? 30f : -30f, -30f),
                    new Vector2(84f, 84f));
            }

            if (_fireButton != null)
            {
                var fireOffset = edgePadding + (baseSize * 0.5f) + (FireButtonSize * 0.5f) + StickEdgeMargin;
                UiFactory.Place(
                    _fireButton.GetComponent<RectTransform>(),
                    new Vector2(_aimStickOnRight ? 1f : 0f, 0f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(_aimStickOnRight ? -fireOffset : fireOffset, edgePadding),
                    new Vector2(FireButtonSize, FireButtonSize));
            }
        }

        public void InvalidateHudCache()
        {
            _lastHealth = int.MinValue;
            _lastMaxHealth = int.MinValue;
            _lastArmor = int.MinValue;
            _lastMaxArmor = int.MinValue;
            _lastRound = int.MinValue;
            _lastEnemies = int.MinValue;
            _lastKills = int.MinValue;
            _lastSupplies = int.MinValue;
            _lastLevel = int.MinValue;
            _lastXp = int.MinValue;
            _lastSkillPoints = int.MinValue;
            _lastBossHealth = int.MinValue;
            _lastBossMaxHealth = int.MinValue;
            _lastBossEnraged = false;
            _lastWeaponName = string.Empty;
            _lastMagazine = int.MinValue;
            _lastMagazineSize = int.MinValue;
            _lastReserve = int.MinValue;
            _lastInfiniteReserve = false;
            _lastReloading = false;
            _lastSkillNode = SkillNode.Turret;
            _lastSkillRank = int.MinValue;
            _lastSkillReady = false;
            _lastSkillCooldownPercent = int.MinValue;
            _lastSkillActivePercent = int.MinValue;
        }

        public void UpdateHud(PlayerState player, int armor, int maxArmor, int round, int enemiesRemaining, int kills, Profile profile)
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

            if (armor != _lastArmor || maxArmor != _lastMaxArmor)
            {
                _lastArmor = armor;
                _lastMaxArmor = maxArmor;
                _armorText.text = maxArmor > 0 || armor > 0
                    ? Localization.Format("hud.armor", armor, maxArmor)
                    : string.Empty;
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

            if (profile.Level != _lastLevel ||
                profile.Xp != _lastXp ||
                profile.SkillPoints != _lastSkillPoints)
            {
                _lastLevel = profile.Level;
                _lastXp = profile.Xp;
                _lastSkillPoints = profile.SkillPoints;
                _levelText.text = Localization.Format(
                    "hud.level",
                    profile.Level,
                    profile.Xp,
                    Profile.XpToNext(profile.Level),
                    profile.SkillPoints);
            }
        }

        /// <summary>
        /// Shows the elite round modifier on the HUD.
        /// </summary>
        public void SetRoundModifier(bool isElite)
        {
            if (_roundModifierText == null)
            {
                return;
            }

            _roundModifierText.text = isElite
                ? Localization.Get("hud.elite")
                : string.Empty;
        }

        /// <summary>
        /// Test hook that presses a character-sheet active-skill button.
        /// </summary>
        public bool ClickSelectActiveSkillForTests(SkillNode node)
        {
            var index = node switch
            {
                SkillNode.Shockwave => 0,
                SkillNode.Dash => 1,
                SkillNode.Turret => 2,
                _ => -1
            };

            if (index < 0 || _selectActiveButtons[index] == null ||
                !_selectActiveButtons[index].interactable)
            {
                return false;
            }

            _selectActiveButtons[index].onClick.Invoke();
            return true;
        }

        public bool RoundModifierVisible => _roundModifierText != null &&
                                            !string.IsNullOrEmpty(_roundModifierText.text);

        /// <summary>
        /// Updates the equipped weapon, magazine, reserve, and reload state.
        /// Values are cached so steady-state frames do not allocate.
        /// </summary>
        public void UpdateWeaponHud(
            WeaponId weaponId,
            string weaponName,
            int magazine,
            int magazineSize,
            int reserve,
            bool infiniteReserve,
            bool reloading,
            float reloadRemaining,
            float reloadDuration)
        {
            if (_weaponText == null || _ammoText == null || _reloadText == null || _reloadFill == null)
            {
                return;
            }

            if (weaponId != _lastWeaponId ||
                !string.Equals(weaponName, _lastWeaponName, System.StringComparison.Ordinal) ||
                magazine != _lastMagazine ||
                magazineSize != _lastMagazineSize ||
                reserve != _lastReserve ||
                infiniteReserve != _lastInfiniteReserve)
            {
                _lastWeaponId = weaponId;
                _lastWeaponName = weaponName;
                _lastMagazine = magazine;
                _lastMagazineSize = magazineSize;
                _lastReserve = reserve;
                _lastInfiniteReserve = infiniteReserve;
                _weaponText.text = weaponName;
                if (_weaponIcon != null)
                {
                    _weaponIcon.sprite = ArtCatalog.Get(WeaponIconKey(weaponId));
                }
                _ammoText.text = infiniteReserve
                    ? Localization.Format(
                        "hud.ammo.infinite",
                        magazine,
                        magazineSize,
                        Localization.Get("armory.reserve.infinite"))
                    : Localization.Format("hud.ammo", magazine, magazineSize, reserve);
            }

            if (reloading != _lastReloading)
            {
                _lastReloading = reloading;
                _reloadText.text = reloading ? Localization.Get("hud.reloading") : string.Empty;
            }

            _reloadFill.fillAmount = reloading && reloadDuration > 0f
                ? Mathf.Clamp01(1f - (reloadRemaining / reloadDuration))
                : 0f;
        }

        /// <summary>
        /// Updates the active skill button, label, and radial cooldown.
        /// </summary>
        public void UpdateActiveSkillHud(
            SkillNode node,
            int rank,
            bool ready,
            float cooldownFraction,
            float activeFraction)
        {
            if (_activeSkillButton == null || _activeSkillCooldown == null || _activeSkillLabel == null)
            {
                return;
            }

            var cooldownPercent = Mathf.RoundToInt(Mathf.Clamp01(cooldownFraction) * 100f);
            var activePercent = Mathf.RoundToInt(Mathf.Clamp01(activeFraction) * 100f);
            if (node == _lastSkillNode &&
                rank == _lastSkillRank &&
                ready == _lastSkillReady &&
                cooldownPercent == _lastSkillCooldownPercent &&
                activePercent == _lastSkillActivePercent)
            {
                return;
            }

            _lastSkillNode = node;
            _lastSkillRank = rank;
            _lastSkillReady = ready;
            _lastSkillCooldownPercent = cooldownPercent;
            _lastSkillActivePercent = activePercent;

            _activeSkillLabel.text = rank <= 0
                ? Localization.Get("hud.skill.locked")
                : Localization.SkillName(node);
            if (_activeSkillIcon != null)
            {
                _activeSkillIcon.sprite = ArtCatalog.Get(SkillIconKey(node));
            }

            _activeSkillButton.interactable = ready;
            _activeSkillCooldown.fillAmount = Mathf.Clamp01(cooldownFraction);
            _activeSkillCooldown.color = activeFraction > 0f
                ? new Color(0.3f, 1f, 0.55f, 0.55f)
                : new Color(0.02f, 0.02f, 0.03f, 0.78f);
        }

        public void ShowLevelUp(int level)
        {
            _levelUpText.text = Localization.Format("hud.levelup", level);
            _levelUpText.gameObject.SetActive(true);
        }

        public void HideLevelUp()
        {
            if (_levelUpText != null)
            {
                _levelUpText.gameObject.SetActive(false);
            }
        }

        public void ShowPaused(bool visible)
        {
            if (visible)
            {
                HideHelpSilently();
            }

            _pausePanel.SetActive(visible);
            if (!visible)
            {
                CloseSettingsPanelSilently();
                HideConfirmation();
            }
        }

        public bool SettingsPanelVisible => _settingsPanel != null && _settingsPanel.activeSelf;

        public bool ConfirmationVisible =>
            _confirmationPanel != null && _confirmationPanel.activeSelf;

        /// <summary>
        /// Shows the reusable full-screen confirmation modal. The cancel
        /// button receives the default focus so accidental confirms are less
        /// likely on touch devices.
        /// </summary>
        public void ShowConfirmation(
            string title,
            string body,
            string confirmLabel,
            System.Action onConfirm,
            System.Action onCancel = null)
        {
            if (_confirmationPanel == null)
            {
                onConfirm?.Invoke();
                return;
            }

            _confirmationTitle.text = title ?? string.Empty;
            _confirmationBody.text = body ?? string.Empty;
            _confirmationConfirmLabel.text = confirmLabel ?? string.Empty;
            _confirmationCancelLabel.text = Localization.Get("confirm.cancel");
            _confirmationConfirmAction = onConfirm;
            _confirmationCancelAction = onCancel;
            _confirmationPanel.SetActive(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_confirmationCancelButton.gameObject);
            }

            UiFactory.PlayUiCue?.Invoke(GameAudioCue.UiOpen);
        }

        public void HideConfirmation()
        {
            if (_confirmationPanel == null || !_confirmationPanel.activeSelf)
            {
                return;
            }

            _confirmationPanel.SetActive(false);
            _confirmationConfirmAction = null;
            _confirmationCancelAction = null;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void ConfirmConfirmation()
        {
            var action = _confirmationConfirmAction;
            HideConfirmation();
            action?.Invoke();
        }

        private void CancelConfirmation()
        {
            var action = _confirmationCancelAction;
            HideConfirmation();
            UiFactory.PlayUiCue?.Invoke(GameAudioCue.UiClose);
            action?.Invoke();
        }

        /// <summary>
        /// Shows the modal first-run help panel above the current menu. Closing
        /// it reveals whichever panel was underneath: main menu, pause menu, or
        /// settings.
        /// </summary>
        public void ShowHelp()
        {
            if (_helpPanel == null)
            {
                return;
            }

            _helpPanel.transform.SetAsLastSibling();
            _helpPanel.SetActive(true);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_helpCloseButton.gameObject);
            }

            UiFactory.PlayUiCue?.Invoke(GameAudioCue.UiOpen);
        }

        /// <summary>
        /// Dismisses the help panel and records the device-local seen flag.
        /// </summary>
        public void HideHelp()
        {
            if (_helpPanel == null || !_helpPanel.activeSelf)
            {
                return;
            }

            _helpPanel.SetActive(false);
            GameSettings.MarkHelpSeen();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            UiFactory.PlayUiCue?.Invoke(GameAudioCue.UiClose);
        }

        private void HideHelpSilently()
        {
            if (_helpPanel != null)
            {
                _helpPanel.SetActive(false);
            }
        }

        public bool HelpVisible => _helpPanel != null && _helpPanel.activeSelf;

        public void ShowSettingsPanel()
        {
            HideHelpSilently();
            RefreshSettingsValues();
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
            }

            UiFactory.PlayUiCue?.Invoke(GameAudioCue.UiOpen);
        }

        public void HideSettingsPanel()
        {
            if (_settingsPanel == null || !_settingsPanel.activeSelf)
            {
                return;
            }

            _settingsPanel.SetActive(false);
            UiFactory.PlayUiCue?.Invoke(GameAudioCue.UiClose);
        }

        private void CloseSettingsPanelSilently()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }

        public Button SettingsButtonMainForTests => _settingsButtonMain;

        public Button SettingsButtonPauseForTests => _settingsButtonPause;

        public Button HelpButtonMainForTests => _helpButtonMain;

        public Button HelpButtonSettingsForTests => _helpButtonSettings;

        public Button HelpCloseButtonForTests => _helpCloseButton;

        public string HelpTitleForTests => _helpTitle?.text ?? string.Empty;

        public string HelpBodyForTests => _helpBody?.text ?? string.Empty;

        public Button SettingsCloseButtonForTests => _settingsCloseButton;

        public Button SettingsResetButtonForTests => _settingsResetButton;

        public Button SettingsMuteButtonForTests => _settingsMuteButton;

        public Button SettingsMasterMinusButtonForTests => _settingsMasterMinusButton;

        public Button SettingsMasterPlusButtonForTests => _settingsMasterPlusButton;

        public Button SettingsEffectsMinusButtonForTests => _settingsEffectsMinusButton;

        public Button SettingsEffectsPlusButtonForTests => _settingsEffectsPlusButton;

        public Button SettingsLanguageSystemButtonForTests => _settingsLanguageSystemButton;

        public Button SettingsLanguageEnglishButtonForTests => _settingsLanguageEnglishButton;

        public Button SettingsLanguageSpanishButtonForTests => _settingsLanguageSpanishButton;

        public Button SettingsLeftHandedButtonForTests => _settingsLeftHandedButton;

        public Button SettingsJoystickSizeMinusButtonForTests => _settingsJoystickSizeMinusButton;

        public Button SettingsJoystickSizePlusButtonForTests => _settingsJoystickSizePlusButton;

        public Button SettingsJoystickOpacityMinusButtonForTests => _settingsJoystickOpacityMinusButton;

        public Button SettingsJoystickOpacityPlusButtonForTests => _settingsJoystickOpacityPlusButton;

        public Button SettingsAimAssistButtonForTests => _settingsAimAssistButton;

        public Button SettingsAutoFireButtonForTests => _settingsAutoFireButton;

        public Button SettingsReducedMotionButtonForTests => _settingsReducedMotionButton;

        public Button SettingsHapticsButtonForTests => _settingsHapticsButton;

        public Button SettingsColorblindButtonForTests => _settingsColorblindButton;

        public string SettingsMuteLabelForTests => _settingsMuteLabel?.text ?? string.Empty;

        public string SettingsMasterLabelForTests => _settingsMasterLabel?.text ?? string.Empty;

        public string SettingsEffectsLabelForTests => _settingsEffectsLabel?.text ?? string.Empty;

        public string SettingsLanguageCurrentLabelForTests =>
            _settingsLanguageCurrentLabel?.text ?? string.Empty;

        public string SettingsLeftHandedLabelForTests => _settingsLeftHandedLabel?.text ?? string.Empty;

        public string SettingsJoystickSizeLabelForTests => _settingsJoystickSizeLabel?.text ?? string.Empty;

        public string SettingsJoystickOpacityLabelForTests =>
            _settingsJoystickOpacityLabel?.text ?? string.Empty;

        public string SettingsAimAssistLabelForTests => _settingsAimAssistLabel?.text ?? string.Empty;

        public string SettingsAutoFireLabelForTests => _settingsAutoFireLabel?.text ?? string.Empty;

        public string SettingsReducedMotionLabelForTests =>
            _settingsReducedMotionLabel?.text ?? string.Empty;

        public string SettingsHapticsLabelForTests => _settingsHapticsLabel?.text ?? string.Empty;

        public string SettingsColorblindLabelForTests =>
            _settingsColorblindLabel?.text ?? string.Empty;

        public bool ConfirmationVisibleForTests => ConfirmationVisible;

        public string ConfirmationTitleForTests => _confirmationTitle?.text ?? string.Empty;

        public string ConfirmationBodyForTests => _confirmationBody?.text ?? string.Empty;

        public string ConfirmationConfirmLabelForTests =>
            _confirmationConfirmLabel?.text ?? string.Empty;

        public string ConfirmationCancelLabelForTests =>
            _confirmationCancelLabel?.text ?? string.Empty;

        public Button ConfirmationConfirmButtonForTests => _confirmationConfirmButton;

        public Button ConfirmationCancelButtonForTests => _confirmationCancelButton;

        public Button CharacterRespecButtonForTests => _characterRespecButton;

        public Button PauseMenuButtonForTests => _pauseMenuButton;

        public bool PausePanelVisibleForTests => _pausePanel != null && _pausePanel.activeSelf;

        public Button FireButtonForTests => _fireButton;

        public bool FireButtonVisibleForTests => _fireButton != null && _fireButton.gameObject.activeSelf;

        public bool IsTouchOverFireButton(Vector2 screenPoint)
        {
            if (_fireButton == null || !_fireButton.gameObject.activeInHierarchy)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(
                _fireButton.GetComponent<RectTransform>(),
                screenPoint,
                null);
        }

        public Vector2 FireButtonScreenCenterForTests
        {
            get
            {
                if (_fireButton == null)
                {
                    return Vector2.zero;
                }

                return RectTransformUtility.WorldToScreenPoint(
                    null,
                    _fireButton.GetComponent<RectTransform>().position);
            }
        }

        public Button ReloadButtonForTests => _reloadButton;

        public Button SwitchWeaponButtonForTests => _switchWeaponButton;

        public Button PauseButtonForTests => _pauseButton;

        public Vector2 MoveStickAnchorForTests => _moveBase != null ? _moveBase.anchorMin : Vector2.zero;

        public Vector2 AimStickAnchorForTests => _aimBase != null ? _aimBase.anchorMin : Vector2.zero;

        public Vector2 MoveStickPositionForTests =>
            _moveBase != null ? _moveBase.anchoredPosition : Vector2.zero;

        public Vector2 AimStickPositionForTests =>
            _aimBase != null ? _aimBase.anchoredPosition : Vector2.zero;

        public float MoveStickSizeForTests => _moveBase != null ? _moveBase.sizeDelta.x : 0f;

        public float AimStickSizeForTests => _aimBase != null ? _aimBase.sizeDelta.x : 0f;

        public float MoveKnobSizeForTests => _moveKnob != null ? _moveKnob.sizeDelta.x : 0f;

        public float AimKnobSizeForTests => _aimKnob != null ? _aimKnob.sizeDelta.x : 0f;

        public Color MoveBaseColorForTests =>
            _moveBaseImage != null ? _moveBaseImage.color : Color.clear;

        public Color AimBaseColorForTests =>
            _aimBaseImage != null ? _aimBaseImage.color : Color.clear;

        public Color MoveKnobColorForTests =>
            _moveKnobImage != null ? _moveKnobImage.color : Color.clear;

        public Color AimKnobColorForTests =>
            _aimKnobImage != null ? _aimKnobImage.color : Color.clear;

        public Vector2 ReloadButtonPositionForTests =>
            _reloadButton != null ? _reloadButton.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;

        public Vector2 SwitchWeaponButtonPositionForTests =>
            _switchWeaponButton != null
                ? _switchWeaponButton.GetComponent<RectTransform>().anchoredPosition
                : Vector2.zero;

        public Vector2 PauseButtonPositionForTests =>
            _pauseButton != null ? _pauseButton.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;

        public Vector2 ActiveSkillButtonPositionForTests =>
            _activeSkillButton != null
                ? _activeSkillButton.GetComponent<RectTransform>().anchoredPosition
                : Vector2.zero;

        public Vector2 FireButtonPositionForTests =>
            _fireButton != null ? _fireButton.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;

        public string ControlsHintForTests => _controlsHint?.text ?? string.Empty;

        /// <summary>
        /// Test hook that presses or releases the manual fire control without
        /// synthesizing a touch.
        /// </summary>
        public void SetFireButtonPressedForTests(bool pressed)
        {
            _onFireHeld?.Invoke(pressed);
        }

        public void RefreshProgress(Profile profile)
        {
            if (_progressText == null || profile == null)
            {
                return;
            }

            _progressText.text = Localization.Format(
                "menu.progress",
                profile.Supplies,
                profile.Level,
                profile.HighestRound);
        }

        private void SelectLocale(string locale)
        {
            _onLocaleSelected?.Invoke(locale);
        }

        private void ToggleLeftHanded()
        {
            GameSettings.SetLeftHanded(!GameSettings.LeftHanded);
            RefreshSettingsValues();
            ApplyControlLayout();
        }

        private void AdjustJoystickSize(float delta)
        {
            GameSettings.SetJoystickSize(GameSettings.JoystickSize + delta);
            RefreshSettingsValues();
            ApplyControlLayout();
        }

        private void AdjustJoystickOpacity(float delta)
        {
            GameSettings.SetJoystickOpacity(GameSettings.JoystickOpacity + delta);
            RefreshSettingsValues();
            ApplyControlLayout();
        }

        private void CycleAimAssist()
        {
            GameSettings.CycleAimAssist();
            RefreshSettingsValues();
            ApplyControlLayout();
        }

        private void ToggleAutoFire()
        {
            GameSettings.SetAutoFire(!GameSettings.AutoFire);
            RefreshSettingsValues();
            ApplyControlLayout();
        }

        private void ToggleReducedMotion()
        {
            GameSettings.SetReducedMotion(!GameSettings.ReducedMotion);
            RefreshSettingsValues();
        }

        private void ToggleHaptics()
        {
            GameSettings.SetHaptics(!GameSettings.Haptics);
            RefreshSettingsValues();
        }

        private void ToggleColorblindSafeTelegraphs()
        {
            GameSettings.SetColorblindSafeTelegraphs(!GameSettings.ColorblindSafeTelegraphs);
            RefreshSettingsValues();
        }

        private void ResetSettings()
        {
            _onResetSettings?.Invoke();
            RefreshSettingsValues();
            ApplyControlLayout();
        }

        /// <summary>
        /// Applies the full audio state and refreshes every settings label.
        /// </summary>
        public void SetAudio(bool muted, float masterVolume, float effectsVolume)
        {
            _muted = muted;
            _masterVolume = Mathf.Clamp01(masterVolume);
            _effectsVolume = Mathf.Clamp01(effectsVolume);
            RefreshSettingsValues();
        }

        /// <summary>
        /// Refreshes every dynamic settings value for the active locale.
        /// </summary>
        public void RefreshSettingsValues()
        {
            var onLabel = Localization.Get("settings.value.on");
            var offLabel = Localization.Get("settings.value.off");

            if (_settingsMuteLabel != null)
            {
                _settingsMuteLabel.text = _muted ? offLabel : onLabel;
            }

            if (_settingsMasterLabel != null)
            {
                _settingsMasterLabel.text = Localization.Format(
                    "settings.value.percent",
                    Mathf.RoundToInt(_masterVolume * 100f));
            }

            if (_settingsEffectsLabel != null)
            {
                _settingsEffectsLabel.text = Localization.Format(
                    "settings.value.percent",
                    Mathf.RoundToInt(_effectsVolume * 100f));
            }

            if (_settingsLanguageCurrentLabel != null)
            {
                var locale = Localization.CurrentLocale == Localization.PseudoLocale
                    ? Localization.DefaultLocale
                    : Localization.CurrentLocale;
                _settingsLanguageCurrentLabel.text = Localization.GetDisplayName(locale);
            }

            if (_settingsLeftHandedLabel != null)
            {
                _settingsLeftHandedLabel.text = GameSettings.LeftHanded ? onLabel : offLabel;
            }

            if (_settingsJoystickSizeLabel != null)
            {
                _settingsJoystickSizeLabel.text = Localization.Format(
                    "settings.value.size",
                    GameSettings.JoystickSize.ToString("0.0", Localization.DisplayCulture));
            }

            if (_settingsJoystickOpacityLabel != null)
            {
                _settingsJoystickOpacityLabel.text = Localization.Format(
                    "settings.value.percent",
                    Mathf.RoundToInt(GameSettings.JoystickOpacity * 100f));
            }

            if (_settingsAimAssistLabel != null)
            {
                _settingsAimAssistLabel.text = Localization.Get(AimAssistKey(GameSettings.AimAssist));
            }

            if (_settingsAutoFireLabel != null)
            {
                _settingsAutoFireLabel.text = GameSettings.AutoFire ? onLabel : offLabel;
            }

            if (_settingsReducedMotionLabel != null)
            {
                _settingsReducedMotionLabel.text = GameSettings.ReducedMotion ? onLabel : offLabel;
            }

            if (_settingsHapticsLabel != null)
            {
                _settingsHapticsLabel.text = GameSettings.Haptics ? onLabel : offLabel;
            }

            if (_settingsColorblindLabel != null)
            {
                _settingsColorblindLabel.text = GameSettings.ColorblindSafeTelegraphs
                    ? onLabel
                    : offLabel;
            }
        }

        private static string AimAssistKey(AimAssistLevel level)
        {
            return level switch
            {
                AimAssistLevel.Low => "settings.aim.low",
                AimAssistLevel.High => "settings.aim.high",
                _ => "settings.aim.off"
            };
        }

        public string MainMenuStartLabelForTests =>
            FindGameObject(_mainMenu, "Start")?.GetComponentInChildren<Text>()?.text ?? string.Empty;

        public string MainMenuSubtitleForTests => FindText(_mainMenu, "Subtitle")?.text ?? string.Empty;

        public string PauseTitleForTests => FindText(_pausePanel, "Title")?.text ?? string.Empty;

        public string HudReloadLabelForTests =>
            FindGameObject(_hud, "Reload Button")?.GetComponentInChildren<Text>()?.text ?? string.Empty;

        public string HudRoundLabelForTests => _roundText?.text ?? string.Empty;

        public string GameOverTitleForTests => FindText(_gameOverPanel, "Title")?.text ?? string.Empty;

        public string VictoryTitleForTests => FindText(_victoryPanel, "Title")?.text ?? string.Empty;

        public string RoundEndHintForTests => FindText(_roundEndPanel, "Hint")?.text ?? string.Empty;

        public string[] RoundEndRewardLabelsForTests
        {
            get
            {
                var labels = new string[_rewardLabels.Length];
                for (var i = 0; i < _rewardLabels.Length; i++)
                {
                    labels[i] = _rewardLabels[i].text;
                }

                return labels;
            }
        }

        public Sprite HudWeaponIconForTests => _weaponIcon != null ? _weaponIcon.sprite : null;

        public Sprite ActiveSkillIconForTests => _activeSkillIcon != null ? _activeSkillIcon.sprite : null;

        public Sprite RewardIconForTests(int index)
        {
            return index >= 0 && index < _rewardIcons.Length && _rewardIcons[index] != null
                ? _rewardIcons[index].sprite
                : null;
        }

        public bool ClickArmoryWeaponActionForTests(WeaponId weaponId)
        {
            for (var i = 0; i < _armoryRows.Length; i++)
            {
                if (_armoryRows[i].WeaponId == weaponId && _armoryRows[i].Button != null)
                {
                    _armoryRows[i].Button.onClick.Invoke();
                    return true;
                }
            }

            return false;
        }

        public bool ClickArmoryUpgradeForTests(WeaponUpgradeStat stat)
        {
            for (var i = 0; i < _armoryUpgradeRows.Length; i++)
            {
                if (_armoryUpgradeRows[i].Stat == stat && _armoryUpgradeRows[i].Button != null)
                {
                    _armoryUpgradeRows[i].Button.onClick.Invoke();
                    return true;
                }
            }

            return false;
        }

        public Sprite ArmoryWeaponIconForTests(WeaponId weaponId)
        {
            for (var i = 0; i < _armoryRows.Length; i++)
            {
                if (_armoryRows[i].WeaponId == weaponId && _armoryRows[i].Icon != null)
                {
                    return _armoryRows[i].Icon.sprite;
                }
            }

            return null;
        }

        public Sprite SelectActiveSkillIconForTests(SkillNode node)
        {
            var index = node switch
            {
                SkillNode.Shockwave => 0,
                SkillNode.Dash => 1,
                SkillNode.Turret => 2,
                _ => -1
            };

            return index >= 0 && index < _selectActiveIcons.Length && _selectActiveIcons[index] != null
                ? _selectActiveIcons[index].sprite
                : null;
        }

        public string BuildLabelForTests => _buildLabel?.text ?? string.Empty;


        public string[] TextOverflowsForTests()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in _safeArea.GetComponentsInChildren<Text>(false))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);
            }

            return UiLayoutAudit.FindOverflows(FindTopmostActiveTextContainer());
        }

        /// <summary>
        /// Test hook that lays the UI out against the 1920x1080 reference canvas
        /// instead of the small batch-mode test screen.
        /// </summary>
        public void UseDesignLayoutForTests()
        {
            UseDesignLayoutForTests(new Vector2(1920f, 1080f));
        }

        /// <summary>
        /// Test hook that lays the UI out against a specific design resolution.
        /// </summary>
        public void UseDesignLayoutForTests(Vector2 designSize)
        {
            var canvas = _safeArea.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.enabled = false;
            }

            foreach (var fitter in canvas.GetComponentsInChildren<SafeAreaFitter>(true))
            {
                fitter.enabled = false;
            }

            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = designSize;
            _safeArea.anchorMin = Vector2.zero;
            _safeArea.anchorMax = Vector2.one;
            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        }

        private RectTransform FindTopmostActiveTextContainer()
        {
            RectTransform topmost = null;
            var highestIndex = -1;
            foreach (Transform child in _safeArea)
            {
                if (!child.gameObject.activeSelf ||
                    child.GetComponentInChildren<Text>(false) == null)
                {
                    continue;
                }

                var index = child.GetSiblingIndex();
                if (index > highestIndex)
                {
                    highestIndex = index;
                    topmost = child as RectTransform;
                }
            }

            return topmost ?? _safeArea;
        }

        public void ShowGameOver(int round, int kills, int suppliesEarned)
        {
            HideHelpSilently();
            _mainMenu.SetActive(false);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
            CloseSettingsPanelSilently();
            HideConfirmation();
            _gameOverPanel.SetActive(true);
            HideVictory();
            HideBoss();
            HideRoundEnd();
            HideArmory();
            HideCharacterSheet();
            _gameOverSummary.text = Localization.Format("gameover.summary", round, kills, suppliesEarned);
        }

        public void ShowVictory(int round, int kills, int suppliesEarned, int xpEarned)
        {
            HideHelpSilently();
            _mainMenu.SetActive(false);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
            CloseSettingsPanelSilently();
            HideConfirmation();
            _gameOverPanel.SetActive(false);
            HideBoss();
            HideRoundEnd();
            HideArmory();
            HideCharacterSheet();
            _victorySummary.text = Localization.Format("victory.summary", round, kills, suppliesEarned, xpEarned);
            _victoryPanel.SetActive(true);
        }

        public bool VictoryVisible => _victoryPanel != null && _victoryPanel.activeSelf;

        public void HideVictory()
        {
            if (_victoryPanel != null)
            {
                _victoryPanel.SetActive(false);
            }
        }

        public bool BossVisible => _bossPanel != null && _bossPanel.activeSelf;

        public void ShowBoss(string name)
        {
            if (_bossPanel == null)
            {
                return;
            }

            _bossNameText.text = name;
            _lastBossHealth = int.MinValue;
            _lastBossMaxHealth = int.MinValue;
            _lastBossEnraged = false;
            _bossPanel.SetActive(true);
        }

        public void UpdateBoss(float health, float maxHealth, bool enraged)
        {
            if (_bossHealthFill == null || _bossHealthText == null)
            {
                return;
            }

            var roundedHealth = Mathf.CeilToInt(health);
            var roundedMaxHealth = Mathf.CeilToInt(maxHealth);
            if (roundedHealth == _lastBossHealth &&
                roundedMaxHealth == _lastBossMaxHealth &&
                enraged == _lastBossEnraged)
            {
                return;
            }

            _lastBossHealth = roundedHealth;
            _lastBossMaxHealth = roundedMaxHealth;
            _lastBossEnraged = enraged;
            var fraction = maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
            _bossHealthFill.fillAmount = fraction;
            _bossHealthFill.color = enraged
                ? new Color(1f, 0.28f, 0.22f)
                : new Color(0.92f, 0.35f, 0.72f);
            _bossHealthText.text = Localization.Format("hud.boss.health", roundedHealth, roundedMaxHealth);
        }

        public void HideBoss()
        {
            if (_bossPanel != null)
            {
                _bossPanel.SetActive(false);
            }
        }

        public bool ArmoryVisible => _armoryPanel != null && _armoryPanel.activeSelf;

        public bool HudVisible => _hud != null && _hud.activeSelf;

        public void ShowArmory(Profile profile, WeaponId displayWeapon)
        {
            HideHelpSilently();
            _mainMenu.SetActive(false);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
            CloseSettingsPanelSilently();
            HideConfirmation();
            _gameOverPanel.SetActive(false);
            HideVictory();
            HideRoundEnd();
            HideCharacterSheet();
            _armoryPanel.SetActive(true);
            _armoryWeaponsPanel.SetActive(true);
            _armoryArmorPanel.SetActive(false);
            RefreshArmory(profile, displayWeapon);
            SetArmoryStatus(string.Empty);
        }

        public void HideArmory()
        {
            if (_armoryPanel != null)
            {
                _armoryPanel.SetActive(false);
            }
        }

        public bool CharacterSheetVisible => _characterSheet != null && _characterSheet.activeSelf;

        public bool SynergyTextVisible => _characterSynergyText != null &&
                                          !string.IsNullOrEmpty(_characterSynergyText.text);

        public string SynergyTextForTests => _characterSynergyText?.text ?? string.Empty;

        public bool LevelUpVisible => _levelUpText != null && _levelUpText.gameObject.activeSelf;

        public void ShowCharacterSheet(Profile profile, bool canAllocate, bool canRespec, WeaponStats weapon, ArmorStats armor)
        {
            HideHelpSilently();
            CloseSettingsPanelSilently();
            _characterSheetTablet = IsTabletLayout();
            _characterSheet.SetActive(true);
            ApplyCharacterSheetLayout();
            SetActiveCharacterTab(_activeCharacterBranch);
            RefreshCharacterSheet(profile, canAllocate, canRespec, weapon, armor);
            SetCharacterSheetStatus(
                canAllocate ? string.Empty : Localization.Get("character.allocation.blocked"));
        }

        public void ShowAccount()
        {
            HideHelpSilently();
            CloseSettingsPanelSilently();
            HideConfirmation();
            _accountPanel.SetActive(true);
            _accountStatusText.text = string.Empty;
        }

        public void HideAccount()
        {
            _accountPanel.SetActive(false);
        }

        public void SetAccountState(AccountState state, string username)
        {
            var signedIn = state == AccountState.SignedIn;
            var stateKey = state switch
            {
                AccountState.SigningIn => "account.state.signingin",
                AccountState.SignedIn => "account.state.signedin",
                AccountState.Offline => "account.state.offline",
                _ => "account.state.signedout"
            };
            var stateText = signedIn
                ? Localization.Format(stateKey, username)
                : Localization.Get(stateKey);

            if (_mainMenuAccountState != null)
            {
                _mainMenuAccountState.text = stateText;
            }

            if (_accountStateText != null)
            {
                _accountStateText.text = stateText;
            }

            var showCredentials = !signedIn;
            _accountUsername.gameObject.SetActive(showCredentials);
            _accountPassword.gameObject.SetActive(showCredentials);
            _accountRegisterButton.gameObject.SetActive(showCredentials);
            _accountLoginButton.gameObject.SetActive(showCredentials);
            _accountLogoutButton.gameObject.SetActive(signedIn);
            if (!signedIn)
            {
                SetCloudControls(false, false);
                HideCloudConflict();
            }
        }

        public void ShowLeaderboard()
        {
            HideHelpSilently();
            CloseSettingsPanelSilently();
            HideConfirmation();
            _leaderboardPanel.SetActive(true);
        }

        public void HideLeaderboard()
        {
            _leaderboardPanel.SetActive(false);
        }

        public void SetLeaderboardStatus(string message)
        {
            _leaderboardStatusText.text = message ?? string.Empty;
        }

        public void SetLeaderboardEntries(LeaderboardEntryDto[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                _leaderboardText.text = string.Empty;
                SetLeaderboardStatus(Localization.Get("leaderboard.empty"));
                return;
            }

            var builder = new StringBuilder();
            foreach (var entry in entries)
            {
                builder.Append(
                        Localization.Format(
                            "leaderboard.row",
                            entry.rank,
                            entry.username,
                            entry.highestRound,
                            entry.totalKills,
                            entry.durationSeconds))
                    .AppendLine();
            }

            _leaderboardText.text = builder.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_leaderboardText.rectTransform);
            SetLeaderboardStatus(string.Empty);
        }

        public void SetTelemetry(bool available, bool enabled)
        {
            _telemetryButton.gameObject.SetActive(available);
            _telemetryLabel.text = Localization.Get(enabled ? "telemetry.on" : "telemetry.off");
        }

        public bool LeaderboardVisibleForTests => _leaderboardPanel != null && _leaderboardPanel.activeSelf;

        public bool TelemetryVisibleForTests => _telemetryButton != null && _telemetryButton.gameObject.activeSelf;

        public string LeaderboardStatusForTests => _leaderboardStatusText?.text ?? string.Empty;

        public string LeaderboardTextForTests => _leaderboardText?.text ?? string.Empty;

        public bool LeaderboardScrollableForTests =>
            _leaderboardViewport != null &&
            _leaderboardText.rectTransform.rect.height > _leaderboardViewport.rect.height;
        public void SetCloudState(CloudSyncState state, string message)
        {
            if (_cloudStatusText == null)
            {
                return;
            }

            _cloudStatusText.text = string.IsNullOrWhiteSpace(message)
                ? Localization.Get(CloudStateKey(state))
                : message;
        }

        public void SetCloudControls(bool signedIn, bool enabled)
        {
            var visible = signedIn && enabled;
            _cloudBackupButton.gameObject.SetActive(visible);
            _cloudRestoreButton.gameObject.SetActive(visible);
            if (!enabled)
            {
                SetCloudState(CloudSyncState.Disabled, null);
            }
            else if (!signedIn)
            {
                SetCloudState(CloudSyncState.SignedOut, null);
            }
        }

        public void ShowCloudConflict(string details)
        {
            _cloudConflictText.text = details;
            _cloudConflictPanel.SetActive(true);
        }

        public void HideCloudConflict()
        {
            _cloudConflictPanel.SetActive(false);
        }

        public string CloudStatusForTests => _cloudStatusText?.text ?? string.Empty;

        public bool CloudConflictVisibleForTests => _cloudConflictPanel != null && _cloudConflictPanel.activeSelf;

        private static string CloudStateKey(CloudSyncState state)
        {
            return state switch
            {
                CloudSyncState.Disabled => "cloud.disabled",
                CloudSyncState.SignedOut => "cloud.signedout",
                CloudSyncState.Syncing => "cloud.syncing",
                CloudSyncState.Synced => "cloud.synced",
                CloudSyncState.Conflict => "cloud.conflict",
                CloudSyncState.Offline => "cloud.offline",
                _ => "cloud.idle"
            };
        }
        public void SetAccountBusy(bool busy)
        {
            _accountRegisterButton.interactable = !busy;
            _accountLoginButton.interactable = !busy;
            _accountLogoutButton.interactable = !busy;
        }

        public void SetAccountStatus(string message)
        {
            _accountStatusText.text = message ?? string.Empty;
        }

        public string AccountStatusForTests => _accountStatusText.text;

        public bool AccountPanelVisibleForTests => _accountPanel.activeSelf;

        public string MainMenuAccountStateForTests => _mainMenuAccountState?.text ?? string.Empty;

        public void HideCharacterSheet()
        {
            if (_characterSheet != null)
            {
                _characterSheet.SetActive(false);
            }

            HideConfirmation();
        }

        public void RefreshCharacterSheet(Profile profile, bool canAllocate, bool canRespec, WeaponStats weapon, ArmorStats armor)
        {
            var stats = StatAggregation.Compute(profile, weapon, armor);
            _characterLevelText.text = Localization.Format(
                "character.level",
                profile.Level,
                profile.Xp,
                Profile.XpToNext(profile.Level));
            _characterPointsText.text = Localization.Format("character.points", profile.SkillPoints);
            _characterStatsText.text = FormatCharacterStats(stats);
            _characterSynergyText.text = FormatSynergies(profile);

            for (var i = 0; i < _characterNodeRows.Length; i++)
            {
                var row = _characterNodeRows[i];
                var definition = SkillCatalog.Get(row.Node);
                var rank = profile.GetSkillRank(row.Node);

                row.Name.text = Localization.SkillName(row.Node);
                row.Rank.text = Localization.Format("character.rank", rank, definition.MaxRanks);
                row.Pips.text = RankPips(rank, definition.MaxRanks);
                row.Preview.text = FormatNodePreview(profile, row.Node, rank, stats, weapon, armor);
                row.Allocate.interactable = canAllocate && profile.CanAllocateSkill(row.Node);
                row.AllocateLabel.text = rank >= definition.MaxRanks
                    ? Localization.Get("character.max")
                    : Localization.Get("character.allocate");
            }

            _characterRespecButton.interactable = canRespec && profile.GetSpentSkillPoints() > 0;
            _characterRespecLabel.text = Localization.Get("character.respec");

            var selectNodes = new[] { SkillNode.Shockwave, SkillNode.Dash, SkillNode.Turret };
            for (var i = 0; i < _selectActiveButtons.Length; i++)
            {
                var node = selectNodes[i];
                var rank = profile.GetSkillRank(node);
                var selected = profile.ActiveSkill == node;
                var name = Localization.SkillName(node);
                _selectActiveLabels[i].text = selected
                    ? Localization.Format("character.active.selected", name)
                    : name;
                _selectActiveLabels[i].color = selected
                    ? new Color(0.45f, 0.95f, 0.7f)
                    : Color.white;
                _selectActiveButtons[i].interactable = (canAllocate || canRespec) && rank > 0 && !selected;
            }
        }

        public void SetCharacterSheetStatus(string status)
        {
            _characterStatusText.text = status;
        }

        public void SetActiveCharacterTab(int index)
        {
            _activeCharacterBranch = Mathf.Clamp(index, 0, _characterBranchPanels.Length - 1);
            for (var i = 0; i < _characterTabLabels.Length; i++)
            {
                _characterTabLabels[i].color = i == _activeCharacterBranch
                    ? new Color(0.45f, 0.95f, 0.7f)
                    : new Color(0.7f, 0.8f, 0.9f);
                if (!_characterSheetTablet)
                {
                    _characterBranchPanels[i].SetActive(i == _activeCharacterBranch);
                }
            }
        }

        private void ApplyCharacterSheetLayout()
        {
            for (var i = 0; i < _characterBranchPanels.Length; i++)
            {
                var rect = _characterBranchPanels[i].GetComponent<RectTransform>();
                _characterTabs[i].gameObject.SetActive(!_characterSheetTablet);
                _characterBranchPanels[i].SetActive(_characterSheetTablet || i == _activeCharacterBranch);
                var branchTitle = _characterBranchPanels[i].transform.Find("Branch Title");
                if (branchTitle != null)
                {
                    branchTitle.gameObject.SetActive(_characterSheetTablet);
                }

                if (_characterSheetTablet)
                {
                    rect.anchorMin = new Vector2(0.02f + (i * 0.32f), 0.17f);
                    rect.anchorMax = new Vector2(0.30f + (i * 0.32f), 0.78f);
                }
                else
                {
                    rect.anchorMin = new Vector2(0.04f, 0.17f);
                    rect.anchorMax = new Vector2(0.96f, 0.78f);
                }

                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private static bool IsTabletLayout()
        {
            return Screen.width >= 1200 &&
                   Screen.height >= 700 &&
                   (Screen.width / (float)Mathf.Max(1, Screen.height)) < 1.7f;
        }

        private static string RankPips(int rank, int maxRank)
        {
            return new string('\u25CF', rank) + new string('\u25CB', Mathf.Max(0, maxRank - rank));
        }

        private static string FormatPercent(float value)
        {
            return (value * 100f).ToString("0'%'", CultureInfo.InvariantCulture);
        }

        private static string FormatSynergies(Profile profile)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < SynergyCatalog.All.Count; i++)
            {
                var definition = SynergyCatalog.All[i];
                if (i > 0)
                {
                    builder.Append("   ");
                }

                var name = Localization.Get(SynergyNameKey(definition.Id));
                if (SynergyCatalog.IsUnlocked(profile, definition.Id))
                {
                    builder.Append(Localization.Format("synergy.active", name));
                }
                else
                {
                    builder.Append(
                        Localization.Format(
                            "synergy.inactive",
                            name,
                            Localization.SkillName(definition.NodeA),
                            profile.GetSkillRank(definition.NodeA),
                            definition.RankA,
                            Localization.SkillName(definition.NodeB),
                            profile.GetSkillRank(definition.NodeB),
                            definition.RankB));
                }
            }

            return builder.ToString();
        }

        private static string SynergyNameKey(SynergyId id)
        {
            return id switch
            {
                SynergyId.Ghost => "synergy.ghost.name",
                SynergyId.Bastion => "synergy.bastion.name",
                _ => "synergy.berserker.name"
            };
        }

        private static string FormatCharacterStats(CharacterStats stats)        {
            return Localization.Format(
                "character.stats",
                stats.MaxHealth.ToString("0", CultureInfo.InvariantCulture),
                stats.WeaponDamageMultiplier.ToString("0.00", CultureInfo.InvariantCulture),
                stats.MoveSpeed.ToString("0.00", CultureInfo.InvariantCulture),
                stats.Armor,
                FormatPercent(stats.CriticalChance),
                FormatPercent(stats.HeadshotBonus),
                stats.ReloadSpeedMultiplier.ToString("0.00", CultureInfo.InvariantCulture),
                stats.AmmoCapacityMultiplier.ToString("0.00", CultureInfo.InvariantCulture));
        }

        private static string FormatNodePreview(
            Profile profile,
            SkillNode node,
            int rank,
            CharacterStats current,
            WeaponStats weapon,
            ArmorStats armor)
        {
            var definition = SkillCatalog.Get(node);
            if (rank >= definition.MaxRanks)
            {
                return Localization.Get("character.max");
            }

            switch (node)
            {
                case SkillNode.Strength:
                {
                    var next = StatAggregation.PreviewAddRank(profile, node, weapon, armor);
                    return Localization.Format(
                        "skill.strength.preview",
                        current.WeaponDamageMultiplier.ToString("0.00", CultureInfo.InvariantCulture),
                        next.WeaponDamageMultiplier.ToString("0.00", CultureInfo.InvariantCulture));
                }

                case SkillNode.Vitality:
                {
                    var next = StatAggregation.PreviewAddRank(profile, node, weapon, armor);
                    return Localization.Format(
                        "skill.vitality.preview",
                        current.MaxHealth.ToString("0", CultureInfo.InvariantCulture),
                        next.MaxHealth.ToString("0", CultureInfo.InvariantCulture));
                }

                case SkillNode.Speed:
                {
                    var next = StatAggregation.PreviewAddRank(profile, node, weapon, armor);
                    return Localization.Format(
                        "skill.speed.preview",
                        current.MoveSpeed.ToString("0.00", CultureInfo.InvariantCulture),
                        next.MoveSpeed.ToString("0.00", CultureInfo.InvariantCulture));
                }

                case SkillNode.Precision:
                {
                    var next = StatAggregation.PreviewAddRank(profile, node, weapon, armor);
                    return Localization.Format(
                        "skill.precision.preview",
                        FormatPercent(current.CriticalChance),
                        FormatPercent(next.CriticalChance),
                        FormatPercent(current.HeadshotBonus),
                        FormatPercent(next.HeadshotBonus));
                }

                case SkillNode.Endurance:
                {
                    var next = StatAggregation.PreviewAddRank(profile, node, weapon, armor);
                    return Localization.Format("skill.endurance.preview", current.Armor, next.Armor);
                }

                case SkillNode.Engineering:
                {
                    var next = StatAggregation.PreviewAddRank(profile, node, weapon, armor);
                    return Localization.Format(
                        "skill.engineering.preview",
                        current.ReloadSpeedMultiplier.ToString("0.00", CultureInfo.InvariantCulture),
                        next.ReloadSpeedMultiplier.ToString("0.00", CultureInfo.InvariantCulture),
                        current.AmmoCapacityMultiplier.ToString("0.00", CultureInfo.InvariantCulture),
                        next.AmmoCapacityMultiplier.ToString("0.00", CultureInfo.InvariantCulture));
                }

                case SkillNode.Shockwave:
                {
                    var first = definition.GetActivationStats(rank);
                    var second = definition.GetActivationStats(rank + 1);
                    return Localization.Format(
                        "skill.shockwave.preview",
                        first.AreaDamage.ToString("0.#", CultureInfo.InvariantCulture),
                        second.AreaDamage.ToString("0.#", CultureInfo.InvariantCulture),
                        first.AreaRadius.ToString("0.00", CultureInfo.InvariantCulture),
                        second.AreaRadius.ToString("0.00", CultureInfo.InvariantCulture),
                        first.CooldownSeconds.ToString("0.#", CultureInfo.InvariantCulture),
                        second.CooldownSeconds.ToString("0.#", CultureInfo.InvariantCulture));
                }

                case SkillNode.Dash:
                {
                    var first = definition.GetActivationStats(rank);
                    var second = definition.GetActivationStats(rank + 1);
                    return Localization.Format(
                        "skill.dash.preview",
                        first.DashDistance.ToString("0.00", CultureInfo.InvariantCulture),
                        second.DashDistance.ToString("0.00", CultureInfo.InvariantCulture),
                        first.CooldownSeconds.ToString("0.#", CultureInfo.InvariantCulture),
                        second.CooldownSeconds.ToString("0.#", CultureInfo.InvariantCulture));
                }

                default:
                {
                    var first = definition.GetActivationStats(rank);
                    var second = definition.GetActivationStats(rank + 1);
                    return Localization.Format(
                        "skill.turret.preview",
                        first.TurretDamage.ToString("0.#", CultureInfo.InvariantCulture),
                        second.TurretDamage.ToString("0.#", CultureInfo.InvariantCulture),
                        first.TurretDuration.ToString("0.#", CultureInfo.InvariantCulture),
                        second.TurretDuration.ToString("0.#", CultureInfo.InvariantCulture),
                        first.CooldownSeconds.ToString("0.#", CultureInfo.InvariantCulture),
                        second.CooldownSeconds.ToString("0.#", CultureInfo.InvariantCulture));
                }
            }
        }

        public void RefreshArmory(Profile profile, WeaponId displayWeapon)
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
                Localization.WeaponName(displayWeapon));

            for (var i = 0; i < _armoryUpgradeRows.Length; i++)
            {
                var row = _armoryUpgradeRows[i];
                var track = WeaponUpgradeCatalog.Get(displayWeapon, row.Stat);
                var tier = profile.GetWeaponUpgradeTier(displayWeapon, row.Stat);
                row.Label.text = FormatUpgradeCard(track, tier);
                row.Button.interactable = tier < track.MaxTier;
            }

            for (var i = 0; i < _armoryArmorRows.Length; i++)
            {
                var row = _armoryArmorRows[i];
                var owned = profile.HasArmor(row.Tier);
                var equipped = profile.EquippedArmor == row.Tier;
                var definition = ArmorCatalog.Get(row.Tier);
                var stats = ArmorService.GetEffectiveStats(profile, row.Tier);

                row.Name.text = Localization.ArmorName(row.Tier);
                row.Name.color = equipped
                    ? new Color(0.45f, 0.95f, 0.7f)
                    : new Color(0.86f, 0.92f, 1f);
                row.Info.text = stats.MaxUpgradeTier > 0
                    ? Localization.Format("armory.armor.row", stats.AbsorbAmount, stats.UpgradeTier, stats.MaxUpgradeTier)
                    : Localization.Format("armory.armor.absorb", stats.AbsorbAmount);

                if (!owned)
                {
                    row.Label.text = definition.UnlockCost > 0
                        ? Localization.Format("armory.armor.unlock", definition.UnlockCost)
                        : Localization.Get("armory.armor.unlock.free");
                    row.Button.interactable = true;
                }
                else if (equipped)
                {
                    row.Label.text = Localization.Get("armory.armor.equipped");
                    row.Button.interactable = false;
                }
                else
                {
                    row.Label.text = Localization.Get("armory.armor.equip");
                    row.Button.interactable = true;
                }
            }

            var equippedArmor = ArmorService.GetEffectiveStats(profile, profile.EquippedArmor);
            var armorDefinition = ArmorCatalog.Get(profile.EquippedArmor);
            var armorPercent = (armorDefinition.UpgradeEffectPerTier * 100f).ToString(
                "0.#",
                CultureInfo.InvariantCulture);
            _armoryArmorUpgradeTitle.text = Localization.Format(
                "armory.armor.upgrade.title",
                Localization.ArmorName(profile.EquippedArmor));

            if (equippedArmor.MaxUpgradeTier <= 0)
            {
                _armoryArmorUpgradeLabel.text = Localization.Get("armory.armor.upgrade.none");
                _armoryArmorUpgradeButton.interactable = false;
            }
            else if (equippedArmor.IsMaxUpgrade)
            {
                _armoryArmorUpgradeLabel.text = Localization.Format(
                    "armory.armor.upgrade.max",
                    armorPercent,
                    equippedArmor.MaxUpgradeTier);
                _armoryArmorUpgradeButton.interactable = false;
            }
            else
            {
                _armoryArmorUpgradeLabel.text = Localization.Format(
                    "armory.armor.upgrade.card",
                    armorPercent,
                    equippedArmor.UpgradeTier,
                    equippedArmor.MaxUpgradeTier,
                    equippedArmor.NextUpgradeCost);
                _armoryArmorUpgradeButton.interactable = true;
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
                if (_rewardIcons[i] != null)
                {
                    _rewardIcons[i].sprite = ArtCatalog.Get(RewardIconKey(options[i].Type));
                }
            }

            _roundEndPanel.SetActive(true);
        }

        public void HideRoundEnd()
        {
            _roundEndPanel.SetActive(false);
        }

        private static string WeaponIconKey(WeaponId weaponId)
        {
            return weaponId switch
            {
                WeaponId.Smg => "UI/weapon_smg",
                WeaponId.Shotgun => "UI/weapon_shotgun",
                WeaponId.Rifle => "UI/weapon_rifle",
                WeaponId.Lmg => "UI/weapon_lmg",
                _ => "UI/weapon_pistol"
            };
        }

        private static string SkillIconKey(SkillNode node)
        {
            return node switch
            {
                SkillNode.Dash => "UI/skill_dash",
                SkillNode.Shockwave => "UI/skill_shockwave",
                _ => "UI/skill_turret"
            };
        }

        private static string RewardIconKey(RoundRewardType type)
        {
            return type switch
            {
                RoundRewardType.Supplies => "UI/reward_supplies",
                RoundRewardType.Xp => "UI/reward_xp",
                RoundRewardType.AmmoCrate => "UI/reward_ammo",
                _ => "UI/reward_armor"
            };
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
            if (_sticksInitialized &&
                move.Active == _lastMoveStick.Active &&
                move.Origin == _lastMoveStick.Origin &&
                move.Current == _lastMoveStick.Current &&
                aim.Active == _lastAimStick.Active &&
                aim.Origin == _lastAimStick.Origin &&
                aim.Current == _lastAimStick.Current)
            {
                return;
            }

            _sticksInitialized = true;
            _lastMoveStick = move;
            _lastAimStick = aim;
            UpdateStick(_moveBase, _moveKnob, _moveBaseImage, move, _moveStickOnRight);
            UpdateStick(_aimBase, _aimKnob, _aimBaseImage, aim, _aimStickOnRight);
        }

        private void UpdateStick(
            RectTransform baseRect,
            RectTransform knob,
            Image baseImage,
            StickState state,
            bool right)
        {
            var baseSize = StickBaseSize * _joystickScale;
            var edgePadding = (baseSize * 0.5f) + StickEdgeMargin;

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
                baseImage.color = new Color(0.45f, 0.9f, 1f, 0.34f * _joystickOpacity);
            }
            else
            {
                baseRect.anchorMin = new Vector2(right ? 1f : 0f, 0f);
                baseRect.anchorMax = new Vector2(right ? 1f : 0f, 0f);
                baseRect.anchoredPosition = new Vector2(right ? -edgePadding : edgePadding, edgePadding);
                knob.anchoredPosition = Vector2.zero;
                baseImage.color = new Color(0.7f, 0.82f, 0.9f, 0.15f * _joystickOpacity);
            }
        }

        private GameObject BuildMainMenu(
            System.Action onStart,
            System.Action onArmory,
            System.Action onCharacterSheet,
            System.Action onAccount,
            System.Action onLeaderboard,
            System.Action onTelemetryToggle,
            System.Action onSettings,
            System.Action onHelp,
            out Text accountStateText,
            out Button telemetryButton,
            out Text telemetryLabel,
            out Text buildLabel,
            out Button settingsButton,
            out Button helpButton)
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

            accountStateText = UiFactory.CreateText(root.transform, "Account State", string.Empty, 24, TextAnchor.MiddleCenter, new Color(0.72f, 0.78f, 0.9f));
            UiFactory.Place(accountStateText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 40f));

            var start = UiFactory.CreateButton(root.transform, "Start", Localization.Get("menu.start"), 38, onStart);
            UiFactory.Place(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0.41f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 96f));

            var armory = UiFactory.CreateButton(root.transform, "Armory", Localization.Get("menu.armory"), 32, onArmory);
            UiFactory.Place(armory.GetComponent<RectTransform>(), new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 76f));

            var character = UiFactory.CreateButton(root.transform, "Skills", Localization.Get("character.open"), 32, onCharacterSheet);
            UiFactory.Place(character.GetComponent<RectTransform>(), new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 76f));

            var leaderboard = UiFactory.CreateButton(root.transform, "Leaderboard", Localization.Get("leaderboard.open"), 26, onLeaderboard);
            UiFactory.Place(leaderboard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.13f), new Vector2(0.5f, 0.5f), new Vector2(-330f, 0f), new Vector2(300f, 76f));

            helpButton = UiFactory.CreateButton(root.transform, "How To Play", Localization.Get("help.open"), 24, onHelp);
            UiFactory.Place(helpButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.13f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 76f));

            var account = UiFactory.CreateButton(root.transform, "Account", Localization.Get("account.open"), 26, onAccount);
            UiFactory.Place(account.GetComponent<RectTransform>(), new Vector2(0.5f, 0.13f), new Vector2(0.5f, 0.5f), new Vector2(330f, 0f), new Vector2(300f, 76f));

            telemetryButton = UiFactory.CreateButton(
                root.transform,
                "Telemetry",
                Localization.Get("telemetry.off"),
                22,
                onTelemetryToggle);
            UiFactory.Place(telemetryButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -32f), new Vector2(360f, 56f));
            telemetryLabel = telemetryButton.GetComponentInChildren<Text>();
            telemetryButton.gameObject.SetActive(false);

            settingsButton = UiFactory.CreateButton(
                root.transform,
                "Settings",
                Localization.Get("settings.open"),
                22,
                onSettings);
            UiFactory.Place(settingsButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -32f), new Vector2(240f, 56f));

            var controls = UiFactory.CreateText(root.transform, "Controls", Localization.Get("menu.controls"), 24, TextAnchor.MiddleCenter, new Color(0.55f, 0.62f, 0.72f));
            UiFactory.Place(controls.rectTransform, new Vector2(0.5f, 0.065f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 36f));

            buildLabel = UiFactory.CreateText(
                root.transform,
                "Build",
                Localization.Format("menu.build", BuildInfo.Version, BuildInfo.Commit),
                20,
                TextAnchor.MiddleLeft,
                new Color(0.45f, 0.52f, 0.62f));
            UiFactory.Place(
                buildLabel.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(36f, 4f),
                new Vector2(700f, 32f));

            return root;
        }

        private GameObject BuildCloudConflictPanel(
            System.Action onUseCloud,
            System.Action onKeepLocal,
            out Text details)
        {
            var panel = UiFactory.CreatePanel(
                _safeArea,
                "Cloud Conflict",
                new Color(0.008f, 0.012f, 0.022f, 1f));
            var root = panel.gameObject;
            root.SetActive(false);

            var title = UiFactory.CreateText(
                root.transform,
                "Title",
                Localization.Get("cloud.conflict.title"),
                52,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.78f, 0.35f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1100f, 80f));

            details = UiFactory.CreateText(
                root.transform,
                "Details",
                string.Empty,
                26,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.86f, 0.94f));
            UiFactory.Place(details.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 220f));

            var useCloud = UiFactory.CreateButton(
                root.transform,
                "Use Cloud",
                Localization.Get("cloud.useCloud"),
                28,
                onUseCloud);
            UiFactory.Place(useCloud.GetComponent<RectTransform>(), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(-210f, 0f), new Vector2(380f, 72f));

            var keepLocal = UiFactory.CreateButton(
                root.transform,
                "Keep Local",
                Localization.Get("cloud.keepLocal"),
                28,
                onKeepLocal);
            UiFactory.Place(keepLocal.GetComponent<RectTransform>(), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(210f, 0f), new Vector2(380f, 72f));

            return root;
        }

        private GameObject BuildConfirmationPanel(
            out Text title,
            out Text body,
            out Button confirmButton,
            out Text confirmLabel,
            out Button cancelButton,
            out Text cancelLabel)
        {
            var panel = UiFactory.CreatePanel(
                _safeArea,
                "Confirmation Panel",
                new Color(0.006f, 0.01f, 0.018f, 1f));
            var root = panel.gameObject;
            root.SetActive(false);

            title = UiFactory.CreateText(
                root.transform,
                "Title",
                string.Empty,
                52,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.78f, 0.35f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(
                title.rectTransform,
                new Vector2(0.5f, 0.74f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1100f, 90f));

            body = UiFactory.CreateText(
                root.transform,
                "Body",
                string.Empty,
                22,
                TextAnchor.MiddleCenter,
                new Color(0.84f, 0.88f, 0.95f));
            UiFactory.Place(
                body.rectTransform,
                new Vector2(0.5f, 0.54f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1200f, 180f));

            cancelButton = UiFactory.CreateButton(
                root.transform,
                "Cancel",
                Localization.Get("confirm.cancel"),
                28,
                CancelConfirmation);
            UiFactory.Place(
                cancelButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.28f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-220f, 0f),
                new Vector2(380f, 84f));
            cancelLabel = cancelButton.GetComponentInChildren<Text>();

            confirmButton = UiFactory.CreateButton(
                root.transform,
                "Confirm",
                string.Empty,
                28,
                ConfirmConfirmation);
            UiFactory.Place(
                confirmButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.28f),
                new Vector2(0.5f, 0.5f),
                new Vector2(220f, 0f),
                new Vector2(380f, 84f));
            confirmLabel = confirmButton.GetComponentInChildren<Text>();

            return root;
        }

        private GameObject BuildHelpPanel(
            out Text title,
            out Text body,
            out Button closeButton)
        {
            var panel = UiFactory.CreatePanel(
                _safeArea,
                "Help Panel",
                new Color(0.006f, 0.01f, 0.018f, 1f));
            var root = panel.gameObject;
            root.SetActive(false);

            title = UiFactory.CreateText(
                root.transform,
                "Title",
                Localization.Get("help.title"),
                56,
                TextAnchor.MiddleCenter,
                new Color(0.4f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(
                title.rectTransform,
                new Vector2(0.5f, 0.9f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1200f, 72f));

            body = UiFactory.CreateText(
                root.transform,
                "Body",
                Localization.Get("help.body"),
                20,
                TextAnchor.MiddleCenter,
                new Color(0.84f, 0.89f, 0.96f));
            UiFactory.Place(
                body.rectTransform,
                new Vector2(0.5f, 0.51f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1260f, 470f));

            closeButton = UiFactory.CreateButton(
                root.transform,
                "Close",
                Localization.Get("help.close"),
                28,
                HideHelp);
            UiFactory.Place(
                closeButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 36f),
                new Vector2(360f, 76f));

            return root;
        }

        private GameObject BuildAccountPanel(
            System.Action<string, string> onRegister,
            System.Action<string, string> onLogin,
            System.Action onLogout,
            System.Action onClose,
            System.Action onCloudBackup,
            System.Action onCloudRestore,
            out Text stateText,
            out Text statusText,
            out InputField username,
            out InputField password,
            out Button registerButton,
            out Button loginButton,
            out Button logoutButton,
            out Text cloudStatus,
            out Button cloudBackup,
            out Button cloudRestore)
        {
            var panel = UiFactory.CreatePanel(
                _safeArea,
                "Account Panel",
                new Color(0.008f, 0.012f, 0.022f, 1f));
            var root = panel.gameObject;
            root.SetActive(false);

            var title = UiFactory.CreateText(
                root.transform,
                "Title",
                Localization.Get("account.title"),
                64,
                TextAnchor.MiddleCenter,
                new Color(0.4f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1100f, 90f));

            stateText = UiFactory.CreateText(
                root.transform,
                "Account State",
                string.Empty,
                28,
                TextAnchor.MiddleCenter,
                new Color(0.72f, 0.78f, 0.9f));
            UiFactory.Place(stateText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 44f));

            var usernameInput = UiFactory.CreateInputField(root.transform, "Username", Localization.Get("account.username"), 28);
            username = usernameInput;
            UiFactory.Place(usernameInput.GetComponent<RectTransform>(), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 72f));

            var passwordInput = UiFactory.CreateInputField(root.transform, "Password", Localization.Get("account.password"), 28);
            password = passwordInput;
            passwordInput.contentType = InputField.ContentType.Password;
            UiFactory.Place(passwordInput.GetComponent<RectTransform>(), new Vector2(0.5f, 0.46f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 72f));

            statusText = UiFactory.CreateText(
                root.transform,
                "Status",
                string.Empty,
                24,
                TextAnchor.MiddleCenter,
                new Color(0.95f, 0.78f, 0.4f));
            UiFactory.Place(statusText.rectTransform, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 60f));

            registerButton = UiFactory.CreateButton(
                root.transform,
                "Register",
                Localization.Get("account.register"),
                30,
                () => onRegister(usernameInput.text, passwordInput.text));
            UiFactory.Place(registerButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.23f), new Vector2(0.5f, 0.5f), new Vector2(-190f, 0f), new Vector2(340f, 72f));

            loginButton = UiFactory.CreateButton(
                root.transform,
                "Login",
                Localization.Get("account.login"),
                30,
                () => onLogin(usernameInput.text, passwordInput.text));
            UiFactory.Place(loginButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.23f), new Vector2(0.5f, 0.5f), new Vector2(190f, 0f), new Vector2(340f, 72f));

            logoutButton = UiFactory.CreateButton(
                root.transform,
                "Logout",
                Localization.Get("account.logout"),
                30,
                onLogout);
            UiFactory.Place(logoutButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.23f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 72f));

            cloudStatus = UiFactory.CreateText(
                root.transform,
                "Cloud Status",
                Localization.Get("cloud.signedout"),
                22,
                TextAnchor.MiddleCenter,
                new Color(0.55f, 0.85f, 0.75f));
            UiFactory.Place(cloudStatus.rectTransform, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 40f));

            cloudBackup = UiFactory.CreateButton(
                root.transform,
                "Cloud Backup",
                Localization.Get("cloud.backup"),
                26,
                onCloudBackup);
            UiFactory.Place(cloudBackup.GetComponent<RectTransform>(), new Vector2(0.5f, 0.07f), new Vector2(0.5f, 0.5f), new Vector2(-190f, 0f), new Vector2(340f, 64f));

            cloudRestore = UiFactory.CreateButton(
                root.transform,
                "Cloud Restore",
                Localization.Get("cloud.restore"),
                26,
                onCloudRestore);
            UiFactory.Place(cloudRestore.GetComponent<RectTransform>(), new Vector2(0.5f, 0.07f), new Vector2(0.5f, 0.5f), new Vector2(190f, 0f), new Vector2(340f, 64f));

            var close = UiFactory.CreateButton(
                root.transform,
                "Close",
                Localization.Get("account.close"),
                26,
                onClose);
            UiFactory.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 32f), new Vector2(220f, 64f));

            return root;
        }

        private GameObject BuildHud(
            System.Action onPause,
            System.Action onReload,
            System.Action onSwitchWeapon,
            System.Action onActiveSkill,
            System.Action<bool> onFireHeld,
            out Text healthText,
            out Text armorText,
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
            out Image aimBaseImage,
            out Image moveKnobImage,
            out Image aimKnobImage)
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

            armorText = UiFactory.CreateText(root.transform, "Armor", string.Empty, 20, TextAnchor.MiddleLeft, new Color(0.45f, 0.75f, 1f));
            UiFactory.Place(armorText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -86f), new Vector2(440f, 34f));

            roundText = UiFactory.CreateText(root.transform, "Round", string.Empty, 36, TextAnchor.MiddleCenter, new Color(0.9f, 0.95f, 1f));
            roundText.fontStyle = FontStyle.Bold;
            UiFactory.Place(roundText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(420f, 60f));

            enemiesText = UiFactory.CreateText(root.transform, "Enemies", string.Empty, 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.72f, 0.42f));
            UiFactory.Place(enemiesText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(420f, 40f));

            var roundModifier = UiFactory.CreateText(root.transform, "Round Modifier", string.Empty, 24, TextAnchor.MiddleCenter, new Color(1f, 0.62f, 0.25f));
            roundModifier.fontStyle = FontStyle.Bold;
            UiFactory.Place(roundModifier.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(700f, 40f));

            var bossPanel = UiFactory.CreatePanel(root.transform, "Boss Panel", new Color(0.09f, 0.02f, 0.07f, 0.86f), blocksRaycasts: false);
            UiFactory.Place(bossPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -218f), new Vector2(860f, 92f));

            var bossName = UiFactory.CreateText(bossPanel, "Boss Name", Localization.Get("boss.name"), 24, TextAnchor.MiddleCenter, new Color(1f, 0.64f, 0.88f));
            bossName.fontStyle = FontStyle.Bold;
            UiFactory.Place(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(820f, 32f));

            var bossHealthBackground = UiFactory.CreateImage(bossPanel, "Boss Health", SpriteFactory.Square, new Color(0.06f, 0.01f, 0.05f, 0.95f));
            UiFactory.Place(bossHealthBackground.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(800f, 34f));

            var bossHealthFill = UiFactory.CreateImage(bossHealthBackground.transform, "Boss Health Fill", SpriteFactory.Square, new Color(0.92f, 0.35f, 0.72f));
            UiFactory.Stretch(bossHealthFill.rectTransform);
            bossHealthFill.rectTransform.offsetMin = new Vector2(3f, 3f);
            bossHealthFill.rectTransform.offsetMax = new Vector2(-3f, -3f);
            bossHealthFill.type = Image.Type.Filled;
            bossHealthFill.fillMethod = Image.FillMethod.Horizontal;
            bossHealthFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            var bossHealthText = UiFactory.CreateText(bossHealthBackground.transform, "Boss Health Text", string.Empty, 18, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Stretch(bossHealthText.rectTransform);
            bossHealthText.fontStyle = FontStyle.Bold;

            bossPanel.gameObject.SetActive(false);

            killsText = UiFactory.CreateText(root.transform, "Kills", string.Empty, 22, TextAnchor.MiddleCenter, new Color(0.6f, 0.68f, 0.78f));
            UiFactory.Place(killsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(420f, 40f));

            suppliesText = UiFactory.CreateText(root.transform, "Supplies", string.Empty, 28, TextAnchor.MiddleRight, new Color(0.55f, 0.95f, 0.75f));
            UiFactory.Place(suppliesText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-158f, -30f), new Vector2(420f, 44f));

            var suppliesIcon = UiFactory.CreateImage(root.transform, "Supplies Icon", ArtCatalog.Get("UI/reward_supplies"), Color.white);
            UiFactory.Place(suppliesIcon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-112f, -30f), new Vector2(36f, 36f));

            levelText = UiFactory.CreateText(root.transform, "Level", string.Empty, 20, TextAnchor.MiddleRight, new Color(0.6f, 0.68f, 0.78f));
            UiFactory.Place(levelText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140f, -78f), new Vector2(500f, 40f));

            var pause = UiFactory.CreateButton(root.transform, "Pause", Localization.Get("hud.pause"), 28, onPause);
            UiFactory.Place(pause.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(84f, 84f));

            var weaponIcon = UiFactory.CreateImage(root.transform, "Weapon Icon", ArtCatalog.Get("UI/weapon_pistol"), Color.white);
            UiFactory.Place(weaponIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -128f), new Vector2(38f, 38f));

            var weaponText = UiFactory.CreateText(root.transform, "Weapon", string.Empty, 22, TextAnchor.MiddleLeft, new Color(0.45f, 0.9f, 1f));
            weaponText.fontStyle = FontStyle.Bold;
            UiFactory.Place(weaponText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(88f, -128f), new Vector2(300f, 28f));

            var ammoText = UiFactory.CreateText(root.transform, "Ammo", string.Empty, 24, TextAnchor.MiddleLeft, new Color(0.92f, 0.95f, 1f));
            ammoText.fontStyle = FontStyle.Bold;
            UiFactory.Place(ammoText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -158f), new Vector2(460f, 32f));

            var reloadBackground = UiFactory.CreateImage(root.transform, "Reload", SpriteFactory.Square, new Color(0.05f, 0.07f, 0.1f, 0.9f));
            UiFactory.Place(reloadBackground.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -196f), new Vector2(300f, 10f));
            var reloadFill = UiFactory.CreateImage(reloadBackground.transform, "Reload Fill", SpriteFactory.Square, new Color(1f, 0.78f, 0.35f));
            UiFactory.Stretch(reloadFill.rectTransform);
            reloadFill.type = Image.Type.Filled;
            reloadFill.fillMethod = Image.FillMethod.Horizontal;
            reloadFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            var reloadStatus = UiFactory.CreateText(root.transform, "Reload Status", string.Empty, 18, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.35f));
            UiFactory.Place(reloadStatus.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -222f), new Vector2(340f, 26f));

            var reload = UiFactory.CreateButton(root.transform, "Reload Button", Localization.Get("hud.reload"), 22, onReload);
            UiFactory.Place(reload.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-110f, 84f), new Vector2(190f, 66f));

            var switchWeapon = UiFactory.CreateButton(root.transform, "Switch Weapon", Localization.Get("hud.switch"), 22, onSwitchWeapon);
            UiFactory.Place(switchWeapon.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(100f, 84f), new Vector2(190f, 66f));

            var activeSkill = UiFactory.CreateButton(root.transform, "Active Skill Button", Localization.Get("hud.skill"), 20, onActiveSkill);
            UiFactory.Place(
                activeSkill.GetComponent<RectTransform>(),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-170f, 330f),
                new Vector2(130f, 130f));
            var cooldown = UiFactory.CreateImage(activeSkill.transform, "Active Skill Cooldown", SpriteFactory.Ring, new Color(0.02f, 0.02f, 0.03f, 0.78f));
            UiFactory.Stretch(cooldown.rectTransform);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillAmount = 0f;

            var skillIcon = UiFactory.CreateImage(activeSkill.transform, "Active Skill Icon", ArtCatalog.Get("UI/skill_turret"), Color.white);
            UiFactory.Place(skillIcon.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));

            var skillLabel = activeSkill.GetComponentInChildren<Text>();
            skillLabel.name = "Active Skill Label";
            skillLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            skillLabel.rectTransform.anchorMax = new Vector2(1f, 0.40f);
            skillLabel.rectTransform.offsetMin = Vector2.zero;
            skillLabel.rectTransform.offsetMax = Vector2.zero;
            skillLabel.transform.SetAsLastSibling();

            var hint = UiFactory.CreateText(root.transform, "Controls Hint", Localization.Get("menu.controls"), 20, TextAnchor.MiddleCenter, new Color(0.48f, 0.56f, 0.66f));
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1400f, 40f));

            var fire = UiFactory.CreateButton(root.transform, "Fire Button", Localization.Get("hud.fire"), 26, null);
            fire.onClick.RemoveAllListeners();
            var fireTrigger = fire.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(fireTrigger, EventTriggerType.PointerDown, () => onFireHeld?.Invoke(true));
            AddPointerTrigger(fireTrigger, EventTriggerType.PointerUp, () => onFireHeld?.Invoke(false));
            AddPointerTrigger(fireTrigger, EventTriggerType.PointerExit, () => onFireHeld?.Invoke(false));
            UiFactory.Place(
                fire.GetComponent<RectTransform>(),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-380f, 150f),
                new Vector2(FireButtonSize, FireButtonSize));
            fire.gameObject.SetActive(false);

            moveBaseImage = BuildStick(root.transform, "Move Stick", out moveBase, out moveKnob, out moveKnobImage);
            aimBaseImage = BuildStick(root.transform, "Aim Stick", out aimBase, out aimKnob, out aimKnobImage);
            moveBaseImage.color = new Color(0.7f, 0.82f, 0.9f, 0.15f);
            aimBaseImage.color = new Color(0.7f, 0.82f, 0.9f, 0.15f);
            moveBase.anchorMin = moveBase.anchorMax = new Vector2(0f, 0f);
            moveBase.anchoredPosition = new Vector2(150f, 150f);
            aimBase.anchorMin = aimBase.anchorMax = new Vector2(1f, 0f);
            aimBase.anchoredPosition = new Vector2(-150f, 150f);

            return root;
        }

        private static void AddPointerTrigger(
            EventTrigger trigger,
            EventTriggerType type,
            System.Action callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
        }

        private static Image BuildStick(
            Transform parent,
            string name,
            out RectTransform baseRect,
            out RectTransform knobRect,
            out Image knobImage)
        {
            var baseImage = UiFactory.CreateImage(parent, name, SpriteFactory.Ring, new Color(0.7f, 0.82f, 0.9f, 0.15f));
            baseRect = baseImage.rectTransform;
            baseRect.anchorMin = new Vector2(0.5f, 0.5f);
            baseRect.anchorMax = new Vector2(0.5f, 0.5f);
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.sizeDelta = new Vector2(StickBaseSize, StickBaseSize);

            knobImage = UiFactory.CreateImage(baseImage.transform, "Knob", SpriteFactory.Circle, new Color(0.72f, 0.95f, 1f, 0.42f));
            knobRect = knobImage.rectTransform;
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(StickKnobSize, StickKnobSize);
            knobRect.anchoredPosition = Vector2.zero;

            return baseImage;
        }

        private GameObject BuildPausePanel(
            System.Action onResume,
            System.Action onMenu,
            System.Action onArmory,
            System.Action onCharacterSheet,
            System.Action onSettings,
            out Button settingsButton,
            out Button menuButton)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Pause Panel", new Color(0.01f, 0.02f, 0.03f, 0.9f));
            var root = panel.gameObject;

            var title = UiFactory.CreateText(root.transform, "Title", Localization.Get("pause.title"), 64, TextAnchor.MiddleCenter, new Color(0.5f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 100f));

            var resume = UiFactory.CreateButton(root.transform, "Resume", Localization.Get("pause.resume"), 32, onResume);
            UiFactory.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 84f));

            var armory = UiFactory.CreateButton(root.transform, "Armory", Localization.Get("pause.armory"), 28, onArmory);
            UiFactory.Place(armory.GetComponent<RectTransform>(), new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 74f));

            var character = UiFactory.CreateButton(root.transform, "Skills", Localization.Get("character.open"), 28, onCharacterSheet);
            UiFactory.Place(character.GetComponent<RectTransform>(), new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 74f));

            var menu = UiFactory.CreateButton(root.transform, "Menu", Localization.Get("pause.menu"), 28, onMenu);
            UiFactory.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 74f));
            menuButton = menu;

            settingsButton = UiFactory.CreateButton(
                root.transform,
                "Settings",
                Localization.Get("settings.open"),
                28,
                onSettings);
            UiFactory.Place(settingsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.11f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 74f));

            return root;
        }

        private GameObject BuildSettingsPanel(
            System.Action onToggleMute,
            System.Action<float> onMasterChanged,
            System.Action<float> onEffectsChanged,
            out Button muteButton,
            out Text muteLabel,
            out Button masterMinus,
            out Button masterPlus,
            out Text masterLabel,
            out Button effectsMinus,
            out Button effectsPlus,
            out Text effectsLabel,
            out Button languageSystemButton,
            out Button languageEnglishButton,
            out Button languageSpanishButton,
            out Text languageCurrentLabel,
            out Button leftHandedButton,
            out Text leftHandedLabel,
            out Button joystickSizeMinus,
            out Button joystickSizePlus,
            out Text joystickSizeLabel,
            out Button joystickOpacityMinus,
            out Button joystickOpacityPlus,
            out Text joystickOpacityLabel,
            out Button aimAssistButton,
            out Text aimAssistLabel,
            out Button autoFireButton,
            out Text autoFireLabel,
            out Button reducedMotionButton,
            out Text reducedMotionLabel,
            out Button hapticsButton,
            out Text hapticsLabel,
            out Button colorblindButton,
            out Text colorblindLabel,
            out Button resetButton,
            out Button closeButton,
            out Button helpButton)
        {
            var panel = UiFactory.CreatePanel(
                _safeArea,
                "Settings Panel",
                new Color(0.006f, 0.01f, 0.018f, 1f));
            var root = panel.gameObject;
            root.SetActive(false);

            var title = UiFactory.CreateText(
                root.transform,
                "Title",
                Localization.Get("settings.title"),
                46,
                TextAnchor.MiddleCenter,
                new Color(0.4f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(900f, 64f));

            var audioCard = CreateSettingsCard(root.transform, "Audio Section", new Vector2(-395f, 195f));
            CreateSettingsCardTitle(audioCard, "Audio Section Title", "settings.audio.title");
            CreateSettingsRowLabel(audioCard, "Sound Label", "settings.audio.mute", -78f);
            muteButton = UiFactory.CreateButton(
                audioCard,
                "Sound Toggle",
                Localization.Get("settings.value.on"),
                22,
                onToggleMute);
            UiFactory.Place(muteButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -78f), new Vector2(210f, 52f));
            muteLabel = muteButton.GetComponentInChildren<Text>();

            CreateSettingsRowLabel(audioCard, "Master Volume Label", "settings.audio.master", -134f);
            masterMinus = CreateSettingsStepperButton(
                audioCard,
                "Master Volume Down",
                "-",
                -236f,
                -134f,
                () => onMasterChanged(Mathf.Clamp01(_masterVolume - AudioVolumeStep)));
            masterLabel = CreateSettingsValueLabel(audioCard, "Master Volume Value", -134f);
            masterPlus = CreateSettingsStepperButton(
                audioCard,
                "Master Volume Up",
                "+",
                -24f,
                -134f,
                () => onMasterChanged(Mathf.Clamp01(_masterVolume + AudioVolumeStep)));

            CreateSettingsRowLabel(audioCard, "Effects Volume Label", "settings.audio.effects", -190f);
            effectsMinus = CreateSettingsStepperButton(
                audioCard,
                "Effects Volume Down",
                "-",
                -236f,
                -190f,
                () => onEffectsChanged(Mathf.Clamp01(_effectsVolume - AudioVolumeStep)));
            effectsLabel = CreateSettingsValueLabel(audioCard, "Effects Volume Value", -190f);
            effectsPlus = CreateSettingsStepperButton(
                audioCard,
                "Effects Volume Up",
                "+",
                -24f,
                -190f,
                () => onEffectsChanged(Mathf.Clamp01(_effectsVolume + AudioVolumeStep)));

            var languageCard = CreateSettingsCard(root.transform, "Language Section", new Vector2(395f, 195f));
            CreateSettingsCardTitle(languageCard, "Language Section Title", "settings.language.title");
            CreateSettingsRowLabel(languageCard, "Language Current Label", "settings.language.current", -78f);
            languageCurrentLabel = UiFactory.CreateText(
                languageCard,
                "Language Current Value",
                string.Empty,
                24,
                TextAnchor.MiddleRight,
                new Color(0.55f, 0.95f, 0.75f));
            UiFactory.Place(languageCurrentLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -78f), new Vector2(260f, 54f));

            languageSystemButton = UiFactory.CreateButton(
                languageCard,
                "System Language",
                Localization.Get("settings.language.system"),
                20,
                () => SelectLocale(Localization.SystemLocaleValue));
            UiFactory.Place(languageSystemButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -180f), new Vector2(230f, 62f));

            languageEnglishButton = UiFactory.CreateButton(
                languageCard,
                "English",
                Localization.GetDisplayName("en"),
                22,
                () => SelectLocale("en"));
            UiFactory.Place(languageEnglishButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(265f, -180f), new Vector2(230f, 62f));

            languageSpanishButton = UiFactory.CreateButton(
                languageCard,
                "Spanish",
                Localization.GetDisplayName("es"),
                22,
                () => SelectLocale("es"));
            UiFactory.Place(languageSpanishButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(510f, -180f), new Vector2(230f, 62f));

            var controlsCard = CreateSettingsCard(root.transform, "Controls Section", new Vector2(-395f, -195f));
            CreateSettingsCardTitle(controlsCard, "Controls Section Title", "settings.controls.title");

            CreateSettingsRowLabel(controlsCard, "Left Handed Label", "settings.controls.leftHanded", -78f);
            leftHandedButton = UiFactory.CreateButton(
                controlsCard,
                "Left Handed Toggle",
                Localization.Get("settings.value.off"),
                22,
                ToggleLeftHanded);
            UiFactory.Place(leftHandedButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -78f), new Vector2(210f, 52f));
            leftHandedLabel = leftHandedButton.GetComponentInChildren<Text>();

            CreateSettingsRowLabel(controlsCard, "Joystick Size Label", "settings.controls.joystickSize", -134f);
            joystickSizeMinus = CreateSettingsStepperButton(
                controlsCard,
                "Joystick Size Down",
                "-",
                -236f,
                -134f,
                () => AdjustJoystickSize(-GameSettings.JoystickSizeStep));
            joystickSizeLabel = CreateSettingsValueLabel(controlsCard, "Joystick Size Value", -134f);
            joystickSizePlus = CreateSettingsStepperButton(
                controlsCard,
                "Joystick Size Up",
                "+",
                -24f,
                -134f,
                () => AdjustJoystickSize(GameSettings.JoystickSizeStep));

            CreateSettingsRowLabel(controlsCard, "Joystick Opacity Label", "settings.controls.joystickOpacity", -190f);
            joystickOpacityMinus = CreateSettingsStepperButton(
                controlsCard,
                "Joystick Opacity Down",
                "-",
                -236f,
                -190f,
                () => AdjustJoystickOpacity(-GameSettings.JoystickOpacityStep));
            joystickOpacityLabel = CreateSettingsValueLabel(controlsCard, "Joystick Opacity Value", -190f);
            joystickOpacityPlus = CreateSettingsStepperButton(
                controlsCard,
                "Joystick Opacity Up",
                "+",
                -24f,
                -190f,
                () => AdjustJoystickOpacity(GameSettings.JoystickOpacityStep));

            CreateSettingsRowLabel(controlsCard, "Aim Assist Label", "settings.controls.aimAssist", -246f);
            aimAssistButton = UiFactory.CreateButton(
                controlsCard,
                "Aim Assist Toggle",
                Localization.Get("settings.aim.off"),
                22,
                CycleAimAssist);
            UiFactory.Place(aimAssistButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -246f), new Vector2(210f, 52f));
            aimAssistLabel = aimAssistButton.GetComponentInChildren<Text>();

            CreateSettingsRowLabel(controlsCard, "Auto Fire Label", "settings.controls.autoFire", -302f);
            autoFireButton = UiFactory.CreateButton(
                controlsCard,
                "Auto Fire Toggle",
                Localization.Get("settings.value.on"),
                22,
                ToggleAutoFire);
            UiFactory.Place(autoFireButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -302f), new Vector2(210f, 52f));
            autoFireLabel = autoFireButton.GetComponentInChildren<Text>();

            var accessibilityCard = CreateSettingsCard(root.transform, "Accessibility Section", new Vector2(395f, -195f));
            CreateSettingsCardTitle(accessibilityCard, "Accessibility Section Title", "settings.accessibility.title");

            CreateSettingsRowLabel(accessibilityCard, "Reduced Motion Label", "settings.accessibility.reducedMotion", -90f);
            reducedMotionButton = UiFactory.CreateButton(
                accessibilityCard,
                "Reduced Motion Toggle",
                Localization.Get("settings.value.off"),
                22,
                ToggleReducedMotion);
            UiFactory.Place(reducedMotionButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -90f), new Vector2(210f, 52f));
            reducedMotionLabel = reducedMotionButton.GetComponentInChildren<Text>();

            CreateSettingsRowLabel(accessibilityCard, "Haptics Label", "settings.accessibility.haptics", -170f);
            hapticsButton = UiFactory.CreateButton(
                accessibilityCard,
                "Haptics Toggle",
                Localization.Get("settings.value.off"),
                22,
                ToggleHaptics);
            UiFactory.Place(hapticsButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -170f), new Vector2(210f, 52f));
            hapticsLabel = hapticsButton.GetComponentInChildren<Text>();

            CreateSettingsRowLabel(accessibilityCard, "Colorblind Label", "settings.accessibility.colorblind", -250f);
            colorblindButton = UiFactory.CreateButton(
                accessibilityCard,
                "Colorblind Toggle",
                Localization.Get("settings.value.off"),
                22,
                ToggleColorblindSafeTelegraphs);
            UiFactory.Place(colorblindButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -250f), new Vector2(210f, 52f));
            colorblindLabel = colorblindButton.GetComponentInChildren<Text>();

            resetButton = UiFactory.CreateButton(
                root.transform,
                "Reset",
                Localization.Get("settings.reset"),
                24,
                ResetSettings);
            UiFactory.Place(resetButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-380f, 26f), new Vector2(360f, 64f));

            helpButton = UiFactory.CreateButton(
                root.transform,
                "How To Play",
                Localization.Get("help.open"),
                24,
                ShowHelp);
            UiFactory.Place(helpButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(320f, 64f));

            closeButton = UiFactory.CreateButton(
                root.transform,
                "Close",
                Localization.Get("settings.close"),
                24,
                HideSettingsPanel);
            UiFactory.Place(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(380f, 26f), new Vector2(360f, 64f));

            return root;
        }

        private static RectTransform CreateSettingsCard(Transform parent, string name, Vector2 position)
        {
            var card = UiFactory.CreatePanel(parent, name, new Color(0.022f, 0.04f, 0.062f, 0.98f));
            UiFactory.Place(
                card,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                new Vector2(760f, 360f));
            return card;
        }

        private static Text CreateSettingsCardTitle(RectTransform card, string name, string key)
        {
            var title = UiFactory.CreateText(
                card,
                name,
                Localization.Get(key),
                24,
                TextAnchor.MiddleLeft,
                new Color(0.52f, 0.9f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(500f, 40f));
            return title;
        }

        private static Text CreateSettingsRowLabel(
            RectTransform card,
            string name,
            string key,
            float y)
        {
            var label = UiFactory.CreateText(
                card,
                name,
                Localization.Get(key),
                20,
                TextAnchor.MiddleLeft,
                new Color(0.78f, 0.84f, 0.92f));
            UiFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, y), new Vector2(440f, 54f));
            return label;
        }

        private static Text CreateSettingsValueLabel(RectTransform card, string name, float y)
        {
            var label = UiFactory.CreateText(
                card,
                name,
                string.Empty,
                22,
                TextAnchor.MiddleRight,
                new Color(0.55f, 0.95f, 0.75f));
            UiFactory.Place(label.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-96f, y), new Vector2(110f, 54f));
            return label;
        }

        private static Button CreateSettingsStepperButton(
            RectTransform card,
            string name,
            string label,
            float x,
            float y,
            System.Action onClick)
        {
            var button = UiFactory.CreateButton(card, name, label, 28, onClick);
            UiFactory.Place(button.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(x, y), new Vector2(56f, 52f));
            return button;
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

        private GameObject BuildLeaderboardPanel(
            System.Action onClose,
            out Text status,
            out Text entries,
            out RectTransform viewport)
        {
            var panel = UiFactory.CreatePanel(
                _safeArea,
                "Leaderboard Panel",
                new Color(0.008f, 0.012f, 0.022f, 1f));
            var root = panel.gameObject;
            root.SetActive(false);

            var title = UiFactory.CreateText(
                root.transform,
                "Title",
                Localization.Get("leaderboard.title"),
                52,
                TextAnchor.MiddleCenter,
                new Color(0.4f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1100f, 70f));

            status = UiFactory.CreateText(
                root.transform,
                "Status",
                string.Empty,
                24,
                TextAnchor.MiddleCenter,
                new Color(0.85f, 0.8f, 0.5f));
            UiFactory.Place(status.rectTransform, new Vector2(0.5f, 0.83f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1200f, 36f));

            var viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(RectMask2D));
            viewportObject.transform.SetParent(root.transform, false);
            viewport = viewportObject.GetComponent<RectTransform>();
            UiFactory.Place(
                viewport,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -30f),
                new Vector2(1900f, 700f));

            entries = UiFactory.CreateText(
                viewportObject.transform,
                "Entries",
                string.Empty,
                20,
                TextAnchor.UpperLeft,
                new Color(0.86f, 0.9f, 0.96f));
            var entriesRect = entries.rectTransform;
            entriesRect.anchorMin = new Vector2(0f, 1f);
            entriesRect.anchorMax = new Vector2(1f, 1f);
            entriesRect.pivot = new Vector2(0.5f, 1f);
            entriesRect.anchoredPosition = Vector2.zero;
            entriesRect.sizeDelta = new Vector2(0f, 0f);
            entries.verticalOverflow = VerticalWrapMode.Overflow;
            var fitter = entries.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = entriesRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var close = UiFactory.CreateButton(
                root.transform,
                "Close",
                Localization.Get("account.close"),
                26,
                onClose);
            UiFactory.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 28f), new Vector2(220f, 64f));

            return root;
        }
        private GameObject BuildVictoryPanel(
            System.Action onNextRun,
            System.Action onMenu,
            System.Action onLeaderboard,
            out Text summary)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Victory Panel", new Color(0.01f, 0.045f, 0.03f, 0.97f));
            var root = panel.gameObject;

            var title = UiFactory.CreateText(root.transform, "Title", Localization.Get("victory.title"), 72, TextAnchor.MiddleCenter, new Color(0.45f, 1f, 0.62f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 110f));

            var subtitle = UiFactory.CreateText(root.transform, "Subtitle", Localization.Get("victory.subtitle"), 28, TextAnchor.MiddleCenter, new Color(0.8f, 0.92f, 0.86f));
            UiFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 60f));

            summary = UiFactory.CreateText(root.transform, "Summary", string.Empty, 30, TextAnchor.MiddleCenter, new Color(0.9f, 0.96f, 0.92f));
            UiFactory.Place(summary.rectTransform, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 120f));

            var next = UiFactory.CreateButton(root.transform, "Next Run", Localization.Get("victory.next"), 36, onNextRun);
            UiFactory.Place(next.GetComponent<RectTransform>(), new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 104f));

            var menu = UiFactory.CreateButton(root.transform, "Menu", Localization.Get("victory.menu"), 30, onMenu);
            UiFactory.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.21f), new Vector2(0.5f, 0.5f), new Vector2(-190f, 0f), new Vector2(340f, 88f));

            var leaderboard = UiFactory.CreateButton(root.transform, "Leaderboard", Localization.Get("leaderboard.open"), 28, onLeaderboard);
            UiFactory.Place(leaderboard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.21f), new Vector2(0.5f, 0.5f), new Vector2(190f, 0f), new Vector2(340f, 88f));

            root.SetActive(false);
            return root;
        }

        private GameObject BuildRoundEndPanel(            System.Action<int> onSelect,
            System.Action onCharacterSheet,
            out Text title,
            out Text summary,
            out Button[] buttons,
            out Text[] labels,
            out Image[] icons)
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
            icons = new Image[GameBalance.RoundRewardOptionCount];
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

                var icon = UiFactory.CreateImage(
                    button.transform,
                    $"Reward {i + 1} Icon",
                    ArtCatalog.Get("UI/reward_supplies"),
                    Color.white);
                UiFactory.Place(
                    icon.rectTransform,
                    new Vector2(0.5f, 0.72f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(96f, 96f));

                labels[i] = button.GetComponentInChildren<Text>();
                labels[i].rectTransform.anchorMin = new Vector2(0f, 0f);
                labels[i].rectTransform.anchorMax = new Vector2(1f, 0.42f);
                labels[i].rectTransform.offsetMin = new Vector2(18f, 10f);
                labels[i].rectTransform.offsetMax = new Vector2(-18f, 0f);
                icons[i] = icon;
                buttons[i] = button;
            }

            var character = UiFactory.CreateButton(
                root.transform,
                "Skills",
                Localization.Get("character.open"),
                28,
                onCharacterSheet);
            UiFactory.Place(
                character.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.12f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(360f, 76f));

            root.SetActive(false);
            return root;
        }

        private GameObject BuildCharacterSheet(
            System.Action<SkillNode> onAllocate,
            System.Action onRespec,
            System.Action<SkillNode> onSelectActive,
            System.Action onClose,
            out Text levelText,
            out Text pointsText,
            out Text statsText,
            out Text statusText,
            out Button respecButton,
            out Text respecLabel,
            out Button[] tabs,
            out Text[] tabLabels,
            out GameObject[] branchPanels,
            out CharacterNodeRow[] nodeRows)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Character Sheet", new Color(0.008f, 0.012f, 0.022f, 1f));
            var root = panel.gameObject;

            var title = UiFactory.CreateText(
                root.transform,
                "Title",
                Localization.Get("character.title"),
                60,
                TextAnchor.MiddleCenter,
                new Color(0.4f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 70f));

            levelText = UiFactory.CreateText(
                root.transform,
                "Level",
                string.Empty,
                26,
                TextAnchor.MiddleLeft,
                new Color(0.72f, 0.82f, 0.9f));
            UiFactory.Place(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(700f, 46f));

            pointsText = UiFactory.CreateText(
                root.transform,
                "Points",
                string.Empty,
                26,
                TextAnchor.MiddleRight,
                new Color(0.55f, 0.95f, 0.75f));
            UiFactory.Place(pointsText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-290f, -26f), new Vector2(500f, 46f));

            var close = UiFactory.CreateButton(root.transform, "Close", Localization.Get("character.close"), 26, onClose);
            UiFactory.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -24f), new Vector2(220f, 72f));

            var branches = new[] { SkillBranch.Strength, SkillBranch.Agility, SkillBranch.Survival };
            tabs = new Button[branches.Length];
            tabLabels = new Text[branches.Length];
            for (var i = 0; i < branches.Length; i++)
            {
                var index = i;
                tabs[i] = UiFactory.CreateButton(
                    root.transform,
                    $"Tab {branches[i]}",
                    Localization.BranchName(branches[i]),
                    28,
                    () => SetActiveCharacterTab(index));
                UiFactory.Place(
                    tabs[i].GetComponent<RectTransform>(),
                    new Vector2(0.27f + (i * 0.23f), 0.81f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(320f, 62f));
                tabLabels[i] = tabs[i].GetComponentInChildren<Text>();
            }

            branchPanels = new GameObject[branches.Length];
            nodeRows = new CharacterNodeRow[SkillCatalog.All.Count];
            for (var branchIndex = 0; branchIndex < branches.Length; branchIndex++)
            {
                var branch = branches[branchIndex];
                var branchPanel = new GameObject($"Branch {branch}", typeof(RectTransform));
                branchPanel.transform.SetParent(root.transform, false);
                branchPanels[branchIndex] = branchPanel;

                var branchTitle = UiFactory.CreateText(
                    branchPanel.transform,
                    "Branch Title",
                    Localization.BranchName(branch),
                    28,
                    TextAnchor.MiddleCenter,
                    new Color(0.4f, 0.9f, 1f));
                branchTitle.fontStyle = FontStyle.Bold;
                UiFactory.Place(branchTitle.rectTransform, new Vector2(0.5f, 0.98f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(420f, 40f));

                var rowIndex = 0;
                for (var nodeIndex = 0; nodeIndex < SkillCatalog.All.Count; nodeIndex++)
                {
                    var definition = SkillCatalog.All[nodeIndex];
                    if (definition.Branch != branch)
                    {
                        continue;
                    }

                    var rowImage = UiFactory.CreateImage(
                        branchPanel.transform,
                        $"Node {definition.Node}",
                        SpriteFactory.Square,
                        new Color(0.05f, 0.08f, 0.12f, 0.96f));
                    var rowRect = rowImage.rectTransform;
                    rowRect.anchorMin = new Vector2(0.03f, 0.66f - (rowIndex * 0.27f));
                    rowRect.anchorMax = new Vector2(0.97f, 0.90f - (rowIndex * 0.27f));
                    rowRect.offsetMin = Vector2.zero;
                    rowRect.offsetMax = Vector2.zero;

                    var name = UiFactory.CreateText(
                        rowImage.transform,
                        "Name",
                        string.Empty,
                        24,
                        TextAnchor.MiddleLeft,
                        Color.white);
                    name.fontStyle = FontStyle.Bold;
                    UiFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(260f, 32f));

                    var rank = UiFactory.CreateText(
                        rowImage.transform,
                        "Rank",
                        string.Empty,
                        20,
                        TextAnchor.MiddleRight,
                        new Color(0.72f, 0.82f, 0.9f));
                    UiFactory.Place(rank.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-100f, -8f), new Vector2(160f, 32f));

                    var pips = UiFactory.CreateText(
                        rowImage.transform,
                        "Pips",
                        string.Empty,
                        22,
                        TextAnchor.MiddleLeft,
                        new Color(0.45f, 0.95f, 0.7f));
                    UiFactory.Place(pips.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -40f), new Vector2(380f, 30f));

                    var preview = UiFactory.CreateText(
                        rowImage.transform,
                        "Preview",
                        string.Empty,
                        18,
                        TextAnchor.MiddleLeft,
                        new Color(0.72f, 0.8f, 0.9f));
                    preview.rectTransform.anchorMin = new Vector2(0f, 0f);
                    preview.rectTransform.anchorMax = new Vector2(1f, 0f);
                    preview.rectTransform.pivot = new Vector2(0f, 0f);
                    preview.rectTransform.offsetMin = new Vector2(18f, 8f);
                    preview.rectTransform.offsetMax = new Vector2(-110f, 58f);

                    var allocate = UiFactory.CreateButton(
                        rowImage.transform,
                        "Allocate",
                        Localization.Get("character.allocate"),
                        38,
                        () => onAllocate?.Invoke(definition.Node));
                    UiFactory.Place(
                        allocate.GetComponent<RectTransform>(),
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(-16f, 0f),
                        new Vector2(84f, 76f));

                    nodeRows[(int)definition.Node] = new CharacterNodeRow
                    {
                        Node = definition.Node,
                        Allocate = allocate,
                        AllocateLabel = allocate.GetComponentInChildren<Text>(),
                        Name = name,
                        Rank = rank,
                        Pips = pips,
                        Preview = preview
                    };
                    rowIndex++;
                }
            }

            statsText = UiFactory.CreateText(
                root.transform,
                "Stats",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                new Color(0.55f, 0.92f, 0.7f));
            UiFactory.Place(statsText.rectTransform, new Vector2(0.5f, 0.075f), new Vector2(0.5f, 0.5f), new Vector2(-100f, 0f), new Vector2(1300f, 36f));

            var synergyText = UiFactory.CreateText(
                root.transform,
                "Synergies",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.76f, 1f));
            UiFactory.Place(
                synergyText.rectTransform,
                new Vector2(0.5f, 0.102f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1500f, 24f));

            respecButton = UiFactory.CreateButton(
                root.transform,
                "Respec",
                Localization.Get("character.respec"),
                24,
                onRespec);
            UiFactory.Place(
                respecButton.GetComponent<RectTransform>(),
                new Vector2(1f, 0.075f),
                new Vector2(1f, 0.5f),
                new Vector2(-40f, 0f),
                new Vector2(300f, 64f));
            respecLabel = respecButton.GetComponentInChildren<Text>();

            statusText = UiFactory.CreateText(
                root.transform,
                "Status",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.75f, 0.42f));
            UiFactory.Place(statusText.rectTransform, new Vector2(0.5f, 0.025f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 36f));

            var selectNodes = new[] { SkillNode.Shockwave, SkillNode.Dash, SkillNode.Turret };
            for (var i = 0; i < selectNodes.Length; i++)
            {
                var node = selectNodes[i];
                var select = UiFactory.CreateButton(
                    root.transform,
                    $"Select {node}",
                    Localization.SkillName(node),
                    20,
                    () => onSelectActive?.Invoke(node));
                UiFactory.Place(
                    select.GetComponent<RectTransform>(),
                    new Vector2(0.22f + (i * 0.28f), 0.145f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(280f, 54f));

                var icon = UiFactory.CreateImage(
                    select.transform,
                    $"Select {node} Icon",
                    ArtCatalog.Get(SkillIconKey(node)),
                    Color.white);
                UiFactory.Place(
                    icon.rectTransform,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(12f, 0f),
                    new Vector2(40f, 40f));

                var label = select.GetComponentInChildren<Text>();
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 1f);
                label.rectTransform.offsetMin = new Vector2(60f, 0f);
                label.rectTransform.offsetMax = new Vector2(-12f, 0f);
            }

            root.SetActive(false);
            return root;
        }

        private sealed class CharacterNodeRow
        {
            public SkillNode Node;
            public Button Allocate;
            public Text AllocateLabel;
            public Text Name;
            public Text Rank;
            public Text Pips;
            public Text Preview;
        }

        private GameObject BuildArmoryPanel(
            System.Action<WeaponId> onAction,
            System.Action<WeaponUpgradeStat> onUpgrade,
            System.Action<ArmorTier> onArmorAction,
            System.Action onArmorUpgrade,
            System.Action onCharacterSheet,
            System.Action onClose,
            out ArmoryRow[] rows,
            out ArmoryUpgradeRow[] upgradeRows,
            out ArmorRow[] armorRows,
            out GameObject weaponsPanel,
            out GameObject armorPanel,
            out Text supplies,
            out Text upgradeTitle,
            out Text armorUpgradeTitle,
            out Button armorUpgradeButton,
            out Text armorUpgradeLabel,
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

            var skills = UiFactory.CreateButton(root.transform, "Skills", Localization.Get("character.open"), 26, onCharacterSheet);
            UiFactory.Place(skills.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-270f, -24f), new Vector2(220f, 76f));

            weaponsPanel = new GameObject("Weapons Panel", typeof(RectTransform));
            weaponsPanel.transform.SetParent(root.transform, false);
            UiFactory.Stretch(weaponsPanel.GetComponent<RectTransform>());

            armorPanel = new GameObject("Armor Panel", typeof(RectTransform));
            armorPanel.transform.SetParent(root.transform, false);
            UiFactory.Stretch(armorPanel.GetComponent<RectTransform>());
            armorPanel.SetActive(false);

            var weaponsRoot = weaponsPanel;
            var armorRoot = armorPanel;

            var weaponsTab = UiFactory.CreateButton(
                root.transform,
                "Weapons Tab",
                Localization.Get("armory.tab.weapons"),
                24,
                () =>
                {
                    weaponsRoot.SetActive(true);
                    armorRoot.SetActive(false);
                });
            UiFactory.Place(
                weaponsTab.GetComponent<RectTransform>(),
                new Vector2(0.42f, 0.88f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(240f, 56f));

            var armorTab = UiFactory.CreateButton(
                root.transform,
                "Armor Tab",
                Localization.Get("armory.tab.armor"),
                24,
                () =>
                {
                    weaponsRoot.SetActive(false);
                    armorRoot.SetActive(true);
                });
            UiFactory.Place(
                armorTab.GetComponent<RectTransform>(),
                new Vector2(0.58f, 0.88f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(240f, 56f));

            var weapons = WeaponCatalog.All;
            rows = new ArmoryRow[weapons.Count];
            for (var i = 0; i < weapons.Count; i++)
            {
                var weapon = weapons[i];
                var rowImage = UiFactory.CreateImage(
                    weaponsPanel.transform,
                    $"Weapon {weapon.Id}",
                    SpriteFactory.Square,
                    new Color(0.05f, 0.08f, 0.12f, 0.95f));
                UiFactory.Place(
                    rowImage.rectTransform,
                    new Vector2(0.5f, 0.82f - (i * 0.105f)),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(1660f, 84f));

                var icon = UiFactory.CreateImage(
                    rowImage.transform,
                    "Icon",
                    ArtCatalog.Get(WeaponIconKey(weapon.Id)),
                    Color.white);
                UiFactory.Place(
                    icon.rectTransform,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(28f, 0f),
                    new Vector2(76f, 64f));

                var name = UiFactory.CreateText(
                    rowImage.transform,
                    "Name",
                    string.Empty,
                    26,
                    TextAnchor.MiddleLeft,
                    Color.white);
                name.fontStyle = FontStyle.Bold;
                UiFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(260f, 78f));

                var info = UiFactory.CreateText(
                    rowImage.transform,
                    "Info",
                    string.Empty,
                    20,
                    TextAnchor.MiddleLeft,
                    new Color(0.72f, 0.8f, 0.9f));
                UiFactory.Place(info.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(390f, 0f), new Vector2(840f, 78f));

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
                    Info = info,
                    Icon = icon
                };
            }

            upgradeTitle = UiFactory.CreateText(
                weaponsPanel.transform,
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
                    weaponsPanel.transform,
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

            var armors = ArmorCatalog.All;
            armorRows = new ArmorRow[armors.Count];
            for (var i = 0; i < armors.Count; i++)
            {
                var armorTier = armors[i].Tier;
                var rowImage = UiFactory.CreateImage(
                    armorPanel.transform,
                    $"Armor {armorTier}",
                    SpriteFactory.Square,
                    new Color(0.05f, 0.08f, 0.12f, 0.95f));
                UiFactory.Place(
                    rowImage.rectTransform,
                    new Vector2(0.5f, 0.82f - (i * 0.12f)),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(1660f, 90f));

                var name = UiFactory.CreateText(
                    rowImage.transform,
                    "Name",
                    string.Empty,
                    26,
                    TextAnchor.MiddleLeft,
                    Color.white);
                name.fontStyle = FontStyle.Bold;
                UiFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(320f, 82f));

                var info = UiFactory.CreateText(
                    rowImage.transform,
                    "Info",
                    string.Empty,
                    20,
                    TextAnchor.MiddleLeft,
                    new Color(0.72f, 0.8f, 0.9f));
                UiFactory.Place(info.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(350f, 0f), new Vector2(880f, 82f));

                var button = UiFactory.CreateButton(
                    rowImage.transform,
                    "Action",
                    string.Empty,
                    26,
                    () => onArmorAction?.Invoke(armorTier));
                UiFactory.Place(
                    button.GetComponent<RectTransform>(),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(-20f, 0f),
                    new Vector2(300f, 68f));

                armorRows[i] = new ArmorRow
                {
                    Tier = armorTier,
                    Button = button,
                    Label = button.GetComponentInChildren<Text>(),
                    Name = name,
                    Info = info
                };
            }

            armorUpgradeTitle = UiFactory.CreateText(
                armorPanel.transform,
                "Armor Upgrade Title",
                string.Empty,
                28,
                TextAnchor.MiddleCenter,
                new Color(0.4f, 0.9f, 1f));
            UiFactory.Place(
                armorUpgradeTitle.rectTransform,
                new Vector2(0.5f, 0.31f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1400f, 40f));

            armorUpgradeButton = UiFactory.CreateButton(
                armorPanel.transform,
                "Armor Upgrade",
                string.Empty,
                22,
                onArmorUpgrade);
            UiFactory.Place(
                armorUpgradeButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.18f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(500f, 145f));
            armorUpgradeLabel = armorUpgradeButton.GetComponentInChildren<Text>();

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
            public Image Icon;
        }

        private sealed class ArmoryUpgradeRow
        {
            public WeaponUpgradeStat Stat;
            public Button Button;
            public Text Label;
        }

        private sealed class ArmorRow
        {
            public ArmorTier Tier;
            public Button Button;
            public Text Label;
            public Text Name;
            public Text Info;
        }

        private void BindStaticLocalization()
        {
            BindLocalized(FindText(_mainMenu, "Title"), "app.title");
            BindLocalized(FindText(_mainMenu, "Subtitle"), "menu.subtitle");
            BindLocalized(
                FindText(_mainMenu, "Controls"),
                () => Localization.Get(
                    GameSettings.LeftHanded ? "menu.controls.leftHanded" : "menu.controls"));
            BindLocalized(_buildLabel, () => Localization.Format("menu.build", BuildInfo.Version, BuildInfo.Commit));
            BindLocalizedButton(_mainMenu, "Start", "menu.start");
            BindLocalizedButton(_mainMenu, "Armory", "menu.armory");
            BindLocalizedButton(_mainMenu, "Skills", "character.open");
            BindLocalizedButton(_mainMenu, "Leaderboard", "leaderboard.open");
            BindLocalizedButton(_mainMenu, "Account", "account.open");
            BindLocalizedButton(_mainMenu, "Settings", "settings.open");
            BindLocalizedButton(_mainMenu, "How To Play", "help.open");

            BindLocalized(FindText(_pausePanel, "Title"), "pause.title");
            BindLocalizedButton(_pausePanel, "Resume", "pause.resume");
            BindLocalizedButton(_pausePanel, "Armory", "pause.armory");
            BindLocalizedButton(_pausePanel, "Skills", "character.open");
            BindLocalizedButton(_pausePanel, "Menu", "pause.menu");
            BindLocalizedButton(_pausePanel, "Settings", "settings.open");

            BindLocalized(FindText(_hud, "Boss Name"), "boss.name");
            BindLocalizedButton(_hud, "Reload Button", "hud.reload");
            BindLocalizedButton(_hud, "Switch Weapon", "hud.switch");
            BindLocalizedButton(_hud, "Fire Button", "hud.fire");
            BindLocalized(
                FindText(_hud, "Controls Hint"),
                () => Localization.Get(
                    GameSettings.LeftHanded ? "menu.controls.leftHanded" : "menu.controls"));

            BindLocalized(FindText(_gameOverPanel, "Title"), "gameover.title");
            BindLocalizedButton(_gameOverPanel, "Restart", "gameover.restart");
            BindLocalizedButton(_gameOverPanel, "Menu", "gameover.menu");

            BindLocalized(FindText(_victoryPanel, "Title"), "victory.title");
            BindLocalized(FindText(_victoryPanel, "Subtitle"), "victory.subtitle");
            BindLocalizedButton(_victoryPanel, "Next Run", "victory.next");
            BindLocalizedButton(_victoryPanel, "Menu", "victory.menu");
            BindLocalizedButton(_victoryPanel, "Leaderboard", "leaderboard.open");

            BindLocalized(FindText(_roundEndPanel, "Hint"), "round.end.hint");
            BindLocalizedButton(_roundEndPanel, "Skills", "character.open");

            BindLocalized(FindText(_armoryPanel, "Title"), "armory.title");
            BindLocalizedButton(_armoryPanel, "Close", "armory.close");
            BindLocalizedButton(_armoryPanel, "Skills", "character.open");
            BindLocalizedButton(_armoryPanel, "Weapons Tab", "armory.tab.weapons");
            BindLocalizedButton(_armoryPanel, "Armor Tab", "armory.tab.armor");

            BindLocalized(FindText(_characterSheet, "Title"), "character.title");
            BindLocalizedButton(_characterSheet, "Close", "character.close");
            BindLocalizedButton(_characterSheet, "Tab Strength", "character.branch.strength");
            BindLocalizedButton(_characterSheet, "Tab Agility", "character.branch.agility");
            BindLocalizedButton(_characterSheet, "Tab Survival", "character.branch.survival");
            var branchKeys = new[]
            {
                "character.branch.strength",
                "character.branch.agility",
                "character.branch.survival"
            };
            for (var i = 0; i < _characterBranchPanels.Length && i < branchKeys.Length; i++)
            {
                BindLocalized(FindText(_characterBranchPanels[i], "Branch Title"), branchKeys[i]);
            }

            BindLocalized(FindText(_accountPanel, "Title"), "account.title");
            BindLocalizedButton(_accountPanel, "Register", "account.register");
            BindLocalizedButton(_accountPanel, "Login", "account.login");
            BindLocalizedButton(_accountPanel, "Logout", "account.logout");
            BindLocalizedButton(_accountPanel, "Close", "account.close");
            BindLocalizedButton(_accountPanel, "Cloud Backup", "cloud.backup");
            BindLocalizedButton(_accountPanel, "Cloud Restore", "cloud.restore");
            BindLocalized(_accountUsername.placeholder as Text, "account.username");
            BindLocalized(_accountPassword.placeholder as Text, "account.password");

            BindLocalized(FindText(_cloudConflictPanel, "Title"), "cloud.conflict.title");
            BindLocalizedButton(_cloudConflictPanel, "Use Cloud", "cloud.useCloud");
            BindLocalizedButton(_cloudConflictPanel, "Keep Local", "cloud.keepLocal");

            BindLocalizedButton(_confirmationPanel, "Cancel", "confirm.cancel");

            BindLocalized(FindText(_leaderboardPanel, "Title"), "leaderboard.title");
            BindLocalizedButton(_leaderboardPanel, "Close", "account.close");

            BindLocalized(FindText(_settingsPanel, "Title"), "settings.title");
            BindLocalized(FindText(_settingsPanel, "Audio Section Title"), "settings.audio.title");
            BindLocalized(FindText(_settingsPanel, "Language Section Title"), "settings.language.title");
            BindLocalized(FindText(_settingsPanel, "Controls Section Title"), "settings.controls.title");
            BindLocalized(FindText(_settingsPanel, "Accessibility Section Title"), "settings.accessibility.title");
            BindLocalized(FindText(_settingsPanel, "Sound Label"), "settings.audio.mute");
            BindLocalized(FindText(_settingsPanel, "Master Volume Label"), "settings.audio.master");
            BindLocalized(FindText(_settingsPanel, "Effects Volume Label"), "settings.audio.effects");
            BindLocalized(FindText(_settingsPanel, "Language Current Label"), "settings.language.current");
            BindLocalized(FindText(_settingsPanel, "Left Handed Label"), "settings.controls.leftHanded");
            BindLocalized(FindText(_settingsPanel, "Joystick Size Label"), "settings.controls.joystickSize");
            BindLocalized(FindText(_settingsPanel, "Joystick Opacity Label"), "settings.controls.joystickOpacity");
            BindLocalized(FindText(_settingsPanel, "Aim Assist Label"), "settings.controls.aimAssist");
            BindLocalized(FindText(_settingsPanel, "Auto Fire Label"), "settings.controls.autoFire");
            BindLocalized(FindText(_settingsPanel, "Reduced Motion Label"), "settings.accessibility.reducedMotion");
            BindLocalized(FindText(_settingsPanel, "Haptics Label"), "settings.accessibility.haptics");
            BindLocalized(FindText(_settingsPanel, "Colorblind Label"), "settings.accessibility.colorblind");
            BindLocalizedButton(_settingsPanel, "System Language", "settings.language.system");
            BindLocalizedButton(_settingsPanel, "Reset", "settings.reset");
            BindLocalizedButton(_settingsPanel, "How To Play", "help.open");
            BindLocalizedButton(_settingsPanel, "Close", "settings.close");

            BindLocalized(FindText(_helpPanel, "Title"), "help.title");
            BindLocalized(FindText(_helpPanel, "Body"), "help.body");
            BindLocalizedButton(_helpPanel, "Close", "help.close");
        }

        private static void BindLocalized(Text text, string key)
        {
            if (text == null)
            {
                return;
            }

            var binder = text.gameObject.GetComponent<LocalizedText>() ??
                         text.gameObject.AddComponent<LocalizedText>();
            binder.Bind(key);
        }

        private static void BindLocalized(Text text, System.Func<string> provider)
        {
            if (text == null)
            {
                return;
            }

            var binder = text.gameObject.GetComponent<LocalizedText>() ??
                         text.gameObject.AddComponent<LocalizedText>();
            binder.Bind(provider);
        }

        private static void BindLocalizedButton(GameObject root, string buttonName, string key)
        {
            var button = FindGameObject(root, buttonName);
            if (button != null)
            {
                BindLocalized(button.GetComponentInChildren<Text>(), key);
            }
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

        private static Image FindImage(GameObject root, string name)
        {
            var images = root.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image.name == name)
                {
                    return image;
                }
            }

            return null;
        }

        private static GameObject FindGameObject(GameObject root, string name)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in transforms)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }

    internal static class UiLayoutAudit
    {
        public static string[] FindOverflows(RectTransform root)
        {
            var overflows = new HashSet<string>();
            var texts = new List<TextItem>();
            foreach (var text in root.GetComponentsInChildren<Text>(false))
            {
                if (text == null || !text.isActiveAndEnabled || string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                var rect = ToScreenRect(text.rectTransform);
                var fontScale = EffectiveFontScale(text);
                var preferredWidth = Mathf.Max(rect.width, text.preferredWidth * fontScale);
                var preferredHeight = Mathf.Max(rect.height, text.preferredHeight * fontScale);
                if (preferredWidth > rect.width + 1f || preferredHeight > rect.height + 1f)
                {
                    overflows.Add(
                        $"{text.name}: '{text.text}' preferred={preferredWidth:0}x{preferredHeight:0} " +
                        $"rect={rect.width:0}x{rect.height:0}");
                }

                texts.Add(new TextItem
                {
                    Transform = text.rectTransform,
                    Name = text.name,
                    Content = Expand(rect, preferredWidth, preferredHeight)
                });
            }

            var controls = new List<RectTransform>();
            foreach (var selectable in root.GetComponentsInChildren<Selectable>(false))
            {
                if (selectable.transform is RectTransform rectTransform)
                {
                    controls.Add(rectTransform);
                }
            }

            for (var i = 0; i < texts.Count; i++)
            {
                var text = texts[i];
                for (var j = 0; j < texts.Count; j++)
                {
                    if (i == j || IsRelated(text.Transform, texts[j].Transform))
                    {
                        continue;
                    }

                    if (Overlaps(text.Content, texts[j].Content))
                    {
                        overflows.Add(
                            $"overlap: '{text.Name}' <-> '{texts[j].Name}'");
                    }
                }

                for (var j = 0; j < controls.Count; j++)
                {
                    var control = controls[j];
                    if (IsRelated(text.Transform, control))
                    {
                        continue;
                    }

                    if (Overlaps(text.Content, ToScreenRect(control)))
                    {
                        overflows.Add(
                            $"overlap: '{text.Name}' <-> '{control.name}'");
                    }
                }
            }

            return new List<string>(overflows).ToArray();
        }

        private static Rect Expand(Rect rect, float width, float height)
        {
            var center = rect.center;
            return new Rect(
                center.x - (width * 0.5f),
                center.y - (height * 0.5f),
                width,
                height);
        }

        private static float EffectiveFontScale(Text text)
        {
            if (!text.resizeTextForBestFit || text.fontSize <= 0)
            {
                return 1f;
            }

            var used = text.cachedTextGenerator.fontSizeUsedForBestFit;
            return used > 0 ? Mathf.Clamp01(used / (float)text.fontSize) : 1f;
        }

        private static bool Overlaps(Rect first, Rect second)
        {
            return first.xMin < second.xMax - 1f &&
                   first.xMax > second.xMin + 1f &&
                   first.yMin < second.yMax - 1f &&
                   first.yMax > second.yMin + 1f;
        }

        private static bool IsRelated(Transform first, Transform second)
        {
            return first == second ||
                   first.IsChildOf(second) ||
                   second.IsChildOf(first);
        }

        private static Rect ToScreenRect(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var min = corners[0];
            var max = corners[0];
            for (var i = 1; i < corners.Length; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private sealed class TextItem
        {
            public RectTransform Transform;
            public string Name;
            public Rect Content;
        }
    }
}
