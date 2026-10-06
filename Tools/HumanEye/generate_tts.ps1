# Generates PROVISIONAL development narration for every row of Docs/HumanEye/VO/HumanEye_VO.csv
# using a locally installed, Windows-licensed OneCore voice. Spoken text is exactly the CSV text;
# SSML is used only for delivery (per-sentence rate/pitch, emphasis on anatomy terms, short breaks), never to change words.
# Clips are then trimmed (60 ms lead-in, 120 ms tail, internal pauses capped at 220 ms) and loudness-normalised,
# so narration follows one line with another without dead air. Replace with recorded VO later (same filenames).
# Usage (Windows PowerShell 5.1):
#   powershell -File Tools/HumanEye/generate_tts.ps1                       # Microsoft Mark into Assets/HumanEye/Audio/VO
#   powershell -File Tools/HumanEye/generate_tts.ps1 -Voice "Microsoft Zira" -OutDir Logs/vo-alt/zira   # audition set
#   powershell -File Tools/HumanEye/generate_tts.ps1 -Line "Now you've seen the whole journey." -LineFile niko_closing -OutDir Assets/HumanEye/Audio/SFX   # one supplemental line
param([string]$Voice = "Microsoft Mark", [string]$Rate = "1.02", [string]$Pitch = "+5%", [string]$OutDir = "", [switch]$NoLog, [string]$Line = "", [string]$LineFile = "niko_closing")
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$csv = Join-Path $root "Docs\HumanEye\VO\HumanEye_VO.csv"
$outDir = if ($OutDir) { Join-Path $root $OutDir } else { Join-Path $root "Assets\HumanEye\Audio\VO" }
$rawDir = Join-Path $env:TEMP "humaneye_tts_raw"
New-Item -ItemType Directory -Force $outDir, $rawDir | Out-Null

Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Media.SpeechSynthesis, ContentType = WindowsRuntime]
$null = [Windows.Storage.Streams.DataReader, Windows.Storage.Streams, ContentType = WindowsRuntime]
$asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
function Await($op, [type]$type) { $t = $asTask.MakeGenericMethod($type).Invoke($null, @($op)); $t.Wait(-1) | Out-Null; $t.Result }

# Anatomy terms get moderate emphasis (the word itself is unchanged).
$terms = @("sclera", "cornea", "iris", "pupil", "aqueous humour", "vitreous humour", "crystalline lens", "lens", "retina", "choroid",
           "optic nerve", "photoreceptors", "rods", "cones", "accommodation", "inverted", "brain", "electrical signals")
function Markup([string]$text, [string]$kind) {
    $escaped = [Security.SecurityElement]::Escape($text)
    foreach ($term in $terms) {
        $escaped = [regex]::Replace($escaped, "(?i)\b(" + [regex]::Escape($term) + ")\b", '<emphasis level="moderate">$1</emphasis>')
    }
    # Sentence-level delivery: exclamations lift, questions rise, prompts slow down, ellipses breathe.
    $parts = [regex]::Split($escaped, '(?<=[.!?…])\s+')
    $sb = New-Object Text.StringBuilder
    $sentences = $parts | Where-Object { $_.Trim().Length -gt 0 }
    $i = 0
    foreach ($s in $sentences) {
        $i++
        $r = $Rate; $p = $Pitch
        if ($s -match '!\s*$') { $r = ([double]$Rate + 0.06).ToString([Globalization.CultureInfo]::InvariantCulture); $p = "+9%" }
        elseif ($s -match '\?\s*$') { $r = ([double]$Rate - 0.03).ToString([Globalization.CultureInfo]::InvariantCulture); $p = "+7%" }
        elseif ($kind -eq "PromptVO") { $r = ([double]$Rate - 0.05).ToString([Globalization.CultureInfo]::InvariantCulture) }
        if ($s.Length -gt 90) { $r = ([double]$r - 0.02).ToString([Globalization.CultureInfo]::InvariantCulture) }
        $null = $sb.Append("<prosody rate='$r' pitch='$p'>$s</prosody>")
        if ($i -lt $sentences.Count) { $null = $sb.Append($(if ($s -match '…\s*$') { "<break time='320ms'/>" } else { "<break time='140ms'/>" })) }
    }
    return $sb.ToString()
}

$synth = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
$synth.Voice = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices | Where-Object { $_.DisplayName -eq $Voice } | Select-Object -First 1
if (-not $synth.Voice) { throw "Voice '$Voice' is not installed." }
$lang = $synth.Voice.Language
$rows = if ($Line) { @([pscustomobject]@{ filename = $LineFile; text = $Line }) } else { Import-Csv $csv -Encoding UTF8 }
$manifest = Import-Csv (Join-Path $root "Docs\HumanEye\VO\VO_MANIFEST.csv") -Encoding UTF8
$log = @()
foreach ($row in $rows) {
    $kind = ($manifest | Where-Object { $_.filename -eq $row.filename } | Select-Object -First 1).type
    $ssml = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='$lang'>" + (Markup $row.text $kind) + "</speak>"
    $stream = Await ($synth.SynthesizeSsmlToStreamAsync($ssml)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
    $reader = New-Object Windows.Storage.Streams.DataReader($stream.GetInputStreamAt(0))
    $null = Await ($reader.LoadAsync([uint32]$stream.Size)) ([uint32])
    $bytes = New-Object byte[] $stream.Size
    $reader.ReadBytes($bytes)
    $raw = Join-Path $rawDir ($row.filename + ".wav")
    [IO.File]::WriteAllBytes($raw, $bytes)
    $out = Join-Path $outDir ($row.filename + ".wav")
    # Trim: lead-in to 60 ms, tail to 120 ms, internal pauses longer than 320 ms shortened to ~220 ms; then loudness-normalise.
    $filters = "silenceremove=start_periods=1:start_threshold=-50dB:start_silence=0.06," +
               "areverse,silenceremove=start_periods=1:start_threshold=-50dB:start_silence=0.12,areverse," +
               "silenceremove=stop_periods=-1:stop_duration=0.22:stop_threshold=-45dB," +
               "loudnorm=I=-16:TP=-1.5:LRA=11"
    & ffmpeg -y -loglevel error -i $raw -af $filters -ar 48000 -ac 1 -sample_fmt s16 $out
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed for $($row.filename)" }
    $log += "$($row.filename),$Voice,$lang,provisional_tts_prosody"
}
if (-not $NoLog -and -not $OutDir -and -not $Line) {
    "filename,voice,language,status" | Set-Content -Encoding UTF8 (Join-Path $root "Docs\HumanEye\VO\TTS_LOG.csv")
    $log | Add-Content -Encoding UTF8 (Join-Path $root "Docs\HumanEye\VO\TTS_LOG.csv")
}
Write-Output "Generated $($log.Count) provisional clips with $Voice ($lang) into $outDir"
