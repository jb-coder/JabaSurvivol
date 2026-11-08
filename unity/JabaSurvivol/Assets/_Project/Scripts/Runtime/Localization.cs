using System;
using System.Collections.Generic;
using System.Globalization;
using JabaSurvivol.Core;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// English-only string table for the walking skeleton. Keys are stable so
    /// the localization phase can replace the storage without touching UI code.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, string> Entries = new(StringComparer.Ordinal)
        {
            ["app.title"] = "JABASURVIVOL",
            ["menu.subtitle"] = "SURVIVE THE NIGHT. KEEP EVERYTHING.",
            ["menu.start"] = "START RUN",
            ["menu.armory"] = "ARMORY",
            ["menu.progress"] = "SUPPLIES {0}    LEVEL {1}    BEST ROUND {2}",
            ["menu.controls"] = "LEFT STICK MOVE    RIGHT STICK AIM & FIRE",
            ["account.open"] = "ACCOUNT",
            ["account.title"] = "ACCOUNT",
            ["account.username"] = "USERNAME",
            ["account.password"] = "PASSWORD",
            ["account.register"] = "REGISTER",
            ["account.login"] = "LOGIN",
            ["account.logout"] = "LOG OUT",
            ["account.close"] = "CLOSE",
            ["account.state.signedout"] = "NOT SIGNED IN - LOCAL PLAY ONLY",
            ["account.state.signingin"] = "SIGNING IN...",
            ["account.state.signedin"] = "SIGNED IN AS {0}",
            ["account.state.offline"] = "OFFLINE - LOCAL PLAY ONLY",
            ["account.welcome"] = "WELCOME {0}",
            ["account.error.username"] = "USERNAME MUST BE AT LEAST 3 CHARACTERS",
            ["account.error.password"] = "PASSWORD MUST BE AT LEAST 8 CHARACTERS",
            ["account.error.taken"] = "USERNAME IS NOT AVAILABLE",
            ["account.error.invalid"] = "INVALID USERNAME OR PASSWORD",
            ["account.error.network"] = "SERVER UNAVAILABLE - TRY AGAIN LATER",
            ["account.error.generic"] = "THE REQUEST COULD NOT BE COMPLETED",
            ["hud.health"] = "HP {0}/{1}",
            ["hud.armor"] = "ARMOR {0}/{1}",
            ["hud.round"] = "ROUND {0}/{1}",
            ["hud.enemies"] = "ENEMIES REMAINING {0}",
            ["hud.supplies"] = "SUPPLIES {0}",
            ["hud.level"] = "LEVEL {0}   XP {1}/{2}   POINTS {3}",
            ["hud.levelup"] = "LEVEL UP! LEVEL {0}",
            ["hud.kills"] = "KILLS {0}",
            ["hud.reload"] = "RELOAD",
            ["hud.switch"] = "SWITCH",
            ["hud.ammo"] = "AMMO {0}/{1}   RESERVE {2}",
            ["hud.ammo.infinite"] = "AMMO {0}/{1}   RESERVE {2}",
            ["hud.reloading"] = "RELOADING",
            ["hud.skill"] = "SKILL",
            ["hud.skill.locked"] = "LOCKED",
            ["hud.elite"] = "ELITE ROUND - REWARDS x2",
            ["hud.boss"] = "BOSS",
            ["hud.boss.health"] = "BOSS {0}/{1}",
            ["boss.name"] = "THE WARDEN",
            ["hud.pause"] = "II",
            ["pause.title"] = "PAUSED",
            ["pause.resume"] = "RESUME",
            ["pause.menu"] = "MAIN MENU",
            ["pause.armory"] = "ARMORY",
            ["armory.title"] = "ARMORY",
            ["armory.close"] = "CLOSE",
            ["armory.supplies"] = "SUPPLIES {0}",
            ["armory.buy"] = "BUY {0}",
            ["armory.equip"] = "EQUIP",
            ["armory.equipped"] = "EQUIPPED",
            ["armory.purchased"] = "{0} PURCHASED AND EQUIPPED",
            ["armory.equipped.status"] = "{0} EQUIPPED",
            ["armory.insufficient"] = "NOT ENOUGH SUPPLIES",
            ["armory.stats"] = "DMG {0}   RATE {1}/s   MAG {2}   RANGE {3}   RESERVE {4}",
            ["armory.reserve.infinite"] = "INF",
            ["armory.upgrade.title"] = "UPGRADES - {0}",
            ["armory.upgrade.card"] = "{0}\nTIER {1}/{2}\n+{4}% NEXT\n{3} SUPPLIES",
            ["armory.upgrade.max"] = "{0}\nMAX TIER {1}",
            ["armory.upgrade.purchased"] = "{0} TIER {1}",
            ["armory.upgrade.maxed"] = "MAX TIER REACHED",
            ["weapon.upgrade.damage"] = "DAMAGE",
            ["weapon.upgrade.firerate"] = "FIRE RATE",
            ["weapon.upgrade.magazine"] = "MAGAZINE",
            ["weapon.upgrade.reload"] = "RELOAD",
            ["armory.tab.weapons"] = "WEAPONS",
            ["armory.tab.armor"] = "ARMOR",
            ["armory.armor.none"] = "NO ARMOR",
            ["armory.armor.light"] = "LIGHT ARMOR",
            ["armory.armor.medium"] = "MEDIUM ARMOR",
            ["armory.armor.heavy"] = "HEAVY ARMOR",
            ["armory.armor.absorb"] = "ABSORB {0}",
            ["armory.armor.row"] = "ABSORB {0}   TIER {1}/{2}",
            ["armory.armor.upgrade.title"] = "ARMOR UPGRADE - {0}",
            ["armory.armor.upgrade.card"] = "ABSORB +{0}%\nTIER {1}/{2}\n{3} SUPPLIES",
            ["armory.armor.upgrade.max"] = "ABSORB +{0}% PER TIER\nMAX TIER {1}",
            ["armory.armor.upgrade.none"] = "EQUIP ARMOR TO UPGRADE",
            ["armory.armor.unlock"] = "UNLOCK {0}",
            ["armory.armor.unlock.free"] = "UNLOCK",
            ["armory.armor.equip"] = "EQUIP",
            ["armory.armor.equipped"] = "EQUIPPED",
            ["armory.armor.purchased"] = "{0} UNLOCKED AND EQUIPPED",
            ["armory.armor.equipped.status"] = "{0} EQUIPPED",
            ["armory.armor.upgraded"] = "ARMOR TIER {0}",
            ["armory.armor.maxed"] = "MAX ARMOR TIER",
            ["character.title"] = "CHARACTER",
            ["character.close"] = "CLOSE",
            ["character.open"] = "SKILLS",
            ["character.level"] = "LEVEL {0}   XP {1}/{2}",
            ["character.points"] = "SKILL POINTS {0}",
            ["character.rank"] = "RANK {0}/{1}",
            ["character.allocate"] = "+",
            ["character.max"] = "MAX",
            ["character.respec"] = "RESPEC",
            ["character.stats"] = "HP {0}   DAMAGE x{1}   MOVE {2}   ARMOR {3}   CRIT {4}   HEADSHOT {5}   RELOAD x{6}   AMMO x{7}",
            ["character.respec.done"] = "RESPEC COMPLETE: {0} POINTS RETURNED",
            ["character.respec.empty"] = "NO ALLOCATED POINTS TO RESPEC",
            ["character.respec.blocked"] = "RESPEC IS ONLY AVAILABLE BETWEEN RUNS",
            ["character.status.allocated"] = "{0} RANK {1}",
            ["character.status.nopoints"] = "NO SKILL POINTS AVAILABLE",
            ["character.status.maxrank"] = "MAX RANK",
            ["character.allocation.blocked"] = "ALLOCATION REQUIRES THE ARMORY, PAUSE MENU, OR ROUND END",
            ["character.active.selected"] = "{0} (ACTIVE)",
            ["character.active.status"] = "ACTIVE SKILL: {0}",
            ["character.active.blocked"] = "ALLOCATE A RANK TO SELECT THIS SKILL",
            ["synergy.berserker.name"] = "BERSERKER",
            ["synergy.ghost.name"] = "GHOST",
            ["synergy.bastion.name"] = "BASTION",
            ["synergy.active"] = "{0} ACTIVE",
            ["synergy.inactive"] = "{0} {1} {2}/{3} + {4} {5}/{6}",
            ["character.branch.strength"] = "STRENGTH",
            ["character.branch.agility"] = "AGILITY",
            ["character.branch.survival"] = "SURVIVAL",
            ["skill.strength.name"] = "STRENGTH",
            ["skill.vitality.name"] = "VITALITY",
            ["skill.shockwave.name"] = "SHOCKWAVE",
            ["skill.speed.name"] = "SPEED",
            ["skill.precision.name"] = "PRECISION",
            ["skill.dash.name"] = "DASH",
            ["skill.endurance.name"] = "ENDURANCE",
            ["skill.engineering.name"] = "ENGINEERING",
            ["skill.turret.name"] = "TURRET",
            ["skill.strength.preview"] = "DAMAGE x{0} -> x{1}",
            ["skill.vitality.preview"] = "MAX HP {0} -> {1}",
            ["skill.speed.preview"] = "MOVE SPEED {0} -> {1}",
            ["skill.precision.preview"] = "CRIT {0} -> {1}   HEADSHOT {2} -> {3}",
            ["skill.endurance.preview"] = "ARMOR {0} -> {1}",
            ["skill.engineering.preview"] = "RELOAD x{0} -> x{1}   AMMO x{2} -> x{3}",
            ["skill.shockwave.preview"] = "DAMAGE {0} -> {1}   RADIUS {2} -> {3}   COOLDOWN {4} -> {5}",
            ["skill.dash.preview"] = "DISTANCE {0} -> {1}   COOLDOWN {2} -> {3}",
            ["skill.turret.preview"] = "DAMAGE {0} -> {1}   DURATION {2} -> {3}   COOLDOWN {4} -> {5}",
            ["weapon.pistol.name"] = "PISTOL",
            ["weapon.smg.name"] = "SMG",
            ["weapon.shotgun.name"] = "SHOTGUN",
            ["weapon.rifle.name"] = "RIFLE",
            ["weapon.lmg.name"] = "LMG",
            ["zombie.walker.name"] = "WALKER",
            ["zombie.runner.name"] = "RUNNER",
            ["zombie.brute.name"] = "BRUTE",
            ["gameover.title"] = "YOU DIED",
            ["gameover.summary"] = "ROUND {0}    KILLS {1}\nSUPPLIES EARNED {2}",
            ["gameover.restart"] = "RESTART",
            ["gameover.menu"] = "MAIN MENU",
            ["victory.title"] = "VICTORY",
            ["victory.subtitle"] = "THE WARDEN HAS FALLEN",
            ["victory.summary"] = "ROUND {0}    KILLS {1}\nSUPPLIES EARNED {2}    XP EARNED {3}",
            ["victory.next"] = "NEXT RUN",
            ["victory.menu"] = "MAIN MENU",
            ["round.end.title"] = "ROUND {0} CLEARED",
            ["round.end.hint"] = "TAP A REWARD OR PRESS 1, 2, OR 3",
            ["round.end.summary"] = "KILLS {0}    SUPPLIES {1}    XP {2}",
            ["reward.supplies.card"] = "SUPPLIES\n+{0}",
            ["reward.xp.card"] = "XP\n+{0}",
            ["reward.ammo.card"] = "AMMO CRATE\n+{0}",
            ["reward.armor.card"] = "ARMOR PLATE\n+{0}"
        };

        public static string Get(string key)
        {
            return Entries.TryGetValue(key, out var value) ? value : key;
        }

        public static string Format(string key, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), arguments);
        }

        public static string WeaponNameKey(WeaponId weaponId)
        {
            return weaponId switch
            {
                WeaponId.Smg => "weapon.smg.name",
                WeaponId.Shotgun => "weapon.shotgun.name",
                WeaponId.Rifle => "weapon.rifle.name",
                WeaponId.Lmg => "weapon.lmg.name",
                _ => "weapon.pistol.name"
            };
        }

        public static string WeaponName(WeaponId weaponId)
        {
            return Get(WeaponNameKey(weaponId));
        }

        public static string WeaponUpgradeStatKey(WeaponUpgradeStat stat)
        {
            return stat switch
            {
                WeaponUpgradeStat.FireRate => "weapon.upgrade.firerate",
                WeaponUpgradeStat.MagazineSize => "weapon.upgrade.magazine",
                WeaponUpgradeStat.ReloadSpeed => "weapon.upgrade.reload",
                _ => "weapon.upgrade.damage"
            };
        }

        public static string WeaponUpgradeStatName(WeaponUpgradeStat stat)
        {
            return Get(WeaponUpgradeStatKey(stat));
        }

        public static string ArmorNameKey(ArmorTier tier)
        {
            return tier switch
            {
                ArmorTier.Light => "armory.armor.light",
                ArmorTier.Medium => "armory.armor.medium",
                ArmorTier.Heavy => "armory.armor.heavy",
                _ => "armory.armor.none"
            };
        }

        public static string ArmorName(ArmorTier tier)
        {
            return Get(ArmorNameKey(tier));
        }

        public static string BranchNameKey(SkillBranch branch)
        {
            return branch switch
            {
                SkillBranch.Agility => "character.branch.agility",
                SkillBranch.Survival => "character.branch.survival",
                _ => "character.branch.strength"
            };
        }

        public static string BranchName(SkillBranch branch)
        {
            return Get(BranchNameKey(branch));
        }

        public static string SkillNameKey(SkillNode node)
        {
            return node switch
            {
                SkillNode.Vitality => "skill.vitality.name",
                SkillNode.Shockwave => "skill.shockwave.name",
                SkillNode.Speed => "skill.speed.name",
                SkillNode.Precision => "skill.precision.name",
                SkillNode.Dash => "skill.dash.name",
                SkillNode.Endurance => "skill.endurance.name",
                SkillNode.Engineering => "skill.engineering.name",
                SkillNode.Turret => "skill.turret.name",
                _ => "skill.strength.name"
            };
        }

        public static string SkillName(SkillNode node)
        {
            return Get(SkillNameKey(node));
        }
    }
}
