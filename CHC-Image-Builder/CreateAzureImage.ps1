param
(
    $vmName = "2702da76-d4c-vm",
    $vmGroupName = "2702da76-d4cc-41bc-aa55-8826588444ad-rg",
    $imgGroupName = "Refereance-Image-Storage-rg",
    $location = "WestUS",
    $imageName = "WIN2K12R2"
)
$ErrorActionPreference = "Stop"

$script:StartTime = $(get-date)

Write-Host "Parameters:"
Write-Host "VmName " $vmName
Write-Host "GroupName", $vmGroupName
Write-Host "Image Group Name", $imgGroupName
Write-Host "Location ", $location
Write-Host "Image Name ", $imageName

Write-Host "Connecting to Azure..."
$applicationId = "f1659788-41eb-439b-9fe2-3e373f870719"
$secpassword = convertto-securestring "hx6NCNBoiKYb+yIKHFWUJqjUXuhYa7kxZ7F1TFohvro=" -AsPlainText -Force
$credential = new-object -typename System.Management.Automation.PSCredential -argumentlist $applicationId, $secpassword

Connect-AzureRMAccount -ServicePrincipal -Credential $credential -TenantId "b37600f0-1e5e-48fb-b34d-d9bdb51cdbc5" -Subscription "a646d321-18de-4209-a895-5c7dec3a9ca0"

Write-Host "Stopping VM $vmName..."
Stop-AzureRmVM -ResourceGroupName $vmGroupName -Name $vmName -Force

Write-Host "Getting VM Disk Information..."
$vm = Get-AzureRmVm -Name $vmName -ResourceGroupName $vmGroupName
$diskID = $vm.StorageProfile.OsDisk.ManagedDisk.Id

Write-Host "Setting VM Disk Information..."
$imageConfig = New-AzureRmImageConfig -Location $location
$imageConfig = Set-AzureRmImageOsDisk -Image $imageConfig -OsState Generalized -OsType Windows -ManagedDiskId $diskID

Write-Host "Saving VM Image..."
$imageName += "-" + (Get-Date -Format s).Replace(":",".")
$imageName
New-AzureRmImage -ImageName $imageName -ResourceGroupName $imgGroupName -Image $imageConfig

$elapsedTime = new-timespan $script:StartTime $(get-date)

"Created Image $imageName - Run Time: " + $elapsedTime.Minutes + " minutes, " + $elapsedTime.Seconds + " seconds..." 