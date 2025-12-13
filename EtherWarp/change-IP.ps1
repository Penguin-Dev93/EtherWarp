# Change-IP.ps1
function Set-StaticIP {
    param (
        [string]$InterfaceAlias,
        [string]$IPAddress,
        [int]$PrefixLength,
        [string]$Gateway = $null
    )

    # Remove existing IP and routes
    Get-NetIPAddress -InterfaceAlias $InterfaceAlias -ErrorAction SilentlyContinue | Remove-NetIPAddress -Confirm:$false
    Get-NetIPConfiguration -InterfaceAlias $InterfaceAlias | Remove-NetRoute -Confirm:$false

    # Set new IP
    New-NetIPAddress -InterfaceAlias $InterfaceAlias -IPAddress $IPAddress -PrefixLength $PrefixLength -ErrorAction Stop

    # Set default gateway if needed
    if ($Gateway) {
        New-NetRoute -InterfaceAlias $InterfaceAlias -DestinationPrefix "0.0.0.0/0" -NextHop $Gateway -ErrorAction Stop
    }

    Write-Host "`n✅ IP configuration applied to $InterfaceAlias"
}

# Adapter name 
$interface = "Ethernet"

# Menu
Write-Host "`nChoose a network config:"
Write-Host "1 - Instert Your desciption Here (100.57.23.5/28, 100.57.23.1)"
Write-Host "2 - MGMT Network (192.168.67.250/24, no gateway)"
Write-Host "3 - Servers (10.10.10.69/24, no gateway)"
$choice = Read-Host "Enter your choice (1, 2, or 3)"

switch ($choice) {
    "1" {
        Set-StaticIP -InterfaceAlias $interface -IPAddress "100.57.23.5/28" -PrefixLength 28 -Gateway "100.57.23.1"
    }
    "2" {
        Set-StaticIP -InterfaceAlias $interface -IPAddress "192.168.67.250" -PrefixLength 24
    }
    "3" {
        Set-StaticIP -InterfaceAlias $interface -IPAddress "10.10.10.69" -PrefixLength 24
    }
    default {
        Write-Host "❌ Invalid selection. Exiting..."
    }
}

# Thank you for using my script! Please let me know if you have any feedback or need further assistance by emailing me at: Penguin-Dev93@pm.me