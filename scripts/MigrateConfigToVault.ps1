# Script to migrate sensitive configuration to HashiCorp Vault
param (
    [string]$VaultUrl = "http://localhost:8200",
    [string]$VaultToken = $env:VAULT_TOKEN,
    [string]$ConfigPath = "src",
    [string]$SecretsEnginePath = "secret"
)

# Check if Vault token is provided
if ([string]::IsNullOrEmpty($VaultToken)) {
    Write-Error "Vault token is required. Please set the VAULT_TOKEN environment variable or provide it as a parameter."
    exit 1
}

# Function to extract sensitive configuration from appsettings files
function Extract-SensitiveConfig {
    param (
        [string]$FilePath
    )

    Write-Host "Processing $FilePath..."
    
    # Read the JSON file
    $config = Get-Content -Path $FilePath -Raw | ConvertFrom-Json
    
    # Extract connection strings
    $connectionStrings = @{}
    if ($config.ConnectionStrings) {
        $config.ConnectionStrings.PSObject.Properties | ForEach-Object {
            if (-not [string]::IsNullOrEmpty($_.Value)) {
                $connectionStrings[$_.Name] = $_.Value
            }
        }
    }
    
    # Extract JWT settings
    $jwtSettings = @{}
    if ($config.JwtSettings) {
        $config.JwtSettings.PSObject.Properties | ForEach-Object {
            if ($_.Name -eq "Secret") {
                $jwtSettings["secret"] = $_.Value
            }
        }
    }
    
    # Extract other sensitive settings
    $sensitiveSettings = @{}
    
    # API keys
    if ($config.ApiKeys) {
        $config.ApiKeys.PSObject.Properties | ForEach-Object {
            $sensitiveSettings["apikeys/$($_.Name)"] = $_.Value
        }
    }
    
    # SMTP settings
    if ($config.Smtp) {
        if ($config.Smtp.Password) {
            $sensitiveSettings["smtp/password"] = $config.Smtp.Password
        }
    }
    
    # Authentication settings
    if ($config.Authentication) {
        foreach ($provider in @("Google", "Microsoft", "GitHub")) {
            if ($config.Authentication.$provider) {
                if ($config.Authentication.$provider.ClientSecret) {
                    $sensitiveSettings["auth/$($provider.ToLower())/clientsecret"] = $config.Authentication.$provider.ClientSecret
                }
            }
        }
    }
    
    # Return the extracted configuration
    return @{
        ConnectionStrings = $connectionStrings
        JwtSettings = $jwtSettings
        SensitiveSettings = $sensitiveSettings
    }
}

# Function to write configuration to Vault
function Write-ToVault {
    param (
        [string]$Path,
        [hashtable]$Data
    )
    
    if ($Data.Count -eq 0) {
        return
    }
    
    # Convert hashtable to JSON
    $jsonData = $Data | ConvertTo-Json -Compress
    
    # Write to Vault
    $headers = @{
        "X-Vault-Token" = $VaultToken
    }
    
    $vaultPath = "$SecretsEnginePath/data/$Path"
    $url = "$VaultUrl/v1/$vaultPath"
    
    $body = @{
        data = $Data
    } | ConvertTo-Json
    
    try {
        Write-Host "Writing to Vault: $vaultPath"
        Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $body -ContentType "application/json"
        Write-Host "Successfully wrote to Vault: $vaultPath" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to write to Vault: $vaultPath. Error: $_"
    }
}

# Find all appsettings files
$appSettingsFiles = Get-ChildItem -Path $ConfigPath -Recurse -Include "appsettings*.json"

# Process each file
foreach ($file in $appSettingsFiles) {
    $config = Extract-SensitiveConfig -FilePath $file.FullName
    
    # Get the service name from the file path
    $serviceName = $file.Directory.Name.ToLower()
    
    # Write connection strings to Vault
    if ($config.ConnectionStrings.Count -gt 0) {
        Write-ToVault -Path "connectionstrings/$serviceName" -Data $config.ConnectionStrings
    }
    
    # Write JWT settings to Vault
    if ($config.JwtSettings.Count -gt 0) {
        Write-ToVault -Path "jwt" -Data $config.JwtSettings
    }
    
    # Write other sensitive settings to Vault
    foreach ($key in $config.SensitiveSettings.Keys) {
        $value = $config.SensitiveSettings[$key]
        $data = @{ value = $value }
        Write-ToVault -Path $key -Data $data
    }
}

Write-Host "Configuration migration completed." -ForegroundColor Green
