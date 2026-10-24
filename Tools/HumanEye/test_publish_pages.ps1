# Local-only publisher check: no network, no source-repository staging, no files removed on completion.
$ErrorActionPreference = 'Stop'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('human-eye-pages-' + [guid]::NewGuid().ToString('N'))
$seed = Join-Path $fixture 'seed'; $remote = Join-Path $fixture 'remote.git'; $site = Join-Path $fixture 'site'
New-Item -ItemType Directory -Path $seed, $site, (Join-Path $site 'Build'), (Join-Path $site 'TemplateData') -Force | Out-Null
function FixtureGit {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    $null = & git.exe @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Fixture Git failed: $Arguments" }
}
FixtureGit @('init', '--bare', $remote)
FixtureGit @('-C', $seed, 'init', '-b', 'gh-pages')
FixtureGit @('-C', $seed, 'config', 'user.name', 'Publisher fixture')
FixtureGit @('-C', $seed, 'config', 'user.email', 'fixture@example.invalid')
FixtureGit @('-C', $seed, 'config', 'core.autocrlf', 'false')
[IO.Directory]::CreateDirectory((Join-Path $seed 'Build')) | Out-Null
[IO.Directory]::CreateDirectory((Join-Path $seed 'TemplateData')) | Out-Null
[IO.File]::WriteAllText((Join-Path $seed 'Build/old.data'), 'old build')
[IO.File]::WriteAllText((Join-Path $seed 'index.html'), "<!doctype html>`n<html>old page</html>`n")
[IO.File]::WriteAllText((Join-Path $seed 'TemplateData/loader.js'), "function oldLoader() {`n  return 1;`n}`n")
[IO.File]::WriteAllText((Join-Path $seed 'TemplateData/loader.css'), "body {`n  color: navy;`n}`n")
[IO.File]::WriteAllText((Join-Path $seed 'CNAME'), 'fixture.example.invalid')
[IO.File]::WriteAllText((Join-Path $seed 'owner-note.txt'), 'keep this manual file')
FixtureGit @('-C', $seed, 'add', '--all')
FixtureGit @('-C', $seed, 'commit', '-m', 'Existing Pages history')
FixtureGit @('-C', $seed, 'remote', 'add', 'origin', $remote)
FixtureGit @('-C', $seed, 'push', 'origin', 'gh-pages')
[IO.File]::WriteAllText((Join-Path $site 'index.html'), "<!doctype html>`n<html>verified fixture</html>`n")
[IO.File]::WriteAllText((Join-Path $site 'TemplateData/loader.js'), "function newLoader() {`n  return 2;`n}`n")
[IO.File]::WriteAllText((Join-Path $site 'TemplateData/loader.css'), "body {`n  color: cyan;`n}`n")
$data = Join-Path $site 'Build/test.data.unityweb'
$stream = [IO.File]::Create($data); try { $stream.SetLength(91MB) } finally { $stream.Dispose() }
$authorName = $env:GIT_AUTHOR_NAME; $authorEmail = $env:GIT_AUTHOR_EMAIL
$committerName = $env:GIT_COMMITTER_NAME; $committerEmail = $env:GIT_COMMITTER_EMAIL
$gitConfigGlobal = $env:GIT_CONFIG_GLOBAL
$windowsConfig = Join-Path $fixture 'windows.gitconfig'
[IO.File]::WriteAllText($windowsConfig, "[core]`n  autocrlf = true`n")
try {
    $env:GIT_AUTHOR_NAME = $env:GIT_COMMITTER_NAME = 'Publisher fixture'
    $env:GIT_AUTHOR_EMAIL = $env:GIT_COMMITTER_EMAIL = 'fixture@example.invalid'
    $env:GIT_CONFIG_GLOBAL = $windowsConfig
    if ((& git.exe config --global --get core.autocrlf).Trim() -ne 'true') { throw 'The Windows checkout regression setting was not applied.' }
    $parent = (& git.exe -C $seed rev-parse HEAD).Trim()
    $prepared = & (Join-Path $PSScriptRoot 'publish_pages.ps1') -Site $site -WorkDir (Join-Path $fixture 'prepared') -Remote $remote
    if ($prepared.Pushed -or $prepared.Parent -ne $parent -or $prepared.SplitFiles -ne 1) { throw 'Preparation did not retain history or split the large data file.' }
    $remoteBefore = ((& git.exe ls-remote $remote refs/heads/gh-pages) -split '\s+')[0]
    if ($remoteBefore -ne $parent) { throw 'Preparation unexpectedly pushed.' }
    foreach ($name in @('CNAME', 'owner-note.txt', '.nojekyll', 'Build/test.data.unityweb.parts.json')) {
        if (!(Test-Path -LiteralPath (Join-Path $prepared.Prepared $name))) { throw "Expected file missing: $name" }
    }
    if (Test-Path -LiteralPath (Join-Path $prepared.Prepared 'Build/old.data')) { throw 'Stale generated data survived.' }
    foreach ($name in @('index.html', 'TemplateData/loader.js', 'TemplateData/loader.css')) {
        $copy = Join-Path $prepared.Prepared $name
        if (([IO.File]::ReadAllBytes($copy) -contains [byte]13) -or (Get-FileHash -LiteralPath $copy).Hash -ne (Get-FileHash -LiteralPath (Join-Path $site $name)).Hash) { throw "Multiline LF bytes changed under Windows autocrlf: $name" }
    }
    $manifest = Get-Content -LiteralPath (Join-Path $prepared.Prepared 'Build/test.data.unityweb.parts.json') -Raw | ConvertFrom-Json
    if ($manifest.parts.Count -ne 3 -or $manifest.size -ne 91MB -or $manifest.sha256 -ne (Get-FileHash -LiteralPath $data).Hash.ToLowerInvariant()) { throw 'Split manifest is incorrect.' }
    $refused = $false
    try { & (Join-Path $PSScriptRoot 'publish_pages.ps1') -Site $site -WorkDir $seed -Remote $remote | Out-Null } catch { $refused = $true }
    if (!$refused -or (Get-Content -LiteralPath (Join-Path $seed 'owner-note.txt') -Raw) -ne 'keep this manual file') { throw 'Existing owner files were not protected.' }
    $published = & (Join-Path $PSScriptRoot 'publish_pages.ps1') -Site $site -WorkDir (Join-Path $fixture 'published') -Remote $remote -Push
    $remoteAfter = ((& git.exe ls-remote $remote refs/heads/gh-pages) -split '\s+')[0]
    if (!$published.Pushed -or $remoteAfter -ne $published.Commit -or $published.Parent -ne $parent) { throw 'Ordinary local-remote push or verification failed.' }
    "PASS: Windows autocrlf=true with multiline LF templates, preparation, existing-folder refusal, retained history, source integrity, 91 MiB split hashes and ordinary fake-remote push. Fixture: $fixture"
}
finally {
    $env:GIT_AUTHOR_NAME = $authorName; $env:GIT_AUTHOR_EMAIL = $authorEmail
    $env:GIT_COMMITTER_NAME = $committerName; $env:GIT_COMMITTER_EMAIL = $committerEmail
    $env:GIT_CONFIG_GLOBAL = $gitConfigGlobal
}
