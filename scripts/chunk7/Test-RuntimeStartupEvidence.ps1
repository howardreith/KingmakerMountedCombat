[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepositoryRoot,[string]$SourceOverlayRoot,[Parameter(Mandatory=$true)][string]$FixtureRoot,[Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
function StartupSource([string]$relative){if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$p=Join-Path $SourceOverlayRoot $relative;if(Test-Path -LiteralPath $p -PathType Leaf){return $p}};Join-Path $RepositoryRoot $relative}
$common=StartupSource 'scripts/runtime/RuntimeHarness.Common.ps1';$reader=StartupSource 'scripts/runtime/RuntimeStartupEvidence.ps1';$gameReader=StartupSource 'scripts/runtime/Test-RuntimeGameResult.ps1'
$paths=@($PSCommandPath,$common,$reader,$gameReader)+@(Get-ChildItem -LiteralPath $FixtureRoot -File|Sort-Object Name|ForEach-Object{$_.FullName})
$inputs=@($paths|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
$tokens=$null;$errors=$null;$commonAst=[Management.Automation.Language.Parser]::ParseFile($common,[ref]$tokens,[ref]$errors)
if($errors.Count-ne0){throw 'Common parse failed.'}
$exact=@($commonAst.FindAll({param($a)$a-is[Management.Automation.Language.FunctionDefinitionAst]-and$a.Name-ceq'Assert-KmcExactProperties'},$true));if($exact.Count-ne1){throw 'Exact properties helper is unavailable.'}
. ([scriptblock]::Create($exact[0].Extent.Text));. $reader
$tokens=$null;$errors=$null;$gameAst=[Management.Automation.Language.Parser]::ParseFile($gameReader,[ref]$tokens,[ref]$errors)
if($errors.Count-ne0){throw 'Game reader parse failed.'}
$commonAssign=@($gameAst.FindAll({param($a)$a-is[Management.Automation.Language.AssignmentStatementAst]-and$a.Left.Extent.Text-ceq'$commonRequired'},$true));if($commonAssign.Count-ne1){throw 'Common result fields are not one exact assignment.'}
$script:startupPass=0;$script:startupFail=0
function Good-Startup([string]$label,[scriptblock]$body){try{& $body|Out-Null;$script:startupPass++;Write-Output ('PASS '+$label)}catch{$script:startupFail++;Write-Output ('FAIL '+$label+' :: '+$_.Exception.Message)}}
function Bad-Startup([string]$label,[scriptblock]$body){try{& $body|Out-Null;$script:startupFail++;Write-Output ('FAIL '+$label+' :: unexpectedly accepted')}catch{$script:startupPass++;Write-Output ('PASS '+$label)}}
function Read-Startup([string]$name){[IO.File]::ReadAllText((Join-Path $FixtureRoot $name))|ConvertFrom-Json}
foreach($name in @('empty','full','partial','unavailable','terminal-prefix')){$selected=$name;Good-Startup ('actual Capture '+$name) {Assert-KmcRuntimeStartupTrace (Read-Startup ($selected+'.json'))}}
foreach($version in @(1,2)){
 $selectedVersion=$version
 Good-Startup ('actual v'+$version+' result DTO with trace') {$game=Read-Startup ('result-v'+$selectedVersion+'.json');. ([scriptblock]::Create($commonAssign[0].Extent.Text));if(@($commonRequired|Where-Object{$_-ceq'startupTrace'}).Count-ne1){throw 'Startup field missing from actual result shape.'}}
 Good-Startup ('historical v'+$version+' result without trace') {$game=Read-Startup ('result-v'+$selectedVersion+'.json');$game.PSObject.Properties.Remove('startupTrace');. ([scriptblock]::Create($commonAssign[0].Extent.Text));if(@($commonRequired|Where-Object{$_-ceq'startupTrace'}).Count-ne0){throw 'Historical shape changed.'}}
}
$mutations=@(
 [pscustomobject]@{label='schema array';change={param($v)$v.schemaVersion=@(1)}}
 [pscustomobject]@{label='kind array';change={param($v)$v.kind=@('runtime-startup-callback-trace')}}
 [pscustomobject]@{label='clock origin array';change={param($v)$v.clockOrigin=@('accepted-runtime-host')}}
 [pscustomobject]@{label='callback cap raised';change={param($v)$v.callbackCapacity=11}}
 [pscustomobject]@{label='sample cap raised';change={param($v)$v.sampleCapacity=51}}
 [pscustomobject]@{label='zero frequency';change={param($v)$v.clockFrequency=0}}
 [pscustomobject]@{label='fractional frequency';change={param($v)$v.clockFrequency=1.2}}
 [pscustomobject]@{label='count above bound';change={param($v)$v.callbacksObserved=11}}
 [pscustomobject]@{label='wrong closed count';change={param($v)$v.closedCallbacks=9}}
 [pscustomobject]@{label='ordering flag type';change={param($v)$v.orderingFailure='false'}}
 [pscustomobject]@{label='read flag type';change={param($v)$v.readFailure=@($false)}}
 [pscustomobject]@{label='unbounded rows';change={param($v)$v.samples=@($v.samples)+@($v.samples[0])}}
 [pscustomobject]@{label='one object instead of rows';change={param($v)$v.samples=$v.samples[0]}}
 [pscustomobject]@{label='unknown root property';change={param($v)$v|Add-Member -NotePropertyName nativeLoadProven -NotePropertyValue $true}}
 [pscustomobject]@{label='unknown row property';change={param($v)$v.samples[0]|Add-Member -NotePropertyName callbackSucceeded -NotePropertyValue $true}}
 [pscustomobject]@{label='callback array';change={param($v)$v.samples[0].callback=@(1)}}
 [pscustomobject]@{label='phase array';change={param($v)$v.samples[0].phase=@('on-update-entry')}}
 [pscustomobject]@{label='elapsed array';change={param($v)$v.samples[0].elapsedTicks=@(0)}}
 [pscustomobject]@{label='negative elapsed';change={param($v)$v.samples[0].elapsedTicks=-1}}
 [pscustomobject]@{label='clock regresses';change={param($v)$v.samples[1].elapsedTicks=100;$v.samples[2].elapsedTicks=0}}
 [pscustomobject]@{label='frame array';change={param($v)$v.samples[0].unityFrame=@(100)}}
 [pscustomobject]@{label='frame negative';change={param($v)$v.samples[0].unityFrame=-1}}
 [pscustomobject]@{label='focus array';change={param($v)$v.samples[0].isFocused=@($true)}}
 [pscustomobject]@{label='background string';change={param($v)$v.samples[0].runInBackground='false'}}
 [pscustomobject]@{label='missing read flag';change={param($v)$v.samples[0].isFocused=$null}}
 [pscustomobject]@{label='false unavailable claim';change={param($v)$v.readFailure=$true}}
 [pscustomobject]@{label='unknown phase';change={param($v)$v.samples[1].phase='invoke-game'}}
 [pscustomobject]@{label='pre-host end before start';change={param($v)$v.samples[1].phase='pre-host-end'}}
 [pscustomobject]@{label='host before pre-host end';change={param($v)$v.samples[2].phase='host-entry'}}
 [pscustomobject]@{label='callback out of sequence';change={param($v)$v.samples[0].callback=2}}
 [pscustomobject]@{label='foreign callback row';change={param($v)$v.samples[2].callback=2}}
)
foreach($case in $mutations){$selectedCase=$case;Bad-Startup $case.label {$value=Read-Startup 'full.json';& $selectedCase.change $value;Assert-KmcRuntimeStartupTrace $value}}
Bad-Startup 'null trace supplied' {Assert-KmcRuntimeStartupTrace $null}
Bad-Startup 'root matching singleton array' {Assert-KmcRuntimeStartupTrace @((Read-Startup 'full.json'))}
Bad-Startup 'actual result assignment refuses null field' {$game=Read-Startup 'result-v2.json';$game.startupTrace=$null;. ([scriptblock]::Create($commonAssign[0].Extent.Text))}
Good-Startup 'observational ordering failure retains valid prefix' {$value=Read-Startup 'partial.json';$value.orderingFailure=$true;Assert-KmcRuntimeStartupTrace $value}
Good-Startup 'same native frame across callbacks allowed' {$value=Read-Startup 'full.json';foreach($row in $value.samples){$row.unityFrame=100};Assert-KmcRuntimeStartupTrace $value}
$changed=@($inputs|Where-Object{(Get-FileHash -LiteralPath $_.path -Algorithm SHA256).Hash.ToLowerInvariant()-cne$_.sha256})
if($changed.Count-ne0){$script:startupFail++;Write-Output 'FAIL source inputs changed'}
$receipt=[ordered]@{schemaVersion=1;kind='runtime-startup-evidence-actual';status=$(if($startupFail-eq0){'PASS'}else{'FAIL'});passed=$startupPass;failed=$startupFail;inputs=$inputs;changedInputs=$changed;source='Actual optional result common-fields assignment and strict startup helper; other complete game-result and native boundaries not executed';nativeInvoked=$false}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'reader-receipt.json'),($receipt|ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding($false)))
Write-Output ('STARTUP READER PASS='+$startupPass+' FAIL='+$startupFail)
if($startupFail-ne0){exit 1};exit 0
