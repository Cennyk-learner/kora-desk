param([string]$Out)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $root 'dist'
if (-not $Out) { $Out = Join-Path $dist 'previews' }
foreach ($exe in 'Kora.exe','Kora.Widget.exe') { if (-not (Test-Path (Join-Path $dist $exe))) { throw "Run scripts\build.ps1 first: $exe is missing." } }
New-Item -ItemType Directory -Path $Out -Force | Out-Null
# Previews use a throwaway data folder with the example plan, never the real one.
$data = Join-Path ([IO.Path]::GetTempPath()) ('KoraPreview-' + [Guid]::NewGuid().ToString('N'))
$env:KORA_DATA_DIR = $data
try {
  foreach ($theme in 'mac','fluent','material','carbon','dot') {
    foreach ($look in 'light','dark') {
      $tag = "$theme-$look"
      foreach ($page in 0,1,2) {
        $p = Start-Process -FilePath (Join-Path $dist 'Kora.exe') -ArgumentList '--preview',"--theme=$theme","--appearance=$look","--page=$page","--out=$Out\manager-$tag-page$page.png" -Wait -PassThru
        if ($p.ExitCode -ne 0) { Write-Warning "Planner preview failed: $tag page $page" }
      }
      $p = Start-Process -FilePath (Join-Path $dist 'Kora.Widget.exe') -ArgumentList '--preview',"--theme=$theme","--appearance=$look","--out=$Out\widget-$tag.png" -Wait -PassThru
      if ($p.ExitCode -ne 0) { Write-Warning "Widget preview failed: $tag" }
    }
  }
} finally {
  Remove-Item Env:\KORA_DATA_DIR -ErrorAction SilentlyContinue
  if (Test-Path -LiteralPath $data) { Remove-Item -LiteralPath $data -Recurse -Force }
}
Write-Host "Previews written to $Out"
