param(
    [string] $UnityPath = "",
    [string] $ProjectPath = "unity/JabaSurvivol",
    [string] $OutputPath = "artifacts/android/JabaSurvivol.apk"
)

$candidatePaths = @()
if (-not [string]::IsNullOrWhiteSpace($UnityPath)) {
    $candidatePaths += $UnityPath
}
else {
    $hubRoots = @(
        "C:\Program Files\Unity\Hub\Editor",
        "C:\dev\personal\Unity\Hub\Editor",
        (Join-Path $env:LOCALAPPDATA "Unity\Hub\Editor")
    )

    foreach ($hubRoot in $hubRoots) {
        if (Test-Path -LiteralPath $hubRoot) {
            $candidatePaths += Get-ChildItem -LiteralPath $hubRoot -Directory |
                Sort-Object Name -Descending |
                ForEach-Object { Join-Path $_.FullName "Editor\Unity.exe" }
        }
    }
}

$unity = $candidatePaths |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($unity)) {
    Write-Error "Unity 6.3 LTS was not found. Install Unity Hub and the Android module first."
    exit 1
}

if (-not (Test-Path -LiteralPath (Join-Path $ProjectPath "ProjectSettings/ProjectVersion.txt"))) {
    Write-Error "Unity project not found at $ProjectPath. See docs/setup/unity.md."
    exit 1
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null

& $unity -batchmode -quit -projectPath $ProjectPath -buildTarget Android -executeMethod JabaSurvivol.Editor.BuildScript.BuildAndroid -logFile - -buildOutput $OutputPath
exit $LASTEXITCODE
