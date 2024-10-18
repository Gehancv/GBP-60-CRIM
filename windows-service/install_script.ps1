$serviceName = "GBWindowsService"
$exePath = "C:\ACG\Green Bay Camera App\GBP-60-CRIM\windows-service\bin\GBPackingSlipAttachmentService.exe"
$serviceDescription = "This service is responsible for uploading packing slips for receipts in IFS"

if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Write-Host "-------------------Service $serviceName already exists.-------------------"
    sc.exe delete $serviceName
    Write-Host "Service $serviceName removed."    
}   

Write-Host "-------------------Installing Service $serviceName.-------------------"
New-Service -Name $serviceName -BinaryPathName $exePath -Description $serviceDescription -StartupType Manual