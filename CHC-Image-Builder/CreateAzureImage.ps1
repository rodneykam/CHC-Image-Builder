$ErrorActionPreference = "Stop"

$script:StartTime = $(get-date)
$guidId = [System.Guid]::NewGuid()

# Get Authorization Info from enviornment variables
$subscriptionId = $Env:SubscriptionId
$clientId = $Env:ClientId
$clientKey = $Env:ClientKey
$tenantId = $Env:TenantId

Write-Host "Getting Azure Authorization Info..."
if ($null -eq $subscriptionId -or 
    $null -eq $clientId -or
    $null -eq $clientKey -or
    $null -eq $tenantId)
{
    # Full Authorization Info not in Environment, try reading Yaml File authorization.yaml
    $authInfo = Get-Content '.\authorization.yaml' | ConvertFrom-Yaml 
    if ($null -eq $authInfo)
    {
        throw "Authorization Info Not Found!"
    }
    else 
    {
        $subscriptionId = $authInfo.SubscriptionId
        $clientId = $authInfo.ClientId
        $clientKey = $authInfo.ClientKey
        $tenantId = $authInfo.TenantId
    }
}

Write-Host "Connecting to Azure..."
$secpassword = convertto-securestring $clientKey -AsPlainText -Force
$credential = new-object -typename System.Management.Automation.PScredential -argumentlist $clientId, $secpassword

Connect-AzureRMAccount -ServicePrincipal -credential $credential -TenantId $tenantId -Subscription $subscriptionId

Write-Host "Getting Image Configuration Info..."

$imageInfo = Get-Content '.\image.yaml' | ConvertFrom-Yaml
if ($null -eq $imageInfo)
{
    throw "Unable to Read Image Configuration File!"
}

$imageName = $imageInfo.ImageName
$imageGroupName = $imageInfo.ImageGroup
$VMLocalAdminUser = $imageInfo.AdminUser
$VMLocalAdminSecurePassword = ConvertTo-SecureString $imageInfo.AdminPW -AsPlainText -Force
$locationName = $imageInfo.Location
$rgName = "$guidId-rg"
$computerName = $imageInfo.ComputerName
$VMName = ([string]$guidId).SubString(0,12) + "-vm"
$VMSize = $imageInfo.VMSizeType

Write-Host "Creating Virtual Machine...ID = $guidId"
$networkName = "$guidId-vnet"
$NICName = "$guidId-nic"
$SubnetName = "$guidId-subnet"
$SubnetAddressPrefix = "10.0.0.0/24"
$vnetAddressPrefix = "10.0.0.0/16"

New-AzureRmResourceGroup -Name $rgName -Location $locationName
$singleSubnet = New-AzureRmVirtualNetworkSubnetConfig -Name $SubnetName -AddressPrefix $SubnetAddressPrefix
$vnet = New-AzureRmVirtualNetwork -Name $networkName -ResourceGroupName $rgName -Location $locationName -AddressPrefix $vnetAddressPrefix -Subnet $singleSubnet
$NIC = New-AzureRmNetworkInterface -Name $NICName -ResourceGroupName  $rgName -Location $locationName -SubnetId $vnet.Subnets[0].Id

$credential = New-Object System.Management.Automation.PScredential ($VMLocalAdminUser, $VMLocalAdminSecurePassword);

$vm = New-AzureRmVMConfig -VMName $VMName -VMSize $VMSize
$vm = Set-AzureRmVMOperatingSystem -VM $vm -Windows -computerName $computerName -credential $credential -ProvisionVMAgent -EnableAutoUpdate
$vm = Add-AzureRmVMNetworkInterface -VM $vm -Id $NIC.Id
$vm = Set-AzureRmVMSourceImage -VM $vm -PublisherName $imageInfo.VMPublisher -Offer $imageInfo.VMOffer -Skus $imageInfo.VMSKU -Version latest

New-AzureRmVM -ResourceGroupName $rgName -Location $locationName -VM $vm -Verbose

write-host "Virtual Machine Creation Completed"
$elapsedTime = new-timespan $script:StartTime $(get-date)
"Elapsed Time: " + $elapsedTime.Minutes + " minutes, " + $elapsedTime.Seconds + " seconds..." 

Write-Host "Stopping VM $VMName..."
Stop-AzureRmVM -ResourceGroupName $rgName -Name $VMName -Force

Write-Host "Getting VM Disk Information..."
$vm = Get-AzureRmVm -Name $VMName -ResourceGroupName $rgName
$diskID = $vm.StorageProfile.OsDisk.ManagedDisk.Id

Write-Host "Setting VM Disk Information..."
$imageConfig = New-AzureRmImageConfig -Location $locationName
$imageConfig = Set-AzureRmImageOsDisk -Image $imageConfig -OsState Generalized -OsType Windows -ManagedDiskId $diskID

Write-Host "Saving VM Image..."
$imageName += "-" + (Get-Date -Format s).Replace(":",".")

New-AzureRmImage -ImageName $imageName -ResourceGroupName $imageGroupName -Image $imageConfig

Write-Host "Deleting Resource Group..."
Remove-AzureRmResourceGroup -Name $rgName -Force

$elapsedTime = new-timespan $script:StartTime $(get-date)
"Created Image $imageName - Total Run Time: " + $elapsedTime.Minutes + " minutes, " + $elapsedTime.Seconds + " seconds..." 
