param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $GhArguments
)

$token = $env:JABA_BUILDER_TOKEN
if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Error "JABA_BUILDER_TOKEN is not set. Configure the builder token before running GitHub operations."
    exit 1
}

if ($GhArguments.Count -eq 0) {
    Write-Error "No gh arguments were provided."
    exit 1
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
