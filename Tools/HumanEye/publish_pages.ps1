# Publish only a verified Unity WebGL build. Preparation commits in a NEW checkout; -Push publishes that commit.
param(
    [Parameter(Mandatory = $true)][string]$Site,
    [Parameter(Mandatory = $true)][string]$WorkDir,
    [string]$Remote,
    [switch]$Push
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')).TrimEnd('\', '/')
$source = (Resolve-Path -LiteralPath $Site).Path.TrimEnd('\', '/')
$output = [IO.Path]::GetFullPath($WorkDir).TrimEnd('\', '/')
function IsWithin([string]$Path, [string]$Root) {
    $Path.Equals($Root, [StringComparison]::OrdinalIgnoreCase) -or $Path.StartsWith($Root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}
if ((Test-Path -LiteralPath $output) -or (IsWithin $output $project) -or (IsWithin $output $source)) {
    throw 'WorkDir must be a new directory outside the project and the source site. Existing files are never removed.'
}
if (!(Test-Path -LiteralPath (Join-Path $source 'index.html') -PathType Leaf) -or !(Test-Path -LiteralPath (Join-Path $source 'Build') -PathType Container)) {
    throw 'Site must be the completed WebGL output containing index.html and Build/.'
}
$sourceItems = @(Get-ChildItem -LiteralPath $source -Recurse -Force)
if ($sourceItems | Where-Object { $_.Name -in @('.git', '.gitattributes') -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) }) {
    throw 'Site must contain plain build files, without Git metadata, attribute rules or links.'
}
function Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    $result = & git.exe @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Git failed: $($result -join [Environment]::NewLine)" }
    $result | ForEach-Object { "$_" }
}
if (!$Remote) { $Remote = (Git @('-C', $project, 'remote', 'get-url', 'origin') | Select-Object -Last 1).Trim() }
Git @('-c', 'core.autocrlf=false', '-c', 'core.attributesFile=NUL', 'clone', '--single-branch', '--branch', 'gh-pages', '--', $Remote, $output) | Out-Null
Git @('-C', $output, 'config', 'core.autocrlf', 'false') | Out-Null
Git @('-C', $output, 'config', 'core.attributesFile', 'NUL') | Out-Null
if (Get-ChildItem -LiteralPath $output -Filter '.gitattributes' -Recurse -Force | Where-Object { !(IsWithin $_.FullName (Join-Path $output '.git')) }) {
    throw 'The Pages branch contains attribute rules; do not publish LFS pointers as a website.'
}
$parent = (Git @('-C', $output, 'rev-parse', 'HEAD') | Select-Object -Last 1).Trim()
# Only generated Unity paths are replaced. A remote CNAME, notes and other manual files stay intact.
Git @('-C', $output, 'rm', '-r', '--ignore-unmatch', '--', 'Build', 'TemplateData', 'StreamingAssets', 'index.html', 'THIRD_PARTY_NOTICES.md', 'favicon.ico', '.nojekyll') | Out-Null
$files = @($sourceItems | Where-Object { !$_.PSIsContainer })
foreach ($file in $files) {
    $relative = $file.FullName.Substring($source.Length + 1)
    $target = Join-Path $output $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    [IO.File]::Copy($file.FullName, $target, $true)
}
[IO.File]::WriteAllBytes((Join-Path $output '.nojekyll'), [byte[]]@())
$split = 0
foreach ($file in $files) {
    $relative = $file.FullName.Substring($source.Length + 1)
    $target = Join-Path $output $relative
    $sourceHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sourceHash) { throw "Copy hash mismatch: $relative" }
    if ($file.Length -le 90MB) { continue }
    if ($relative -notmatch '^Build[\\/].*\.data(?:\.unityweb|\.br|\.gz)?$') {
        throw "Only data files can be split by the existing loader. Other oversized file: $relative"
    }
    $parts = [Collections.Generic.List[string]]::new()
    $hashes = [ordered]@{}
    $buffer = [byte[]]::new(45MB)
    $inputFile = [IO.File]::OpenRead($target)
    try {
        while ($inputFile.Position -lt $inputFile.Length) {
            $count = 0
            while ($count -lt $buffer.Length) {
                $read = $inputFile.Read($buffer, $count, $buffer.Length - $count)
                if ($read -eq 0) { break }
                $count += $read
            }
            $part = "$target.part$($parts.Count)"
            $stream = [IO.File]::Create($part)
            try { $stream.Write($buffer, 0, $count) } finally { $stream.Dispose() }
            $name = [IO.Path]::GetFileName($part)
            $parts.Add($name)
            $hashes[$name] = (Get-FileHash -LiteralPath $part -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    } finally { $inputFile.Dispose() }
    $manifest = [ordered]@{ parts = $parts.ToArray(); size = $file.Length; sha256 = $sourceHash; partSha256 = $hashes }
    [IO.File]::WriteAllText("$target.parts.json", ($manifest | ConvertTo-Json -Depth 4 -Compress), [Text.UTF8Encoding]::new($false))
    # Verify the bytes reconstructed in the same part order the loader uses, before dropping the unsplit copy.
    $check = Get-Content -LiteralPath "$target.parts.json" -Raw | ConvertFrom-Json
    $sha = [Security.Cryptography.SHA256]::Create()
    $readBuffer = [byte[]]::new(1MB)
    [long]$total = 0
    try {
        foreach ($name in $check.parts) {
            $part = Join-Path ([IO.Path]::GetDirectoryName($target)) $name
            if ((Get-FileHash -LiteralPath $part -Algorithm SHA256).Hash.ToLowerInvariant() -ne $check.partSha256.$name) { throw "Part hash mismatch: $name" }
            $stream = [IO.File]::OpenRead($part)
            try {
                while (($read = $stream.Read($readBuffer, 0, $readBuffer.Length)) -gt 0) {
                    $null = $sha.TransformBlock($readBuffer, 0, $read, $null, 0); $total += $read
                }
            } finally { $stream.Dispose() }
        }
        $null = $sha.TransformFinalBlock([byte[]]@(), 0, 0)
        $joinedHash = [BitConverter]::ToString($sha.Hash).Replace('-', '').ToLowerInvariant()
        if ($total -ne $check.size -or $joinedHash -ne $check.sha256 -or $joinedHash -ne $sourceHash) { throw "Reconstructed hash mismatch: $relative" }
    } finally { $sha.Dispose() }
    if (!(IsWithin ([IO.Path]::GetFullPath($target)) $output)) { throw 'Split target escaped the new checkout.' }
    Remove-Item -LiteralPath $target
    $split++
}
$siteFiles = @(Get-ChildItem -LiteralPath $output -Recurse -File -Force | Where-Object { !(IsWithin $_.FullName (Join-Path $output '.git')) })
if ($siteFiles | Where-Object Length -gt 95MB) { throw 'Prepared site has a file over the 95 MiB safety limit.' }
Git @('-C', $output, 'add', '--all', '--') | Out-Null
& git.exe -C $output diff --cached --quiet
if ($LASTEXITCODE -eq 1) { Git @('-C', $output, 'commit', '-m', 'Publish verified Human Eye WebGL build') | Out-Null }
elseif ($LASTEXITCODE -ne 0) { throw 'Could not inspect prepared changes.' }
$head = (Git @('-C', $output, 'rev-parse', 'HEAD') | Select-Object -Last 1).Trim()
if ($head -ne $parent -and (Git @('-C', $output, 'rev-parse', 'HEAD^') | Select-Object -Last 1).Trim() -ne $parent) { throw 'Publish commit did not retain the current Pages parent.' }
if ($Push) {
    Git @('-C', $output, 'push', 'origin', 'HEAD:refs/heads/gh-pages') | Out-Null
    $remoteHead = ((Git @('-C', $output, 'ls-remote', 'origin', 'refs/heads/gh-pages') | Select-Object -Last 1) -split '\s+')[0]
    if ($remoteHead -ne $head) { throw "Remote verification failed: expected $head, got $remoteHead" }
}
[PSCustomObject]@{ Prepared = $output; Parent = $parent; Commit = $head; Pushed = [bool]$Push; Files = $siteFiles.Count; SplitFiles = $split; MiB = [math]::Round(($siteFiles | Measure-Object Length -Sum).Sum / 1MB, 2) }
