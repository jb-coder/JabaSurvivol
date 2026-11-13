param(
    [Parameter(Mandatory = $true)]
    [string] $Owner,

    [Parameter(Mandatory = $true)]
    [string] $Repository
)

$auth = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "GitHub CLI is not authenticated. Run gh auth login first."
    exit 1
}

$labels = @(
    @{ Name = 'type:feature'; Color = '1D76DB'; Description = 'Feature work' },
    @{ Name = 'type:bug'; Color = 'D73A4A'; Description = 'Defect' },
    @{ Name = 'type:chore'; Color = 'C5DEF5'; Description = 'Engineering chore' },
    @{ Name = 'type:docs'; Color = '0075CA'; Description = 'Documentation' },
    @{ Name = 'type:test'; Color = '5319E7'; Description = 'Testing' },
    @{ Name = 'area:game'; Color = 'FBCA04'; Description = 'Unity game' },
    @{ Name = 'area:api'; Color = '0E8A16'; Description = 'Backend API' },
    @{ Name = 'area:ci'; Color = 'BFD4F2'; Description = 'CI and automation' },
    @{ Name = 'area:infra'; Color = 'D4C5F9'; Description = 'Infrastructure' },
    @{ Name = 'area:docs'; Color = '0075CA'; Description = 'Documentation' },
    @{ Name = 'area:art'; Color = 'E99695'; Description = 'Art and visual assets' },
    @{ Name = 'priority:P0'; Color = 'B60205'; Description = 'Crash or data loss' },
    @{ Name = 'priority:P1'; Color = 'D93F0B'; Description = 'Core feature broken' },
    @{ Name = 'priority:P2'; Color = 'FBCA04'; Description = 'Minor issue' },
    @{ Name = 'priority:P3'; Color = 'C2E0C6'; Description = 'Polish' },
    @{ Name = 'status:needs-review'; Color = 'FBCA04'; Description = 'Waiting for review' },
    @{ Name = 'status:in-review'; Color = '1D76DB'; Description = 'Review in progress' },
    @{ Name = 'status:changes-requested'; Color = 'D93F0B'; Description = 'Changes requested' },
    @{ Name = 'status:ready'; Color = '0E8A16'; Description = 'Ready to start' },
    @{ Name = 'status:approved'; Color = '0E8A16'; Description = 'Approved' },
    @{ Name = 'size:S'; Color = 'C2E0C6'; Description = 'Small task' },
    @{ Name = 'size:M'; Color = 'FBCA04'; Description = 'Medium task' },
    @{ Name = 'size:L'; Color = 'D93F0B'; Description = 'Large task' },
    @{ Name = 'agent:approved'; Color = '0E8A16'; Description = 'Reviewer approved' },
    @{ Name = 'agent:changes-requested'; Color = 'D93F0B'; Description = 'Reviewer requested changes' },
    @{ Name = 'agent:blocked'; Color = 'B60205'; Description = 'Blocked after repeated review failures' },
    @{ Name = 'agent:paused'; Color = '5319E7'; Description = 'Paused by automation policy' },
    @{ Name = 'needs-human'; Color = 'B60205'; Description = 'Requires human action for accounts, billing, legal, or identity' },
    @{ Name = 'needs-playtest'; Color = 'FBCA04'; Description = 'Needs playtest evidence' },
    @{ Name = 'blocked'; Color = 'B60205'; Description = 'Blocked by a dependency' },
    @{ Name = 'do-not-merge'; Color = 'B60205'; Description = 'Merge is forbidden' }
)

foreach ($label in $labels) {
    gh label create $label.Name --repo "$Owner/$Repository" --color $label.Color --description $label.Description --force | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create label $($label.Name)."
        exit 1
    }
}

Write-Output "Labels created for $Owner/$Repository."
Write-Output "Next steps:"
Write-Output "1. Create the jaba-builder and jaba-reviewer identities."
Write-Output "2. Configure rulesets for main and develop."
Write-Output "3. Add required checks: pr-policy, backend-ci, unity-tests, android-build, security."
Write-Output "4. Add repository variables and secrets."
Write-Output "5. Enable secret scanning and Dependabot alerts."
