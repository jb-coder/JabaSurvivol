param(
    [string] $Owner = "jb-coder",
    [string] $Repository = "jb-coder/JabaSurvivol",
    [string] $ProjectTitle = "JabaSurvivol Roadmap"
)

$ErrorActionPreference = "Stop"

function Invoke-GhJson {
    param([string[]] $Arguments)

    $output = & gh @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "gh $($Arguments -join ' ') failed."
    }

    return $output | ConvertFrom-Json
}

Write-Output "Resolving project..."

$projects = Invoke-GhJson @("project", "list", "--owner", $Owner, "--format", "json")
$project = $projects.projects | Where-Object { $_.title -eq $ProjectTitle } | Select-Object -First 1

if ($null -eq $project) {
    $project = Invoke-GhJson @("project", "create", "--owner", $Owner, "--title", $ProjectTitle, "--format", "json")
    Write-Output "Created project $ProjectTitle."
}
else {
    Write-Output "Project $ProjectTitle already exists."
}

$number = [int] $project.number
$project = Invoke-GhJson @("project", "view", [string] $number, "--owner", $Owner, "--format", "json")
$projectId = $project.id

try {
    & gh project link $number --owner $Owner --repo $Repository | Out-Null
}
catch {
    Write-Output "Project link already present or not required."
}

function Get-Fields {
    return Invoke-GhJson @("project", "field-list", [string] $number, "--owner", $Owner, "--format", "json")
}

$fields = Get-Fields

$statusField = $fields.fields | Where-Object { $_.name -eq "Status" } | Select-Object -First 1
if ($null -ne $statusField) {
    $statusOptions = @(
        @{ Name = "Backlog"; Color = "GRAY"; Description = "Not ready" },
        @{ Name = "Ready"; Color = "BLUE"; Description = "Ready to start" },
        @{ Name = "In Progress"; Color = "YELLOW"; Description = "Being implemented" },
        @{ Name = "In Review"; Color = "PURPLE"; Description = "Pull request under review" },
        @{ Name = "Changes Requested"; Color = "ORANGE"; Description = "Reviewer requested changes" },
        @{ Name = "In QA"; Color = "PINK"; Description = "Verification and playtest" },
        @{ Name = "Done"; Color = "GREEN"; Description = "Completed" },
        @{ Name = "Blocked"; Color = "RED"; Description = "Blocked by dependency" }
    )

    $optionsJson = ($statusOptions | ForEach-Object {
        "{ name: `"$($_.Name)`", color: $($_.Color), description: `"$($_.Description)`" }"
    }) -join ","

    $query = "mutation { updateProjectV2Field(input: { fieldId: `"$($statusField.id)`", singleSelectOptions: [$optionsJson] }) { projectV2Field { ... on ProjectV2SingleSelectField { id name } } } }"
    & gh api graphql -f "query=$query" | Out-Null
    Write-Output "Updated Status options."
}

$singleSelectFields = @(
    @{ Name = "Priority"; Options = @("P0", "P1", "P2", "P3") },
    @{ Name = "Area"; Options = @("Game", "API", "CI", "Infra", "Docs") },
    @{ Name = "Size"; Options = @("S", "M", "L") },
    @{ Name = "Phase"; Options = @("Walking Skeleton", "Rounds", "Armory", "Skills", "Content", "Backend", "Localization", "Monetization", "Release") }
)

foreach ($definition in $singleSelectFields) {
    $existing = $fields.fields | Where-Object { $_.name -eq $definition.Name } | Select-Object -First 1
    if ($null -eq $existing) {
        & gh project field-create $number --owner $Owner --name $definition.Name --data-type SINGLE_SELECT --single-select-options ($definition.Options -join ",") --format json | Out-Null
        Write-Output "Created field $($definition.Name)."
    }
}

$targetField = $fields.fields | Where-Object { $_.name -eq "Target" } | Select-Object -First 1
if ($null -eq $targetField) {
    & gh project field-create $number --owner $Owner --name "Target" --data-type DATE --format json | Out-Null
    Write-Output "Created field Target."
}

$fields = Get-Fields
$sprintField = $fields.fields | Where-Object { $_.name -eq "Sprint" } | Select-Object -First 1
if ($null -eq $sprintField) {
    $today = (Get-Date).Date
    $daysUntilMonday = (8 - [int] $today.DayOfWeek) % 7
    $startDate = $today.AddDays($daysUntilMonday)
    $iterations = @()
    for ($i = 1; $i -le 6; $i++) {
        $iterations += @{
            Title = "Sprint $i"
            StartDate = $startDate.AddDays(($i - 1) * 14).ToString("yyyy-MM-dd")
        }
    }

    $iterationsJson = ($iterations | ForEach-Object {
        "{ title: `"$($_.Title)`", startDate: `"$($_.StartDate)`", duration: 14 }"
    }) -join ","

    $query = "mutation { createProjectV2Field(input: { projectId: `"$projectId`", dataType: ITERATION, name: `"Sprint`", iterationConfiguration: { startDate: `"$($startDate.ToString('yyyy-MM-dd'))`", duration: 14, iterations: [$iterationsJson] } }) { projectV2Field { ... on ProjectV2IterationField { id name } } } }"
    & gh api graphql -f "query=$query" | Out-Null
    Write-Output "Created Sprint field."
}

