param([string]$Version='1.1.1')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
& "$PSScriptRoot\build.ps1" -Test
$release=Join-Path $root 'release'
$folder=Join-Path $release "KoraDesk-v$Version-win-x64"
if(Test-Path -LiteralPath $folder){throw 'Package folder already exists. Use a new version or archive the previous build.'}
New-Item -ItemType Directory -Path $folder -Force | Out-Null
foreach($name in @('Kora.exe','Kora.exe.config','Kora.Widget.exe','Kora.Widget.exe.config','Kora.Core.dll','Kora.ico')) {Copy-Item -LiteralPath (Join-Path "$root\dist" $name) -Destination $folder}
Copy-Item -LiteralPath "$root\LICENSE" -Destination $folder
Copy-Item -LiteralPath "$root\dist\fonts" -Destination (Join-Path $folder 'fonts') -Recurse
@"
Kora Desk · 刻序

1. 完整解压后，双击 Kora.exe。
2. 在桌面组件上直接勾选任务。
3. 关闭管理窗口，组件仍会继续运行。
4. 组件右上角 ··· 可切换置顶；右键托盘图标可退出。
5. 偏好设置里可以选择五种界面风格，以及浅色、深色或跟随系统。

所有任务都可在管理窗口编辑。数据保存在本机 LocalAppData\KoraDesk。
仅启动组件时，运行 Kora.Widget.exe。
fonts 文件夹里是点阵风格使用的字体，请和程序放在一起。

Windows 10/11 x64 · .NET Framework 4.8+
项目与完整说明：https://github.com/Cennyk-learner/kora-desk
"@ | Set-Content -LiteralPath (Join-Path $folder '开始使用.txt') -Encoding UTF8
$zip="$folder.zip"
Compress-Archive -LiteralPath $folder -DestinationPath $zip
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $zip -Leaf)" | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Encoding ASCII
Write-Host "Ready: $zip"
