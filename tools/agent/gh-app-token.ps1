param(
    [string] $AppId = $env:JABA_REVIEWER_APP_ID,
    [string] $PrivateKeyPath = $env:JABA_REVIEWER_PRIVATE_KEY_PATH,
    [string] $InstallationId = $env:JABA_REVIEWER_INSTALLATION_ID
)

Import-Module (Join-Path $PSScriptRoot "GitHubApp.psm1") -Force

try {
    if ([string]::IsNullOrWhiteSpace($AppId) -or [string]::IsNullOrWhiteSpace($PrivateKeyPath)) {
        $config = Get-JabaGitHubAppConfig
        $AppId = [string] $config.appId
        $PrivateKeyPath = [string] $config.privateKeyPath
        if ([string]::IsNullOrWhiteSpace($InstallationId)) {
            $InstallationId = [string] $config.installationId
        }
    }

    if (-not (Test-Path -LiteralPath $PrivateKeyPath)) {
        throw "Reviewer private key not found at $PrivateKeyPath."
    }

    $pem = Get-Content -LiteralPath $PrivateKeyPath -Raw
    $jwt = New-GitHubAppJwt -AppId $AppId -PrivateKeyPem $pem

    if ([string]::IsNullOrWhiteSpace($InstallationId)) {
        $installation = Get-GitHubAppInstallation -Jwt $jwt
        if ($null -eq $installation) {
            throw "No installation found for jb-coder. Install the reviewer app on JabaSurvivol first."
        }

        $InstallationId = [string] $installation.id
    }

    $token = New-GitHubAppInstallationToken -Jwt $jwt -InstallationId $InstallationId
    Write-Output $token
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
