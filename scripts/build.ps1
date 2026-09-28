param([switch]$Test)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $framework 'csc.exe'
$wpf = Join-Path $framework 'WPF'
$out = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$refs = @('System.dll','System.Xml.dll','System.Core.dll','System.Runtime.Serialization.dll','System.Xaml.dll','System.Windows.Forms.dll','System.Drawing.dll') | ForEach-Object { '/r:' + (Join-Path $framework $_) }
$refs += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/r:' + (Join-Path $wpf $_) }
$common = @('/nologo','/utf8output','/optimize+','/platform:x64','/warn:4') + $refs
& $csc @common /target:library "/out:$out\Kora.Core.dll" "$root\src\Core.cs" "$root\src\Ui.cs" "$root\src\DesktopHost.cs"
if ($LASTEXITCODE -ne 0) { throw 'Core build failed' }
foreach ($app in @(@('Kora','Manager'),@('Kora.Widget','Widget'))) {
  & $csc @common /target:winexe "/win32icon:$root\docs\assets\Kora.ico" "/win32manifest:$root\src\app.manifest" "/r:$out\Kora.Core.dll" "/out:$out\$($app[0]).exe" "$root\src\$($app[1]).cs"
  if ($LASTEXITCODE -ne 0) { throw "$($app[0]) build failed" }
  Copy-Item -LiteralPath "$root\src\App.config" -Destination "$out\$($app[0]).exe.config" -Force
}
if ($Test) {
  & $csc @common /target:exe "/r:$out\Kora.Core.dll" "/out:$out\Kora.Tests.exe" "$root\tests\Tests.cs"
  if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
  & "$out\Kora.Tests.exe"
  if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
}
Copy-Item -LiteralPath "$root\docs\assets\Kora.ico" -Destination "$out\Kora.ico" -Force
Write-Host "Build complete: $out"
