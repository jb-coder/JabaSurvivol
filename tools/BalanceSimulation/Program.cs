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
builder.AppendLine("| Round | Budget | Zombie HP | Speed | Damage | Weapon | DMG/shot | Shots | Kill s | Round s | Supplies | XP | Reward | Remaining | Spent | Level | SP |");
builder.AppendLine("|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|");

foreach (var round in report.Rounds)
{
    builder.AppendLine(
        $"| {round.Round} | {round.SpawnBudget} | " +
        $"{Format(round.ZombieHealth)} | {Format(round.ZombieSpeed)} | {Format(round.ZombieDamage)} | " +
        $"{round.Build.Weapon} | {Format(round.Build.DamagePerShot)} | {round.ShotsToKill} | " +
        $"{Format(round.KillSeconds)} | {Format(round.RoundSeconds)} | " +
        $"{round.SuppliesEarned} | {round.XpEarned} | {round.Reward.Type} +{round.Reward.Amount} | " +
        $"{round.CumulativeSupplies} | {round.Build.SuppliesSpent} | {round.Level} | {round.SkillPoints} |");
}

builder.AppendLine();
builder.AppendLine("## Build");
builder.AppendLine();
builder.AppendLine("| Round | Weapon | Unlocks | DMG | RATE | MAG | RELOAD | Armor | Absorb | STR | VIT | SPD | PRC | END | ENG | TUR |");
builder.AppendLine("|---:|---|---|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|");

foreach (var round in report.Rounds)
{
    var build = round.Build;
    builder.AppendLine(
        $"| {round.Round} | {build.Weapon} | {UnlockSummary(build)} | " +
        $"{build.DamageTier} | {build.FireRateTier} | {build.MagazineTier} | {build.ReloadTier} | " +
        $"{build.Armor} | {build.ArmorAbsorb} | {build.StrengthRank} | {build.VitalityRank} | " +
        $"{build.SpeedRank} | {build.PrecisionRank} | {build.EnduranceRank} | " +
        $"{build.EngineeringRank} | {build.TurretRank} |");
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

Console.Write(builder.ToString());
Console.WriteLine();
Console.WriteLine($"Report written to {outputPath}.");

static string UnlockSummary(BuildSnapshot build)
{
    return $"SMG {(build.HasSmg ? "Y" : "N")} / SG {(build.HasShotgun ? "Y" : "N")} / RIFLE {(build.HasRifle ? "Y" : "N")} / LMG {(build.HasLmg ? "Y" : "N")}";
}

static string Format(float value)
{
    return value.ToString("0.00", CultureInfo.InvariantCulture);
}
