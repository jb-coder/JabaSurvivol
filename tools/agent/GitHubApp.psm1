$script:DefaultConfigPath = Join-Path $env:USERPROFILE ".config\jaba-survivol\github-app\config.json"

function ConvertTo-Base64Url {
    param(
        [Parameter(Mandatory = $true)]
        [byte[]] $Bytes
    )

    return [Convert]::ToBase64String($Bytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

function Get-JabaGitHubAppConfig {
    if (-not [string]::IsNullOrWhiteSpace($env:JABA_REVIEWER_APP_ID) -and
        -not [string]::IsNullOrWhiteSpace($env:JABA_REVIEWER_PRIVATE_KEY_PATH)) {
        return [pscustomobject]@{
            appId = $env:JABA_REVIEWER_APP_ID
            privateKeyPath = $env:JABA_REVIEWER_PRIVATE_KEY_PATH
            installationId = $env:JABA_REVIEWER_INSTALLATION_ID
            slug = $env:JABA_REVIEWER_APP_SLUG
        }
    }

    $configPath = $env:JABA_REVIEWER_CONFIG_PATH
    if ([string]::IsNullOrWhiteSpace($configPath)) {
        $configPath = $script:DefaultConfigPath
    }

    if (-not (Test-Path -LiteralPath $configPath)) {
        throw "GitHub App config not found at $configPath. Run tools/bootstrap/New-GitHubReviewerApp.ps1 first."
    }

    return Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
}

function New-GitHubAppJwt {
    param(
        [Parameter(Mandatory = $true)]
        [string] $AppId,

        [Parameter(Mandatory = $true)]
        [string] $PrivateKeyPem
    )

    $rsa = [System.Security.Cryptography.RSA]::Create()
    try {
        $rsa.ImportFromPem($PrivateKeyPem)

        $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        $headerJson = '{"alg":"RS256","typ":"JWT"}'
        $payloadJson = [ordered]@{
            iat = $now - 60
            exp = $now + 600
            iss = $AppId
        } | ConvertTo-Json -Compress

        $header = ConvertTo-Base64Url -Bytes ([Text.Encoding]::UTF8.GetBytes($headerJson))
        $payload = ConvertTo-Base64Url -Bytes ([Text.Encoding]::UTF8.GetBytes($payloadJson))
        $unsigned = "$header.$payload"
        $signature = $rsa.SignData(
            [Text.Encoding]::UTF8.GetBytes($unsigned),
            [Security.Cryptography.HashAlgorithmName]::SHA256,
            [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $encodedSignature = ConvertTo-Base64Url -Bytes $signature

        return "$unsigned.$encodedSignature"
    }
    finally {
        $rsa.Dispose()
    }
}

function Get-GitHubAppInstallation {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Jwt,

        [string] $AccountLogin = "jb-coder"
    )

    $headers = @{
        Authorization = "Bearer $Jwt"
        Accept = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
        "User-Agent" = "JabaSurvivol-Agent"
    }

    $installations = Invoke-RestMethod -Uri "https://api.github.com/app/installations" -Headers $headers
    return $installations | Where-Object { $_.account.login -eq $AccountLogin } | Select-Object -First 1
}

function New-GitHubAppInstallationToken {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Jwt,

        [Parameter(Mandatory = $true)]
        [string] $InstallationId
    )

    $headers = @{
        Authorization = "Bearer $Jwt"
        Accept = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
        "User-Agent" = "JabaSurvivol-Agent"
    }

    $response = Invoke-RestMethod `
        -Method Post `
        -Uri "https://api.github.com/app/installations/$InstallationId/access_tokens" `
        -Headers $headers `
        -ContentType "application/json" `
        -Body "{}"

    return $response.token
}

Export-ModuleMember -Function ConvertTo-Base64Url, Get-JabaGitHubAppConfig, New-GitHubAppJwt, Get-GitHubAppInstallation, New-GitHubAppInstallationToken
