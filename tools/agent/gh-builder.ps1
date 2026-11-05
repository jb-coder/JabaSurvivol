param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $GhArguments
)

if ($GhArguments.Count -eq 0) {
    Write-Error "No gh arguments were provided."
    exit 1
}

$token = $env:JABA_BUILDER_TOKEN
$previousToken = $env:GH_TOKEN
$previousHost = $env:GH_HOST
$exitCode = 0

try {
    $env:GH_HOST = "github.com"

    if ([string]::IsNullOrWhiteSpace($token)) {
        Remove-Item Env:GH_TOKEN -ErrorAction SilentlyContinue
    }
    else {
        $env:GH_TOKEN = $token
    }

    & gh @GhArguments
    $exitCode = $LASTEXITCODE
}
finally {
    $env:GH_TOKEN = $previousToken
    $env:GH_HOST = $previousHost
}

exit $exitCode
