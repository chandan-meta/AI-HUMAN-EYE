param(
    [string]$Run = 'review',
    [int]$Width = 1600,
    [int]$Height = 900,
    [int]$Chapter = 0,
    [int]$Stop = 99,
    [int]$Fast = 6,
    [switch]$Reduced,
    [switch]$Stress
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$capture = Join-Path $project "Builds\Checks\$Run"
$log = Join-Path $project "Logs\polish\$Run.log"
New-Item -ItemType Directory -Force $capture, (Split-Path $log) | Out-Null
$arguments = "-force-d3d11 -screen-width $Width -screen-height $Height -screen-fullscreen 0 -heAutopilot -heOffscreen -hePolish -heFast $Fast -heChapter $Chapter -heStop $Stop -heCapture `"$capture`" -logFile `"$log`""
if ($Reduced) { $arguments += ' -heReduced' }
if ($Stress) { $arguments += ' -heStress' }
$player = Start-Process -FilePath (Join-Path $project 'Builds\HumanEye\Windows\HumanEye.exe') -ArgumentList $arguments -WorkingDirectory $project -WindowStyle Hidden -PassThru
$player.WaitForExit()
"Exit=$($player.ExitCode) Capture=$capture Log=$log"
if ($player.ExitCode -ne 0) { exit $player.ExitCode }
$walkthrough = Join-Path $capture 'walkthrough.log'
if (!(Test-Path $walkthrough) -or !(Select-String -LiteralPath $walkthrough -Pattern '\tdone\t')) { throw 'Walkthrough did not complete.' }
if (Select-String -LiteralPath $walkthrough -Pattern '\tFAIL\t') { throw 'Runtime checks failed; see walkthrough.log.' }
