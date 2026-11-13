using JabaSurvivol.ArtGenerator;

var root = ArtPipeline.FindRepositoryRoot();
var outputRoot = Path.Combine(root, ArtPipeline.DefaultOutputRoot);
var manifestPath = Path.Combine(root, ArtPipeline.DefaultManifestPath);
var contactSheetPath = Path.Combine(root, ArtPipeline.DefaultContactSheetPath);

var mode = args.Length == 0 ? "generate" : args[0].TrimStart('-').ToLowerInvariant();

switch (mode)
{
    case "generate":
        var manifest = ArtPipeline.Generate(outputRoot, manifestPath);
        Console.WriteLine($"Generated {manifest.Assets.Count} sprites in {outputRoot}.");
        Console.WriteLine($"Manifest: {manifestPath}");
        return 0;

    case "check":
        var errors = ArtPipeline.Check(outputRoot, manifestPath);
        if (errors.Count == 0)
        {
            Console.WriteLine("Art assets and manifest are up to date.");
            return 0;
        }

        foreach (var error in errors)
        {
            Console.Error.WriteLine(error);
        }

        return 1;

    case "contact-sheet":
    case "contactsheet":
        ArtPipeline.WriteContactSheet(outputRoot, manifestPath, contactSheetPath);
        Console.WriteLine($"Contact sheet: {contactSheetPath}");
        return 0;

    default:
        Console.Error.WriteLine("Usage: ArtGenerator [generate|check|contact-sheet]");
        return 2;
}
