<#
.SYNOPSIS
  构建 Baseline 的发布产物（自包含单文件 + 框架依赖版），可选直接建 GitHub release。

.DESCRIPTION
  固化两条容易踩坑的 publish 命令：
    - 框架依赖版必须用 --no-self-contained（--self-contained false 在当前 SDK 不生效，会打成 ~166MB 自包含）
    - 自包含单文件需 IncludeNativeLibrariesForSelfExtract + EnableCompressionInSingleFile 才能从 ~166MB 压到 ~73MB
  两次构建之间清理 bin/Release、obj/Release，避免复用上一次的二进制。

.PARAMETER Version
  版本号，如 1.3.0。用于 fd zip 文件名与（-Release 时）tag/标题。

.PARAMETER Release
  额外执行 gh release create，上传两个资产。需配合 -NotesFile。

.PARAMETER NotesFile
  发布说明 markdown 路径（-Release 时必填）。

.EXAMPLE
  pwsh scripts/publish.ps1 -Version 1.3.0
  pwsh scripts/publish.ps1 -Version 1.3.0 -Release -NotesFile publish/notes-1.3.0.md
#>
param(
  [Parameter(Mandatory=$true)][string]$Version,
  [switch]$Release,
  [string]$NotesFile
)
$ErrorActionPreference = 'Stop'

$Root    = Split-Path $PSScriptRoot -Parent
$Proj    = Join-Path $Root 'src\Baseline\Baseline.csproj'
$Pub     = Join-Path $Root 'publish'
$ScDir   = Join-Path $Pub  'self-contained'
$FdDir   = Join-Path $Pub  'framework-dependent'
$Zip     = Join-Path $Pub  "Baseline-$Version-framework-dependent.zip"
$RID     = 'win-x64'

function Clear-Build {
  Remove-Item (Join-Path $Root 'src\Baseline\bin\Release') -Recurse -Force -ErrorAction SilentlyContinue
  Remove-Item (Join-Path $Root 'src\Baseline\obj\Release') -Recurse -Force -ErrorAction SilentlyContinue
}

if($Release -and -not $NotesFile){ throw '-Release 需要同时指定 -NotesFile' }

# 发版前同步远端：gh release create 会在远端分支 HEAD 上打 tag，若本地有未推送的提交，
# tag 会落到旧代码上、与上传的二进制不一致（v1.4.0 首发就踩过）。先确保跟踪文件无未提交
# 改动（忽略 publish/ 等未跟踪产物），再把当前分支推上去。
if($Release){
  $dirty = git status --porcelain -uno
  if($dirty){ throw "工作区有未提交的改动，发版前请先提交或清理：`n$dirty" }
  $branch = (git rev-parse --abbrev-ref HEAD).Trim()
  Write-Host "推送 $branch 到 origin ..." -ForegroundColor Cyan
  git push origin $branch
  if($LASTEXITCODE -ne 0){ throw 'git push 失败，发版中止' }
}

# 1) 框架依赖版（--no-self-contained 是关键）
Write-Host "[1/3] 构建框架依赖版 ..." -ForegroundColor Cyan
Remove-Item $FdDir -Recurse -Force -ErrorAction SilentlyContinue
Clear-Build
dotnet publish $Proj -c Release -r $RID --no-self-contained -p:PublishSingleFile=true -o $FdDir
if($LASTEXITCODE -ne 0){ throw 'framework-dependent 构建失败' }

# 2) 自包含单文件（原生内嵌 + 压缩）
Write-Host "[2/3] 构建自包含单文件 ..." -ForegroundColor Cyan
Remove-Item $ScDir -Recurse -Force -ErrorAction SilentlyContinue
Clear-Build
dotnet publish $Proj -c Release -r $RID --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -o $ScDir
if($LASTEXITCODE -ne 0){ throw 'self-contained 构建失败' }

# 3) 打 fd zip（exe + 原生 dll，排除 pdb）
Write-Host "[3/3] 打包框架依赖版 zip ..." -ForegroundColor Cyan
if(Test-Path $Zip){ Remove-Item $Zip -Force }
$fdFiles = Get-ChildItem $FdDir -File | Where-Object { $_.Extension -ne '.pdb' }
Compress-Archive -Path $fdFiles.FullName -DestinationPath $Zip

$scExe = Join-Path $ScDir 'Baseline.exe'
"`n产物就绪："
"  自包含单文件 : {0}  ({1:N1} MB)" -f $scExe, ((Get-Item $scExe).Length/1MB)
"  框架依赖 zip : {0}  ({1:N1} MB)" -f $Zip,   ((Get-Item $Zip).Length/1MB)

if($Release){
  $notesPath = if([IO.Path]::IsPathRooted($NotesFile)){ $NotesFile } else { Join-Path $Root $NotesFile }
  if(-not (Test-Path $notesPath)){ throw "找不到发布说明: $notesPath" }
  Write-Host "`n创建 GitHub release v$Version ..." -ForegroundColor Cyan
  # --target 钉到当前 commit，避免 tag 落到远端分支的其他 HEAD 上。
  $head = (git rev-parse HEAD).Trim()
  gh release create "v$Version" --target $head --title "Baseline v$Version" --notes-file $notesPath $scExe $Zip
  if($LASTEXITCODE -ne 0){ throw 'gh release create 失败' }
  Write-Host "release 已发布" -ForegroundColor Green
}
