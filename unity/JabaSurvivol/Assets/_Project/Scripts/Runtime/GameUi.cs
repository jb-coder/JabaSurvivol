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
        private readonly Text _ammoText;
        private readonly Text _reloadText;
        private readonly Image _reloadFill;
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
        private readonly Text _characterStatusText;
        private readonly Button _characterRespecButton;
        private readonly Text _characterRespecLabel;
        private readonly Button[] _characterTabs;
        private readonly Text[] _characterTabLabels;
        private readonly GameObject[] _characterBranchPanels;
        private readonly CharacterNodeRow[] _characterNodeRows;
        private readonly Button[] _rewardButtons;
        private readonly Text[] _rewardLabels;
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
        private int _lastMagazine = int.MinValue;
        private int _lastMagazineSize = int.MinValue;
        private int _lastReserve = int.MinValue;
        private bool _lastInfiniteReserve;
        private bool _lastReloading;
        private bool _characterSheetTablet;
        private int _activeCharacterBranch;

        public GameUi(
            Transform parent,
            System.Action onStart,
            System.Action onRestart,
            System.Action onMenu,
            System.Action onPause,
            System.Action onResume,
            System.Action onReload,
            System.Action onSwitchWeapon,
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
            System.Action onArmoryClose)
        {
            var canvasObject = UiFactory.CreateCanvas("JabaSurvivol UI", parent);

            var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasObject.transform, false);
            _safeArea = safeAreaObject.GetComponent<RectTransform>();
            UiFactory.Stretch(_safeArea);
            safeAreaObject.AddComponent<SafeAreaFitter>();

            _mainMenu = BuildMainMenu(onStart, onArmory, onCharacterSheet);
            _hud = BuildHud(onPause, onReload, onSwitchWeapon, out _healthText, out _armorText, out _healthFill, out _roundText, out _enemiesText, out _killsText, out _suppliesText, out _levelText, out _moveBase, out _moveKnob, out _aimBase, out _aimKnob, out _moveBaseImage, out _aimBaseImage);
            _roundModifierText = FindText(_hud, "Round Modifier");
            _weaponText = FindText(_hud, "Weapon");
            _ammoText = FindText(_hud, "Ammo");
            _reloadText = FindText(_hud, "Reload Status");
            _reloadFill = FindImage(_hud, "Reload Fill");
            _bossPanel = FindGameObject(_hud, "Boss Panel");
            _bossNameText = FindText(_hud, "Boss Name");
            _bossHealthText = FindText(_hud, "Boss Health Text");
            _bossHealthFill = FindImage(_hud, "Boss Health Fill");
            _pausePanel = BuildPausePanel(onResume, onMenu, onArmory, onCharacterSheet);
            _gameOverPanel = BuildGameOverPanel(onRestart, onMenu, out _gameOverSummary);
            _victoryPanel = BuildVictoryPanel(onRestart, onMenu, out _victorySummary);
            _roundEndPanel = BuildRoundEndPanel(
                onRewardSelected,
                onCharacterSheet,
                out _roundEndTitle,
                out _roundEndSummary,
                out _rewardButtons,
                out _rewardLabels);
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
            ShowMainMenu(new Profile());
        }

        public void ShowMainMenu(Profile profile)
        {
            _mainMenu.SetActive(true);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
            _gameOverPanel.SetActive(false);
            HideVictory();
            HideBoss();
            HideRoundEnd();
            HideArmory();
            HideCharacterSheet();
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
            HideVictory();
            HideBoss();
            HideArmory();
            HideCharacterSheet();
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
            _lastWeaponName = string.Empty;
            _lastMagazine = int.MinValue;
            _lastMagazineSize = int.MinValue;
            _lastReserve = int.MinValue;
            _lastInfiniteReserve = false;
            _lastReloading = false;
            HideLevelUp();
            HideRoundEnd();
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

        public bool RoundModifierVisible => _roundModifierText != null &&
                                            !string.IsNullOrEmpty(_roundModifierText.text);

        /// <summary>
        /// Updates the equipped weapon, magazine, reserve, and reload state.
        /// Values are cached so steady-state frames do not allocate.
        /// </summary>
        public void UpdateWeaponHud(
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

            if (!string.Equals(weaponName, _lastWeaponName, System.StringComparison.Ordinal) ||
                magazine != _lastMagazine ||
                magazineSize != _lastMagazineSize ||
                reserve != _lastReserve ||
                infiniteReserve != _lastInfiniteReserve)
            {
                _lastWeaponName = weaponName;
                _lastMagazine = magazine;
                _lastMagazineSize = magazineSize;
                _lastReserve = reserve;
                _lastInfiniteReserve = infiniteReserve;
                _weaponText.text = weaponName;
                _ammoText.text = Localization.Format(
                    infiniteReserve ? "hud.ammo.infinite" : "hud.ammo",
                    magazine,
                    magazineSize,
                    infiniteReserve
                        ? Localization.Get("armory.reserve.infinite")
                        : reserve.ToString(CultureInfo.InvariantCulture));
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
            _pausePanel.SetActive(visible);
        }

        public void ShowGameOver(int round, int kills, int suppliesEarned)
        {
            _mainMenu.SetActive(false);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
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
            _mainMenu.SetActive(false);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
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

        public void ShowArmory(Profile profile)
        {
            _mainMenu.SetActive(false);
            _hud.SetActive(false);
            _pausePanel.SetActive(false);
            _gameOverPanel.SetActive(false);
            HideVictory();
            HideRoundEnd();
            HideCharacterSheet();
            _armoryPanel.SetActive(true);
            _armoryWeaponsPanel.SetActive(true);
            _armoryArmorPanel.SetActive(false);
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

        public bool CharacterSheetVisible => _characterSheet != null && _characterSheet.activeSelf;

        public bool LevelUpVisible => _levelUpText != null && _levelUpText.gameObject.activeSelf;

        public void ShowCharacterSheet(Profile profile, bool canAllocate, bool canRespec, WeaponStats weapon, ArmorStats armor)
        {
            _characterSheetTablet = IsTabletLayout();
            _characterSheet.SetActive(true);
            ApplyCharacterSheetLayout();
            SetActiveCharacterTab(_activeCharacterBranch);
            RefreshCharacterSheet(profile, canAllocate, canRespec, weapon, armor);
            SetCharacterSheetStatus(
                canAllocate ? string.Empty : Localization.Get("character.allocation.blocked"));
        }

        public void HideCharacterSheet()
        {
            if (_characterSheet != null)
            {
                _characterSheet.SetActive(false);
            }
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

        private static string FormatCharacterStats(CharacterStats stats)
        {
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

        private GameObject BuildMainMenu(System.Action onStart, System.Action onArmory, System.Action onCharacterSheet)
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
            UiFactory.Place(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 96f));

            var armory = UiFactory.CreateButton(root.transform, "Armory", Localization.Get("menu.armory"), 32, onArmory);
            UiFactory.Place(armory.GetComponent<RectTransform>(), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 78f));

            var character = UiFactory.CreateButton(root.transform, "Skills", Localization.Get("character.open"), 32, onCharacterSheet);
            UiFactory.Place(character.GetComponent<RectTransform>(), new Vector2(0.5f, 0.21f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 78f));

            var controls = UiFactory.CreateText(root.transform, "Controls", Localization.Get("menu.controls"), 24, TextAnchor.MiddleCenter, new Color(0.55f, 0.62f, 0.72f));
            UiFactory.Place(controls.rectTransform, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 50f));

            return root;
        }

        private GameObject BuildHud(
            System.Action onPause,
            System.Action onReload,
            System.Action onSwitchWeapon,
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

            armorText = UiFactory.CreateText(root.transform, "Armor", string.Empty, 20, TextAnchor.MiddleLeft, new Color(0.45f, 0.75f, 1f));
            UiFactory.Place(armorText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -86f), new Vector2(440f, 34f));

            roundText = UiFactory.CreateText(root.transform, "Round", string.Empty, 36, TextAnchor.MiddleCenter, new Color(0.9f, 0.95f, 1f));
            roundText.fontStyle = FontStyle.Bold;
            UiFactory.Place(roundText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(420f, 60f));

            enemiesText = UiFactory.CreateText(root.transform, "Enemies", string.Empty, 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.72f, 0.42f));
            UiFactory.Place(enemiesText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(420f, 40f));

            var roundModifier = UiFactory.CreateText(root.transform, "Round Modifier", string.Empty, 24, TextAnchor.MiddleCenter, new Color(1f, 0.62f, 0.25f));
            roundModifier.fontStyle = FontStyle.Bold;
            UiFactory.Place(roundModifier.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(700f, 40f));

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
            UiFactory.Place(killsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(420f, 40f));

            suppliesText = UiFactory.CreateText(root.transform, "Supplies", string.Empty, 28, TextAnchor.MiddleRight, new Color(0.55f, 0.95f, 0.75f));
            UiFactory.Place(suppliesText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -30f), new Vector2(420f, 44f));

            levelText = UiFactory.CreateText(root.transform, "Level", string.Empty, 20, TextAnchor.MiddleRight, new Color(0.6f, 0.68f, 0.78f));
            UiFactory.Place(levelText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-140f, -78f), new Vector2(620f, 40f));

            var pause = UiFactory.CreateButton(root.transform, "Pause", Localization.Get("hud.pause"), 28, onPause);
            UiFactory.Place(pause.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(84f, 84f));

            var weaponText = UiFactory.CreateText(root.transform, "Weapon", string.Empty, 22, TextAnchor.MiddleLeft, new Color(0.45f, 0.9f, 1f));
            weaponText.fontStyle = FontStyle.Bold;
            UiFactory.Place(weaponText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -128f), new Vector2(340f, 28f));

            var ammoText = UiFactory.CreateText(root.transform, "Ammo", string.Empty, 24, TextAnchor.MiddleLeft, new Color(0.92f, 0.95f, 1f));
            ammoText.fontStyle = FontStyle.Bold;
            UiFactory.Place(ammoText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -158f), new Vector2(340f, 32f));

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

        private GameObject BuildPausePanel(System.Action onResume, System.Action onMenu, System.Action onArmory, System.Action onCharacterSheet)
        {
            var panel = UiFactory.CreatePanel(_safeArea, "Pause Panel", new Color(0.01f, 0.02f, 0.03f, 0.9f));
            var root = panel.gameObject;

            var title = UiFactory.CreateText(root.transform, "Title", Localization.Get("pause.title"), 64, TextAnchor.MiddleCenter, new Color(0.5f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 100f));

            var resume = UiFactory.CreateButton(root.transform, "Resume", Localization.Get("pause.resume"), 32, onResume);
            UiFactory.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.51f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 84f));

            var armory = UiFactory.CreateButton(root.transform, "Armory", Localization.Get("pause.armory"), 28, onArmory);
            UiFactory.Place(armory.GetComponent<RectTransform>(), new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 74f));

            var character = UiFactory.CreateButton(root.transform, "Skills", Localization.Get("character.open"), 28, onCharacterSheet);
            UiFactory.Place(character.GetComponent<RectTransform>(), new Vector2(0.5f, 0.29f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 74f));

            var menu = UiFactory.CreateButton(root.transform, "Menu", Localization.Get("pause.menu"), 28, onMenu);
            UiFactory.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 74f));

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

        private GameObject BuildVictoryPanel(System.Action onNextRun, System.Action onMenu, out Text summary)
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
            UiFactory.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.21f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 88f));

            root.SetActive(false);
            return root;
        }

        private GameObject BuildRoundEndPanel(            System.Action<int> onSelect,
            System.Action onCharacterSheet,
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
}