$milestones = @(
    @{ Title = "v0.1.0 Walking Skeleton"; Due = "2026-11-23T23:59:59Z"; Description = "Unity project, touch controls, shooting, one zombie, supplies, game over, first APK." },
    @{ Title = "v0.2.0 Rounds and Economy"; Due = "2026-12-07T23:59:59Z"; Description = "Round system, rewards, XP, character levels, persistent supplies." },
    @{ Title = "v0.3.0 Armory and Skills"; Due = "2026-12-21T23:59:59Z"; Description = "Weapons, armor, upgrades, passive skills, respec." },
    @{ Title = "v0.4.0 Content and Boss"; Due = "2027-01-04T23:59:59Z"; Description = "Additional enemies, boss, active skills, synergies, balance." },
    @{ Title = "v0.5.0 Backend and Cloud"; Due = "2027-01-18T23:59:59Z"; Description = "Cloud saves, leaderboards, telemetry, remote config, and backend deployment automation." },
    @{ Title = "v0.5.1 Polish and Local Beta"; Due = "2026-12-14T23:59:59Z"; Description = "Polish the single-player experience, validate the online flow with five local testers over Tailscale, and defer paid VPS and monetization work." },
    @{ Title = "v0.6.0 Localization"; Due = "2027-02-01T23:59:59Z"; Description = "English plus additional locales, store metadata, layout tests." },
    @{ Title = "v1.0.0 Release and Donations"; Due = "2027-02-15T23:59:59Z"; Description = "Free release readiness, optional PayPal donation link, and final release. No premium currency, cosmetic store, payments sandbox, or in-game purchases." }
)

$existingMilestones = Invoke-GhJson @("api", "repos/$Repository/milestones")
foreach ($milestone in $milestones) {
    $existing = $existingMilestones | Where-Object { $_.title -eq $milestone.Title } | Select-Object -First 1
    if ($null -eq $existing) {
        & gh api -X POST "repos/$Repository/milestones" -f "title=$($milestone.Title)" -f "description=$($milestone.Description)" -f "due_on=$($milestone.Due)" | Out-Null
        Write-Output "Created milestone $($milestone.Title)."
    }
}

$labels = @(
    @{ Name = "size:S"; Color = "C2E0C6"; Description = "Small task" },
    @{ Name = "size:M"; Color = "FBCA04"; Description = "Medium task" },
    @{ Name = "size:L"; Color = "D93F0B"; Description = "Large task" },
    @{ Name = "status:ready"; Color = "0E8A16"; Description = "Ready to start" },
    @{ Name = "needs-human"; Color = "B60205"; Description = "Requires human action for accounts, billing, legal, or identity" }
)

foreach ($label in $labels) {
    & gh label create $label.Name --repo $Repository --color $label.Color --description $label.Description --force | Out-Null
}

$fields = Get-Fields
$fieldByName = @{}
foreach ($field in $fields.fields) {
    $fieldByName[$field.name] = $field
}

