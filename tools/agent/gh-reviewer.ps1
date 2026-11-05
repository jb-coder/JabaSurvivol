param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $GhArguments
)

if ($GhArguments.Count -eq 0) {
    Write-Error "No gh arguments were provided."
    exit 1
}

$token = $env:JABA_REVIEWER_TOKEN
if ([string]::IsNullOrWhiteSpace($token)) {
    $tokenOutput = & (Join-Path $PSScriptRoot "gh-app-token.ps1")
    $token = ($tokenOutput | Select-Object -Last 1)
    if ([string]::IsNullOrWhiteSpace($token)) {
        Write-Error "Unable to obtain a reviewer token. Configure the reviewer GitHub App or set JABA_REVIEWER_TOKEN."
        exit 1
    }

    $token = $token.Trim()
}

$previousToken = $env:GH_TOKEN
$previousHost = $env:GH_HOST
$exitCode = 0

try {
    $env:GH_TOKEN = $token
    $env:GH_HOST = "github.com"
    & gh @GhArguments
    $exitCode = $LASTEXITCODE
}
finally {
    $env:GH_TOKEN = $previousToken
    $env:GH_HOST = $previousHost
}

exit $exitCode
