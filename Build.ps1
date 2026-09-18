param([ValidateSet('Check','Windows','Android','AndroidBundle','IOS')][string]$Target='Check',[string]$Unity='C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$method=if($Target -eq 'Check'){'PrepareAndValidate'}else{$Target}
New-Item -ItemType Directory -Force (Join-Path $PSScriptRoot 'Builds') | Out-Null
$log=Join-Path $PSScriptRoot ('Builds/build-'+$Target+'.log')
$process=Start-Process -FilePath $Unity -ArgumentList @('-batchmode','-nographics','-quit','-projectPath',('"'+$PSScriptRoot+'"'),'-executeMethod',('ArrowFlow.Editor.BuildTools.'+$method),'-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru -Wait
if($process.ExitCode -ne 0){throw "Unity failed. See $log"}
Write-Output "Finished. See $log"