$desiredViews = @(
    @{ Name = "All Work"; Layout = "TABLE_LAYOUT"; Filter = "is:open"; Fields = @("Title", "Status", "Priority", "Area", "Size", "Sprint", "Milestone", "Assignees") },
    @{ Name = "Backlog"; Layout = "TABLE_LAYOUT"; Filter = "is:issue is:open"; Fields = @("Title", "Status", "Priority", "Area", "Size", "Sprint", "Milestone", "Labels", "Assignees") },
    @{ Name = "Delivery Board"; Layout = "BOARD_LAYOUT"; Filter = "is:open"; Fields = @("Title", "Status", "Priority", "Area", "Size", "Sprint", "Milestone", "Assignees") },
    @{ Name = "Roadmap"; Layout = "ROADMAP_LAYOUT"; Filter = "is:issue"; Fields = @() },
    @{ Name = "Pull Requests"; Layout = "TABLE_LAYOUT"; Filter = "is:pr is:open"; Fields = @("Title", "Status", "Priority", "Area", "Size", "Sprint", "Reviewers", "Labels") },
    @{ Name = "My Work"; Layout = "TABLE_LAYOUT"; Filter = "assignee:@me is:open"; Fields = @("Title", "Status", "Priority", "Area", "Size", "Sprint", "Milestone") }
)

$viewsQuery = "query { node(id: `"$projectId`") { ... on ProjectV2 { views(first: 30) { nodes { id name } } } } }"
$viewsResult = & gh api graphql -f "query=$viewsQuery" | ConvertFrom-Json
$existingViews = @($viewsResult.data.node.views.nodes)

foreach ($view in $desiredViews) {
    $existing = $existingViews | Where-Object { $_.name -eq $view.Name } | Select-Object -First 1

    if ($null -eq $existing -and $view.Name -eq "All Work") {
        $existing = $existingViews | Where-Object { $_.name -eq "View 1" } | Select-Object -First 1
        if ($null -ne $existing) {
            $updateQuery = "mutation { updateProjectV2View(input: { viewId: `"$($existing.id)`", name: `"All Work`", filter: `"is:open`" }) { projectV2View { id name } } }"
            & gh api graphql -f "query=$updateQuery" | Out-Null
            Write-Output "Renamed View 1 to All Work."
            continue
        }
    }

    if ($null -ne $existing) {
        continue
    }

    if ($view.Layout -eq "ROADMAP_LAYOUT") {
        $createQuery = "mutation { createProjectV2View(input: { projectId: `"$projectId`", name: `"$($view.Name)`", layout: ROADMAP_LAYOUT }) { projectV2View { id name } } }"
    }
    else {
        $visibleIds = @()
        foreach ($fieldName in $view.Fields) {
            $field = $fieldByName[$fieldName]
            if ($null -ne $field) {
                $visibleIds += "`"$($field.id)`""
            }
        }

        $configuration = ""
        if ($visibleIds.Count -gt 0) {
            $configuration = ", configuration: { visibleFieldIds: [$($visibleIds -join ',')] }"
        }

        $createQuery = "mutation { createProjectV2View(input: { projectId: `"$projectId`", name: `"$($view.Name)`", layout: $($view.Layout)$configuration }) { projectV2View { id name } } }"
    }

    $created = & gh api graphql -f "query=$createQuery" | ConvertFrom-Json
    $createdId = $created.data.createProjectV2View.projectV2View.id
    $updateQuery = "mutation { updateProjectV2View(input: { viewId: `"$createdId`", filter: `"$($view.Filter)`" }) { projectV2View { id name } } }"
    & gh api graphql -f "query=$updateQuery" | Out-Null
    Write-Output "Created view $($view.Name)."
}

$openIssues = Invoke-GhJson @("issue", "list", "--repo", $Repository, "--state", "open", "--limit", "200", "--json", "url")
foreach ($issue in $openIssues) {
    & (Join-Path $PSScriptRoot "..\agent\sync-project-item.ps1") -ItemUrl $issue.url -Owner $Owner -ProjectNumber $number -Repository $Repository | Out-Null
}

$openPullRequests = Invoke-GhJson @("pr", "list", "--repo", $Repository, "--state", "open", "--limit", "200", "--json", "url")
foreach ($pullRequest in $openPullRequests) {
    & (Join-Path $PSScriptRoot "..\agent\sync-project-item.ps1") -ItemUrl $pullRequest.url -Owner $Owner -ProjectNumber $number -Repository $Repository | Out-Null
}

Write-Output "Project ready: $($project.url)"
Write-Output "Number: $number"
Write-Output "Open views: https://github.com/users/$Owner/projects/$number"
