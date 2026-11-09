param(
    [string] $BaseUrl = "http://127.0.0.1:5102",
    [string] $PostgresContainer = "jaba-v050-pg",
    [string] $OutputPath = "artifacts/e2e/v05-online-flow.json"
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')
$suffix = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$userName = "e2e_$suffix"
$password = "Password1"
$results = [ordered]@{}
function Assert-Equal { param($Actual, $Expected, [string] $Name) if ($Actual -ne $Expected) { throw "$Name expected '$Expected' but was '$Actual'." } }
function Assert-True { param($Condition, [string] $Name) if (-not $Condition) { throw "$Name failed." } }
$headers = @{}

function Invoke-Json {
    param([string] $Method, [string] $Path, $Body, [hashtable] $ExtraHeaders = @{})
    $parameters = @{
        Method = $Method
        Uri = "$BaseUrl$Path"
        Headers = $ExtraHeaders
        TimeoutSec = 15
        SkipHttpErrorCheck = $true
    }
    if ($null -ne $Body) {
        $parameters.ContentType = "application/json"
        $parameters.Body = ($Body | ConvertTo-Json -Depth 20 -Compress)
    }
    $response = Invoke-WebRequest @parameters
    $json = $null
    if ($response.Content) {
        try { $json = $response.Content | ConvertFrom-Json } catch { $json = $response.Content }
    }
    return [pscustomobject]@{ Status = [int]$response.StatusCode; Body = $json; Raw = $response.Content }
}

$register = Invoke-Json -Method Post -Path "/api/v1/identity/register" -Body @{ username = $userName; password = $password }
$results.register = $register.Status
Assert-Equal $register.Status 201 "register"
$login = Invoke-Json -Method Post -Path "/api/v1/identity/login" -Body @{ username = $userName; password = $password }
$results.login = $login.Status
Assert-Equal $login.Status 200 "login"
$headers.Authorization = "Bearer $($login.Body.accessToken)"
$refresh = Invoke-Json -Method Post -Path "/api/v1/identity/refresh" -Body @{ refreshToken = $login.Body.refreshToken }
$results.refresh = $refresh.Status
Assert-Equal $refresh.Status 200 "refresh"
$headers.Authorization = "Bearer $($refresh.Body.accessToken)"
$me = Invoke-Json -Method Get -Path "/api/v1/identity/me" -ExtraHeaders $headers
$results.me = $me.Status
Assert-Equal $me.Status 200 "me"

$profile = @{
    SchemaVersion = 1
    Supplies = 100
    Xp = 0
    Level = 2
    SkillPoints = 0
    HighestRound = 2
    TotalKills = 5
    TotalRuns = 1
    BestKills = 5
    UnlockedWeapons = @(0)
    EquippedWeapon = 0
    WeaponUpgrades = @()
    UnlockedArmor = @(0)
    EquippedArmor = 0
    ArmorUpgrades = @()
    AllocatedSkills = @()
    ActiveSkill = 5
}
$save = Invoke-Json -Method Put -Path "/api/v1/game/saves/primary" -ExtraHeaders $headers -Body @{ baseRevision = 0; document = $profile }
$results.save = $save.Status
Assert-Equal $save.Status 200 "first save"
$revision = [long]$save.Body.revision
$getSave = Invoke-Json -Method Get -Path "/api/v1/game/saves/primary" -ExtraHeaders $headers
$results.getSave = $getSave.Status
Assert-Equal $getSave.Status 200 "get save"
Assert-Equal $getSave.Body.document.Supplies 100 "save document"
$stale = Invoke-Json -Method Put -Path "/api/v1/game/saves/primary" -ExtraHeaders $headers -Body @{ baseRevision = 0; document = $profile }
$results.staleConflict = $stale.Status
Assert-Equal $stale.Status 409 "stale conflict"
$update = Invoke-Json -Method Put -Path "/api/v1/game/saves/primary" -ExtraHeaders $headers -Body @{ baseRevision = $revision; document = $profile }
$results.update = $update.Status
Assert-Equal $update.Status 200 "update save"

$runId = [guid]::NewGuid().ToString()
$run = Invoke-Json -Method Post -Path "/api/v1/game/leaderboards/highest-round/runs" -ExtraHeaders $headers -Body @{
    runId = $runId
    highestRound = 4
    totalKills = 25
    durationSeconds = 300
    clientVersion = "0.5.0"
}
$results.leaderboardSubmit = $run.Status
Assert-Equal $run.Status 200 "leaderboard submit"
$duplicate = Invoke-Json -Method Post -Path "/api/v1/game/leaderboards/highest-round/runs" -ExtraHeaders $headers -Body @{
    runId = $runId
    highestRound = 1
    totalKills = 0
    durationSeconds = 1
    clientVersion = "0.5.0"
}
$results.leaderboardDuplicate = $duplicate.Status
Assert-Equal $duplicate.Status 200 "leaderboard duplicate"
$board = Invoke-Json -Method Get -Path "/api/v1/game/leaderboards/highest-round?limit=50"
$results.board = $board.Status
$results.boardContainsUser = [bool]($board.Body.entries | Where-Object { $_.username -eq $userName })
Assert-True $results.boardContainsUser "board contains user"

$telemetry = @{
    events = @(
        @{
            eventId = [guid]::NewGuid().ToString()
            eventType = "run_start"
            occurredAt = [DateTimeOffset]::UtcNow.ToString("O")
            sessionId = [guid]::NewGuid().ToString()
            payload = @{ startingRound = 1 }
        },
        @{
            eventId = [guid]::NewGuid().ToString()
            eventType = "run_end"
            occurredAt = [DateTimeOffset]::UtcNow.ToString("O")
            sessionId = [guid]::NewGuid().ToString()
            payload = @{ highestRound = 4; totalKills = 25; durationSeconds = 300 }
        }
    )
}
$telemetryResult = Invoke-Json -Method Post -Path "/api/v1/telemetry/events" -ExtraHeaders $headers -Body $telemetry
$results.telemetry = $telemetryResult.Status
$results.telemetryAccepted = $telemetryResult.Body.accepted
Assert-Equal $telemetryResult.Status 200 "telemetry"
Assert-Equal $telemetryResult.Body.accepted 2 "telemetry accepted"
$duplicateTelemetry = Invoke-Json -Method Post -Path "/api/v1/telemetry/events" -ExtraHeaders $headers -Body $telemetry
$results.telemetryDuplicates = $duplicateTelemetry.Body.duplicates
Assert-Equal $duplicateTelemetry.Body.duplicates 2 "telemetry duplicates"

$config = Invoke-Json -Method Get -Path "/api/v1/game/config"
$results.config = $config.Status
Assert-Equal $config.Status 200 "config"
Assert-True ($config.Body.cloudSavesEnabled -and $config.Body.leaderboardsEnabled) "config flags"
$results.configFlags = $config.Body

$results.telemetryAggregate = $null
if ($PostgresContainer) {
    Start-Sleep -Seconds 20
    $query = 'SELECT "RunCount","KillCount","EventCount" FROM telemetry."DailyAggregates" WHERE "Date" = CURRENT_DATE;'
    $aggregate = docker exec $PostgresContainer psql -U postgres -d jabasurvivol -t -A -F "," -c $query 2>$null
    if ($LASTEXITCODE -eq 0 -and $aggregate) {
        $results.telemetryAggregate = ($aggregate | Select-Object -First 1).Trim()
    }
}

$logout = Invoke-Json -Method Post -Path "/api/v1/identity/logout" -ExtraHeaders $headers -Body @{ refreshToken = $refresh.Body.refreshToken }
$results.logout = $logout.Status
Assert-Equal $logout.Status 204 "logout"
$reuse = Invoke-Json -Method Post -Path "/api/v1/identity/refresh" -Body @{ refreshToken = $refresh.Body.refreshToken }
$results.refreshAfterLogout = $reuse.Status
Assert-Equal $reuse.Status 401 "refresh after logout"

$directory = Split-Path -Parent $OutputPath
if ($directory) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }
$results.capturedAt = [DateTimeOffset]::UtcNow.ToString("O")
$results | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath
$results | ConvertTo-Json -Depth 10
