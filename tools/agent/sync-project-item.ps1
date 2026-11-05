param(
    [Parameter(Mandatory = $true)]
    [string] $ItemUrl,

    [string] $Owner = "jb-coder",
    [int] $ProjectNumber = 1,
    [string] $Repository = "jb-coder/JabaSurvivol",
    [string] $EventName = "",
    [string] $Action = "",
    [string] $ReviewState = ""
)

$number = [int] (($ItemUrl.TrimEnd("/") -split "/")[-1])
$isPullRequest = $ItemUrl -match "/pull/"

if ($isPullRequest) {
    $itemJson = gh pr view $number --repo $Repository --json number,title,state,isDraft,mergedAt,labels,milestone,url,reviewDecision,assignees
}
else {
    $itemJson = gh issue view $number --repo $Repository --json number,title,state,labels,milestone,url,assignees
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unable to read item $ItemUrl."
    exit 1
}

$item = $itemJson | ConvertFrom-Json
$labels = @($item.labels | ForEach-Object { $_.name })
$milestoneTitle = $item.milestone.title
$milestoneDue = $item.milestone.dueOn

$fieldListJson = gh project field-list $ProjectNumber --owner $Owner --format json
if ($LASTEXITCODE -ne 0) {
    Write-Error "Unable to read project fields."
    exit 1
}

$fieldList = $fieldListJson | ConvertFrom-Json
$fields = @{}
foreach ($field in $fieldList.fields) {
    $fields[$field.name] = $field
}

$project = gh project view $ProjectNumber --owner $Owner --format json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    Write-Error "Unable to read project $ProjectNumber."
    exit 1
}

$projectId = $project.id
$projectItems = gh project item-list $ProjectNumber --owner $Owner --format json | ConvertFrom-Json
$existingItem = $projectItems.items | Where-Object { $_.content.url -eq $ItemUrl } | Select-Object -First 1

if ($null -ne $existingItem) {
    $itemId = $existingItem.id
}
else {
    $added = gh project item-add $ProjectNumber --owner $Owner --url $ItemUrl --format json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Unable to add $ItemUrl to project $ProjectNumber."
        exit 1
    }

    $itemId = $added.id
}

function Get-FieldOptionId {
    param(
        [string] $FieldName,
        [string] $OptionName
    )

    if ([string]::IsNullOrWhiteSpace($OptionName)) {
        return $null
    }

    $field = $fields[$FieldName]
    if ($null -eq $field -or $null -eq $field.options) {
        return $null
    }

    $option = $field.options | Where-Object { $_.name -eq $OptionName } | Select-Object -First 1
    return $option.id
}

function Set-SingleSelect {
    param(
        [string] $FieldName,
        [string] $OptionName
    )

    $optionId = Get-FieldOptionId -FieldName $FieldName -OptionName $OptionName
    if ([string]::IsNullOrWhiteSpace($optionId)) {
        return
    }

    gh project item-edit --id $itemId --project-id $projectId --field-id $fields[$FieldName].id --single-select-option-id $optionId | Out-Null
}

if ($isPullRequest) {
    if ($item.mergedAt -or $item.state -eq "MERGED" -or $item.state -eq "CLOSED") {
        $status = "Done"
    }
    elseif ($item.isDraft) {
        $status = "In Progress"
    }
    elseif ($item.reviewDecision -eq "CHANGES_REQUESTED" -or $ReviewState -eq "changes_requested") {
        $status = "Changes Requested"
    }
    else {
        $status = "In Review"
    }
}
else {
    if ($item.state -eq "CLOSED") {
        $status = "Done"
    }
    elseif ($labels -contains "status:blocked") {
        $status = "Blocked"
    }
    elseif ($labels -contains "status:ready") {
        $status = "Ready"
    }
    elseif ($labels -contains "status:in-progress") {
        $status = "In Progress"
    }
    else {
        $status = "Backlog"
    }
}

$priority = ($labels | Where-Object { $_ -match "^priority:(P[0-3])$" } | Select-Object -First 1)
if ($priority) {
    $priority = ($priority -split ":")[1]
}
else {
    $priority = "P2"
}

$area = $null
foreach ($label in $labels) {
    switch ($label) {
        "area:game" { $area = "Game"; break }
        "area:api" { $area = "API"; break }
        "area:ci" { $area = "CI"; break }
        "area:infra" { $area = "Infra"; break }
        "area:docs" { $area = "Docs"; break }
    }

    if ($area) { break }
}

$size = ($labels | Where-Object { $_ -match "^size:(S|M|L)$" } | Select-Object -First 1)
if ($size) {
    $size = ($size -split ":")[1]
}

$phase = $null
if ($milestoneTitle) {
    switch -Wildcard ($milestoneTitle) {
        "v0.1.0*" { $phase = "Walking Skeleton" }
        "v0.2.0*" { $phase = "Rounds" }
        "v0.3.0*" { $phase = "Armory" }
        "v0.4.0*" { $phase = "Content" }
        "v0.5.0*" { $phase = "Backend" }
        "v0.6.0*" { $phase = "Localization" }
        "v1.0.0*" { $phase = "Monetization" }
    }
}

$targetDate = $null
if ($milestoneDue) {
    $targetDate = ([datetime] $milestoneDue).ToString("yyyy-MM-dd")
}

$currentSprint = $null
$sprintField = $fields["Sprint"]
if ($null -ne $sprintField) {
    $sprintQuery = "query { node(id: `"$($sprintField.id)`") { ... on ProjectV2IterationField { configuration { iterations { id title startDate duration } } } } }"
    $sprintResult = gh api graphql -f "query=$sprintQuery" | ConvertFrom-Json
    $iterations = @($sprintResult.data.node.configuration.iterations)
    $today = (Get-Date).Date
    $currentSprint = $iterations | Where-Object {
        $start = [datetime] $_.startDate
        $end = $start.AddDays([int] $_.duration)
        $today -ge $start -and $today -lt $end
    } | Select-Object -First 1
}

Set-SingleSelect -FieldName "Status" -OptionName $status
Set-SingleSelect -FieldName "Priority" -OptionName $priority
Set-SingleSelect -FieldName "Area" -OptionName $area
Set-SingleSelect -FieldName "Size" -OptionName $size
Set-SingleSelect -FieldName "Phase" -OptionName $phase

if ($targetDate -and $fields["Target"]) {
    gh project item-edit --id $itemId --project-id $projectId --field-id $fields["Target"].id --date $targetDate | Out-Null
}

if ($currentSprint -and $sprintField) {
    gh project item-edit --id $itemId --project-id $projectId --field-id $sprintField.id --iteration-id $currentSprint.id | Out-Null
}

Write-Output "Synced $ItemUrl -> status=$status priority=$priority area=$area size=$size phase=$phase sprint=$($currentSprint.title)"
