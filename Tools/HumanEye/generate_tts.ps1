# Generates PROVISIONAL development narration for every row of Docs/HumanEye/VO/HumanEye_VO.csv
# using a locally installed, Windows-licensed OneCore voice. Spoken text is exactly the CSV text;
# SSML is used only for prosody (rate/pitch), never to change words. Replace with recorded VO later.
# Usage (Windows PowerShell 5.1): powershell -File Tools/HumanEye/generate_tts.ps1 [-Voice "Microsoft Ravi"]
param([string]$Voice = "Microsoft Mark", [string]$Rate = "0.95", [string]$Pitch = "+3%")
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$csv = Join-Path $root "Docs\HumanEye\VO\HumanEye_VO.csv"
$outDir = Join-Path $root "Assets\HumanEye\Audio\VO"
$rawDir = Join-Path $env:TEMP "humaneye_tts_raw"
New-Item -ItemType Directory -Force $outDir, $rawDir | Out-Null

Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Media.SpeechSynthesis, ContentType = WindowsRuntime]
$null = [Windows.Storage.Streams.DataReader, Windows.Storage.Streams, ContentType = WindowsRuntime]
$asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
function Await($op, [type]$type) { $t = $asTask.MakeGenericMethod($type).Invoke($null, @($op)); $t.Wait(-1) | Out-Null; $t.Result }

$synth = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
$synth.Voice = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices | Where-Object { $_.DisplayName -eq $Voice } | Select-Object -First 1
if (-not $synth.Voice) { throw "Voice '$Voice' is not installed." }
$lang = $synth.Voice.Language
$rows = Import-Csv $csv -Encoding UTF8
$log = @()
foreach ($row in $rows) {
    $escaped = [Security.SecurityElement]::Escape($row.text)
    $ssml = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='$lang'><prosody rate='$Rate' pitch='$Pitch'>$escaped</prosody></speak>"
    $stream = Await ($synth.SynthesizeSsmlToStreamAsync($ssml)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
    $reader = New-Object Windows.Storage.Streams.DataReader($stream.GetInputStreamAt(0))
    $null = Await ($reader.LoadAsync([uint32]$stream.Size)) ([uint32])
    $bytes = New-Object byte[] $stream.Size
    $reader.ReadBytes($bytes)
    $raw = Join-Path $rawDir ($row.filename + ".wav")
    [IO.File]::WriteAllBytes($raw, $bytes)
    $out = Join-Path $outDir ($row.filename + ".wav")
    & ffmpeg -y -loglevel error -i $raw -af "adelay=60,apad=pad_dur=0.2,loudnorm=I=-16:TP=-1.5:LRA=11" -ar 48000 -ac 1 -sample_fmt s16 $out
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed for $($row.filename)" }
    $log += "$($row.filename),$Voice,$lang,provisional_tts"
}
"filename,voice,language,status" | Set-Content -Encoding UTF8 (Join-Path $root "Docs\HumanEye\VO\TTS_LOG.csv")
$log | Add-Content -Encoding UTF8 (Join-Path $root "Docs\HumanEye\VO\TTS_LOG.csv")
Write-Output "Generated $($log.Count) provisional clips with $Voice ($lang)"
