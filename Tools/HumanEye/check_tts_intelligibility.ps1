# Rough, automated intelligibility check of the provisional TTS (NOT a substitute for human pronunciation review):
# runs the local Windows desktop speech recognizer (dictation) over each clip and reports word recall vs the exact text.
param([string]$Culture = "en-US")
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Add-Type -AssemblyName System.Speech
$rec = New-Object System.Speech.Recognition.SpeechRecognitionEngine([Globalization.CultureInfo]::GetCultureInfo($Culture))
$rec.LoadGrammar((New-Object System.Speech.Recognition.DictationGrammar))
$rows = Import-Csv (Join-Path $root "Docs\HumanEye\VO\HumanEye_VO.csv") -Encoding UTF8
$tmp = Join-Path $env:TEMP "he_asr.wav"
$out = @("filename,recall,heard")
$terms = @{}
foreach ($r in $rows) {
    $wav = Join-Path $root ("Assets\HumanEye\Audio\VO\" + $r.filename + ".wav")
    & ffmpeg -y -loglevel error -i $wav -ar 16000 -ac 1 -sample_fmt s16 $tmp
    $rec.SetInputToWaveFile($tmp)
    $heard = ""
    while ($true) { try { $res = $rec.Recognize() } catch { break }; if ($res -eq $null) { break }; $heard += " " + $res.Text }
    $rec.SetInputToNull()
    $want = ($r.text.ToLower() -replace "[^a-z ]", " ") -split "\s+" | Where-Object { $_.Length -gt 2 }
    $got = ($heard.ToLower() -replace "[^a-z ]", " ") -split "\s+"
    $hit = ($want | Where-Object { $got -contains $_ }).Count
    $recall = if ($want.Count) { [math]::Round($hit / $want.Count, 2) } else { 1 }
    foreach ($t in @("sclera","cornea","iris","pupil","aqueous","vitreous","crystalline","biconvex","retina","choroid","optic","photoreceptors","accommodation","rods","cones")) {
        if ($want -contains $t) { if (-not $terms.ContainsKey($t)) { $terms[$t] = @(0,0) }; $terms[$t][1]++; if ($got -contains $t) { $terms[$t][0]++ } }
    }
    $out += ('{0},{1},"{2}"' -f $r.filename, $recall, $heard.Trim().Replace('"',"'"))
}
$out | Set-Content -Encoding UTF8 (Join-Path $root "Docs\HumanEye\VO\TTS_INTELLIGIBILITY.csv")
"term,recognised,occurrences" | Set-Content -Encoding UTF8 (Join-Path $root "Docs\HumanEye\VO\TTS_TERMS.csv")
foreach ($k in $terms.Keys | Sort-Object) { "$k,$($terms[$k][0]),$($terms[$k][1])" | Add-Content -Encoding UTF8 (Join-Path $root "Docs\HumanEye\VO\TTS_TERMS.csv") }
$avg = ($out | Select-Object -Skip 1 | ForEach-Object { [double]($_.Split(',')[1]) } | Measure-Object -Average).Average
"Average word recall: {0:N2} over {1} clips (recognizer: {2})" -f $avg, $rows.Count, $Culture

