param(
    [Parameter(Mandatory = $true)]
    [string] $Message
)

$pattern = '^(feat|fix|refactor|test|docs|chore|perf|build|ci)(\([a-z0-9-]+\))?: .{1,72}$'

if ($Message -notmatch $pattern) {
    Write-Error "Invalid commit message. Use Conventional Commits in English, for example: feat(game): add persistent supplies"
    exit 1
}

Write-Output "Commit message is valid."
