$ErrorActionPreference = "Stop"

$script:StartTime = $(get-date)

Write-Host "Connecting to Azure..."
$applicationId = "f1659788-41eb-439b-9fe2-3e373f870719"
$secpassword = convertto-securestring "hx6NCNBoiKYb+yIKHFWUJqjUXuhYa7kxZ7F1TFohvro=" -AsPlainText -Force
$credential = new-object -typename System.Management.Automation.PScredential -argumentlist $applicationId, $secpassword

Connect-AzureRMAccount -ServicePrincipal -credential $credential -TenantId "b37600f0-1e5e-48fb-b34d-d9bdb51cdbc5" -Subscription "a646d321-18de-4209-a895-5c7dec3a9ca0"

$guidId = [System.Guid]::NewGuid()

Write-Host "Creating Virtual Machine...ID = $guidId"

$imageName = "WIN2K12"
$imageGroupName = "Refereance-Image-Storage-rg"
$VMLocalAdminUser = "LocalAdminUser"
$VMLocalAdminSecurePassword = ConvertTo-SecureString 'LocalAdminP@ssw0rd' -AsPlainText -Force
$locationName = "WestUS"
$rgName = "$guidId-rg"
$computerName = "REFERENCE-VM"
$VMName = ([string]$guidId).SubString(0,12) + "-vm"
$VMSize = "Standard_DS3"

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
$vm = Set-AzureRmVMSourceImage -VM $vm -PublisherName 'MicrosoftWindowsServer' -Offer 'WindowsServer' -Skus '2012-R2-Datacenter' -Version latest

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

Write-Host "Deleting Resource4 Group..."
Remove-AzureRmResourceGroup -Name $rgName -Force

$elapsedTime = new-timespan $script:StartTime $(get-date)
"Created Image $imageName - Total Run Time: " + $elapsedTime.Minutes + " minutes, " + $elapsedTime.Seconds + " seconds..." 