using System.Globalization;
using System.Text;
using JabaSurvivol.Core;

var report = BalanceSimulation.Run();
var builder = new StringBuilder();

builder.AppendLine("# JabaSurvivol balance simulation");
builder.AppendLine();
builder.AppendLine($"- Seed: {report.Seed}");
builder.AppendLine($"- Rounds: {report.Rounds.Count}");
builder.AppendLine($"- Total supplies earned: {report.TotalSuppliesEarned}");
builder.AppendLine($"- Total supplies spent: {report.TotalSuppliesSpent}");
builder.AppendLine($"- Supplies remaining: {report.TotalSupplies}");
builder.AppendLine($"- Total XP: {report.TotalXp}");
builder.AppendLine($"- Final level: {report.FinalLevel}");
builder.AppendLine($"- Available skill points: {report.FinalSkillPoints}");
builder.AppendLine($"- Outliers: {report.Outliers.Count}");
builder.AppendLine();

builder.AppendLine("## Rounds");
builder.AppendLine();
builder.AppendLine("| Round | Budget | Mix W/R/B | Max HP | Max speed | Max damage | Weapon | DMG/shot | Shots/enemy | Total shots | Kill s | Round s | Ammo s | Reloads | Switches | Supplies | XP | Reward | Remaining | Spent | Level | SP |");
builder.AppendLine("|---:|---:|---|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|");

foreach (var round in report.Rounds)
{
    builder.AppendLine(
        $"| {round.Round} | {round.SpawnBudget} | {round.Content.Walkers}/{round.Content.Runners}/{round.Content.Brutes} | " +
        $"{Format(round.ZombieHealth)} | {Format(round.ZombieSpeed)} | {Format(round.ZombieDamage)} | " +
        $"{round.Build.Weapon} | {Format(round.Build.DamagePerShot)} | {round.ShotsToKill} | {round.TotalShots} | " +
        $"{Format(round.KillSeconds)} | {Format(round.RoundSeconds)} | " +
        $"{Format(round.Ammo.ReloadSeconds)} | {round.Ammo.Reloads} | {round.Ammo.Switches} | " +
        $"{round.SuppliesEarned} | {round.XpEarned} | {round.Reward.Type} +{round.Reward.Amount} | " +
        $"{round.CumulativeSupplies} | {round.Build.SuppliesSpent} | {round.Level} | {round.SkillPoints} |");
}

builder.AppendLine();
builder.AppendLine("## Content");
builder.AppendLine();
builder.AppendLine("| Round | Walkers | Runners | Brutes | Elite | Boss | Boss TTK s | Ammo pressure | Selected skill | Turret % | Dash % | Shockwave % | Berserker % | Ghost % | Bastion % |");
builder.AppendLine("|---:|---:|---:|---:|---|---|---:|---:|---|---:|---:|---:|---:|---:|---:|");

foreach (var round in report.Rounds)
{
    builder.AppendLine(
        $"| {round.Round} | {round.Content.Walkers} | {round.Content.Runners} | {round.Content.Brutes} | " +
        $"{(round.IsElite ? "Y" : "-")} | {(round.IsBoss ? "Y" : "-")} | {Format(round.Content.BossSeconds)} | " +
        $"{Format(round.Ammo.Pressure)} | {round.Skills.Selected} | " +
        $"{FormatPercent(round.Skills.TurretUptime)} | {FormatPercent(round.Skills.DashUptime)} | " +
        $"{FormatPercent(round.Skills.ShockwaveUptime)} | {FormatPercent(round.Skills.BerserkerUptime)} | " +
        $"{FormatPercent(round.Skills.GhostUptime)} | {FormatPercent(round.Skills.BastionUptime)} |");
}

