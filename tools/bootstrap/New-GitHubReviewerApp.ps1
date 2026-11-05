param(
    [string] $AppName = "jaba-survivol-reviewer-jb",
    [string] $AccountLogin = "jb-coder",
    [string] $Repository = "jb-coder/JabaSurvivol",
    [int] $TimeoutMinutes = 10
)

Import-Module (Join-Path $PSScriptRoot "..\agent\GitHubApp.psm1") -Force

$secureDir = Join-Path $env:USERPROFILE ".config\jaba-survivol\github-app"
New-Item -ItemType Directory -Force -Path $secureDir | Out-Null

$listener = $null
$port = 0
for ($candidate = 8765; $candidate -lt 8800; $candidate++) {
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $candidate)
        $listener.Start()
        $port = $candidate
        break
    }
    catch {
        $listener = $null
    }
}

if ($null -eq $listener) {
    Write-Error "No free local port was found for the GitHub App manifest callback."
    exit 1
}

$state = [guid]::NewGuid().ToString("N")
$redirectUrl = "http://127.0.0.1:$port/callback"

$manifest = [ordered]@{
    name = $AppName
    description = "Independent reviewer for JabaSurvivol."
    url = "https://github.com/$Repository"
    hook_attributes = [ordered]@{
        url = "https://example.com"
        active = $false
    }
    redirect_url = $redirectUrl
    public = $false
    request_oauth_on_install = $false
    default_permissions = [ordered]@{
        contents = "read"
        pull_requests = "write"
        issues = "write"
        checks = "write"
        metadata = "read"
    }
    default_events = @()
}

$manifestJson = $manifest | ConvertTo-Json -Depth 8 -Compress
$encodedManifest = [System.Net.WebUtility]::HtmlEncode($manifestJson)
$actionUrl = "https://github.com/settings/apps/new?state=$state"

$html = @"
<!doctype html>
<html>
<head><meta charset="utf-8"><title>JabaSurvivol GitHub App</title></head>
<body>
  <p>Creating the JabaSurvivol reviewer GitHub App...</p>
  <form id="manifest-form" action="$actionUrl" method="post">
    <input type="hidden" name="manifest" value="$encodedManifest">
    <button type="submit">Create GitHub App</button>
  </form>
  <script>document.getElementById("manifest-form").submit();</script>
</body>
</html>
"@

$htmlPath = Join-Path $env:TEMP "jaba-survivol-github-app.html"
[System.IO.File]::WriteAllText($htmlPath, $html)
Start-Process $htmlPath

Write-Output "Browser opened. Confirm the GitHub App creation, then install it only on $Repository."
Write-Output "Waiting for GitHub callback on $redirectUrl ..."

$client = $null
$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
while ((Get-Date) -lt $deadline) {
    if ($listener.Pending()) {
        $client = $listener.AcceptTcpClient()
        break
    }

    Start-Sleep -Milliseconds 250
}

if ($null -eq $client) {
    $listener.Stop()
    Write-Error "Timed out waiting for GitHub to create the app."
    exit 1
}

try {
    $stream = $client.GetStream()
    $reader = [System.IO.StreamReader]::new($stream, [Text.Encoding]::ASCII)
    $requestLine = $reader.ReadLine()
    $requestPath = ($requestLine -split " ")[1]

    $responseHtml = "<html><body><h1>GitHub App created</h1><p>You can close this window and return to the terminal.</p></body></html>"
    $responseBytes = [Text.Encoding]::UTF8.GetBytes($responseHtml)
    $responseHeader = "HTTP/1.1 200 OK`r`nContent-Type: text/html; charset=utf-8`r`nContent-Length: $($responseBytes.Length)`r`nConnection: close`r`n`r`n"
    $writer = [System.IO.StreamWriter]::new($stream, [Text.Encoding]::UTF8)
    $writer.Write($responseHeader)
    $writer.Write($responseHtml)
    $writer.Flush()
    $writer.Dispose()
    $client.Close()
}
finally {
    $listener.Stop()
}

$uri = [Uri]("http://127.0.0.1:$port$requestPath")
$query = $uri.Query.TrimStart("?")
$parameters = @{}
foreach ($pair in $query.Split("&")) {
    if ([string]::IsNullOrWhiteSpace($pair)) {
        continue
    }

    $keyValue = $pair.Split("=", 2)
    $parameters[[Uri]::UnescapeDataString($keyValue[0])] = [Uri]::UnescapeDataString($keyValue[1])
}

if ($parameters["state"] -ne $state) {
    Write-Error "GitHub App callback state mismatch."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($parameters["code"])) {
    Write-Error "GitHub App callback did not include a code."
    exit 1
}

$conversion = Invoke-RestMethod `
    -Method Post `
    -Uri "https://api.github.com/app-manifests/$($parameters["code"])/conversions" `
    -Headers @{
        Accept = "application/vnd.github+json"
        "User-Agent" = "JabaSurvivol-Agent"
    } `
    -ContentType "application/json" `
    -Body "{}"

$privateKeyPath = Join-Path $secureDir "private-key.pem"
[System.IO.File]::WriteAllText($privateKeyPath, $conversion.pem)

$configPath = Join-Path $secureDir "config.json"
$config = [ordered]@{
    appId = [string] $conversion.id
    slug = [string] $conversion.slug
    privateKeyPath = $privateKeyPath
    installationId = $null
}
$config | ConvertTo-Json | Set-Content -LiteralPath $configPath -Encoding utf8

Write-Output "GitHub App created."
Write-Output "App ID: $($conversion.id)"
Write-Output "Slug: $($conversion.slug)"
Write-Output "Credentials: $secureDir"

$installUrl = "https://github.com/apps/$($conversion.slug)/installations/new"
Start-Process $installUrl
Write-Output "Install the app only on $Repository. Waiting for installation detection..."

$jwt = New-GitHubAppJwt -AppId ([string] $conversion.id) -PrivateKeyPem $conversion.pem
$installDeadline = (Get-Date).AddMinutes($TimeoutMinutes)
$installation = $null

while ((Get-Date) -lt $installDeadline -and $null -eq $installation) {
    Start-Sleep -Seconds 5
    try {
        $installation = Get-GitHubAppInstallation -Jwt $jwt -AccountLogin $AccountLogin
    }
    catch {
        $installation = $null
    }
}

if ($null -eq $installation) {
    Write-Error "The app was created but no installation was detected. Install it at $installUrl and run tools/agent/gh-app-token.ps1."
    exit 1
}

$config.installationId = [string] $installation.id
$config | ConvertTo-Json | Set-Content -LiteralPath $configPath -Encoding utf8

Write-Output "Reviewer GitHub App installed."
Write-Output "Installation ID: $($installation.id)"
Write-Output "Configuration: $configPath"
