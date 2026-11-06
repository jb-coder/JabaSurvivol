using System.Globalization;
using System.Text;
using JabaSurvivol.Core;

var report = BalanceSimulation.Run();
var builder = new StringBuilder();

builder.AppendLine("# JabaSurvivol balance simulation");
builder.AppendLine();
builder.AppendLine($"- Seed: {report.Seed}");
builder.AppendLine($"- Rounds: {report.Rounds.Count}");
builder.AppendLine($"- Total supplies: {report.TotalSupplies}");
builder.AppendLine($"- Total XP: {report.TotalXp}");
builder.AppendLine($"- Final level: {report.FinalLevel}");
builder.AppendLine($"- Final skill points: {report.FinalSkillPoints}");
builder.AppendLine($"- Outliers: {report.Outliers.Count}");
builder.AppendLine();
builder.AppendLine("| Round | Budget | Zombie HP | Speed | Damage | Shots | Kill s | Round s | Supplies | XP | Reward | Cumulative supplies | Cumulative XP | Level | SP |");
builder.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|");

foreach (var round in report.Rounds)
{
    builder.AppendLine(
        $"| {round.Round} | {round.SpawnBudget} | " +
        $"{Format(round.ZombieHealth)} | {Format(round.ZombieSpeed)} | {Format(round.ZombieDamage)} | " +
        $"{round.ShotsToKill} | {Format(round.KillSeconds)} | {Format(round.RoundSeconds)} | " +
        $"{round.SuppliesEarned} | {round.XpEarned} | {round.Reward.Type} +{round.Reward.Amount} | " +
        $"{round.CumulativeSupplies} | {round.CumulativeXp} | {round.Level} | {round.SkillPoints} |");
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

static string Format(float value)
{
    return value.ToString("0.00", CultureInfo.InvariantCulture);
}