builder.AppendLine();
builder.AppendLine("## Build");
builder.AppendLine();
builder.AppendLine("| Round | Weapon | Unlocks | DMG | RATE | MAG | RELOAD | Armor | Tier absorb | Total armor | STR | VIT | SHK | SPD | PRC | DSH | END | ENG | TUR |");
builder.AppendLine("|---:|---|---|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

foreach (var round in report.Rounds)
{
    var build = round.Build;
    builder.AppendLine(
        $"| {round.Round} | {build.Weapon} | {UnlockSummary(build)} | " +
        $"{build.DamageTier} | {build.FireRateTier} | {build.MagazineTier} | {build.ReloadTier} | " +
        $"{build.Armor} | {build.ArmorTierAbsorb} | {build.TotalArmor} | {build.StrengthRank} | {build.VitalityRank} | " +
        $"{build.ShockwaveRank} | {build.SpeedRank} | {build.PrecisionRank} | {build.DashRank} | " +
        $"{build.EnduranceRank} | {build.EngineeringRank} | {build.TurretRank} |");
}

builder.AppendLine();
builder.AppendLine("## Outliers");
builder.AppendLine();

if (report.HasOutliers)
{
    foreach (var outlier in report.Outliers)
    {
        builder.AppendLine($"- {outlier}");
    }
}
else
{
    builder.AppendLine("No outliers detected.");
}

var outputDirectory = Path.Combine("artifacts", "balance");
Directory.CreateDirectory(outputDirectory);
var outputPath = Path.Combine(outputDirectory, "rounds.md");
File.WriteAllText(outputPath, builder.ToString());

var scenarios = BalanceSimulation.RunWeaponScenarios();
var scenarioBuilder = new StringBuilder();
scenarioBuilder.AppendLine("# JabaSurvivol weapon scenarios");
scenarioBuilder.AppendLine();
scenarioBuilder.AppendLine("Every weapon unlocked and equipped at level 1; the default rifle-first strategy does not replace it.");
scenarioBuilder.AppendLine();
scenarioBuilder.AppendLine("| Weapon | Run s | Supplies earned | Supplies remaining | Final level | Boss TTK s | Max ammo pressure | Outliers |");
scenarioBuilder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");

foreach (var scenario in scenarios)
{
    var scenarioReport = scenario.Report;
    var totalSeconds = 0f;
    var maxPressure = 0f;
    foreach (var round in scenarioReport.Rounds)
    {
        totalSeconds += round.RoundSeconds;
        maxPressure = Math.Max(maxPressure, round.Ammo.Pressure);
    }

    var bossSeconds = scenarioReport.Rounds.Count == 0 ? 0f : scenarioReport.Rounds[^1].Content.BossSeconds;
    scenarioBuilder.AppendLine(
        $"| {scenario.Weapon} | {Format(totalSeconds)} | {scenarioReport.TotalSuppliesEarned} | " +
        $"{scenarioReport.TotalSupplies} | {scenarioReport.FinalLevel} | {Format(bossSeconds)} | " +
        $"{Format(maxPressure)} | {scenarioReport.Outliers.Count} |");
}

scenarioBuilder.AppendLine();
scenarioBuilder.AppendLine("## Scenario outliers");
scenarioBuilder.AppendLine();

foreach (var scenario in scenarios)
{
    scenarioBuilder.AppendLine($"### {scenario.Weapon}");
    scenarioBuilder.AppendLine();
    if (scenario.Report.HasOutliers)
    {
        foreach (var outlier in scenario.Report.Outliers)
        {
            scenarioBuilder.AppendLine($"- {outlier}");
        }
    }
    else
    {
        scenarioBuilder.AppendLine("No outliers detected.");
    }

    scenarioBuilder.AppendLine();
}

var scenarioPath = Path.Combine(outputDirectory, "weapons.md");
File.WriteAllText(scenarioPath, scenarioBuilder.ToString());

Console.Write(builder.ToString());
Console.WriteLine();
Console.WriteLine($"Report written to {outputPath}.");
Console.WriteLine($"Weapon scenarios written to {scenarioPath}.");

static string UnlockSummary(BuildSnapshot build)
{
    return $"SMG {(build.HasSmg ? "Y" : "N")} / SG {(build.HasShotgun ? "Y" : "N")} / RIFLE {(build.HasRifle ? "Y" : "N")} / LMG {(build.HasLmg ? "Y" : "N")}";
}

static string Format(float value)
{
    return value.ToString("0.00", CultureInfo.InvariantCulture);
}

static string FormatPercent(float value)
{
    return (value * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
}
