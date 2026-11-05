param(
    [Parameter(Mandatory = $true)]
    [int] $PullRequest,

    [string] $Repository = "jb-coder/JabaSurvivol",

    [ValidateSet("success", "failure", "neutral")]
    [string] $Conclusion = "success",

    [string] $Summary = "Reviewer approval recorded for the current head commit."
)

$reviewerGh = Join-Path $PSScriptRoot "gh-reviewer.ps1"

$headSha = (& $reviewerGh pr view $PullRequest --repo $Repository --json headRefOid --jq ".headRefOid")
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($headSha)) {
    Write-Error "Unable to resolve the head commit for pull request $PullRequest."
    exit 1
}

$headSha = $headSha.Trim()
$checkBody = [ordered]@{
    name = "reviewer-approval"
    head_sha = $headSha
    status = "completed"
    conclusion = $Conclusion
    output = [ordered]@{
        title = "Reviewer approval"
        summary = $Summary
    }
} | ConvertTo-Json -Depth 5

$tempPath = Join-Path $env:TEMP "jaba-reviewer-approval-$PullRequest.json"
[System.IO.File]::WriteAllText($tempPath, $checkBody)

try {
    & $reviewerGh api -X POST "repos/$Repository/check-runs" --input $tempPath | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Unable to create the reviewer-approval check run."
        exit 1
    }

    Write-Output "Reviewer approval check recorded for $Repository#$PullRequest at $headSha."
}
finally {
    Remove-Item -LiteralPath $tempPath -ErrorAction SilentlyContinue
}
