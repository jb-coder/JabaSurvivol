param(
    [string] $UnityPath = "",
    [string] $ProjectPath = "unity/JabaSurvivol",
    [switch] $CheckOnly
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
    Write-Output "Unity 6.3 LTS was not found."
    Write-Output "Install Unity Hub and Unity 6.3 LTS with Android Build Support, then create a Universal 2D project at $ProjectPath."
    exit 1
}

Write-Output "Found Unity at $unity"

if ($CheckOnly) {
    exit 0
}

$projectFile = Join-Path $ProjectPath "ProjectSettings/ProjectVersion.txt"
if (Test-Path -LiteralPath $projectFile) {
    Write-Output "Unity project already exists at $ProjectPath."
    exit 0
}

New-Item -ItemType Directory -Force -Path $ProjectPath | Out-Null
& $unity -createProject $ProjectPath -quit -batchmode -nographics
if ($LASTEXITCODE -eq 198) {
    Write-Error "Unity license not activated. Open Unity Hub, sign in, and activate a free Personal license, then retry."
    exit 198
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unity project creation failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Output "Unity project created at $ProjectPath."
Write-Output "Open it in Unity Hub and apply the Universal 2D template before implementing gameplay."
