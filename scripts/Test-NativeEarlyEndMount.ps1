param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'));$kmcTestRoot=Join-Path $lab ('analysis-cache/early-end-tests/'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($kmcTestRoot)|Out-Null
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$draftAssembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$factory=[IO.File]::ReadAllText((Join-Path $repo 'scripts/Test-Chunk6aOrderEnvelope.ps1'))
$cut=$factory.IndexOf('$paths=[xml]');if($cut-lt0){throw 'Factory boundary absent'}
$factory=$factory.Substring(0,$cut).Replace('$PSScriptRoot',"'$repo/scripts'")
. ([scriptblock]::Create($factory)) -Configuration Release
function New-EarlyEndFixture([bool]$First){
 # Build a synthetic positive window with unused Swift before making the continuation.
 $body=(Get-Command New-OrderEnvelope).ScriptBlock.ToString()
 $anchor='$proof=New-FullTbProof $First;'
 $replacement='$proof=New-FullTbProof $First;foreach($b in @($proof.preClick.state)+@($proof.samples|ForEach-Object{$_.state})){$b.rider.swift=0.0};foreach($event in $proof.resourceWindow.events){if($event.state.actor-ceq''rider''){$event.state.swift=0.0}};'
 if(-not$body.Contains($anchor)){throw 'Order synthetic proof anchor absent'}
 . ([scriptblock]::Create('function New-EarlyOrderEnvelope {'+$body.Replace($anchor,$replacement)+'}'))
 $a=New-EarlyOrderEnvelope $First;$e=$a.observations.chunk6aMountOrder
 $r=@($e.completion.calls|Where-Object {$_.kind-ceq'force-to-end'-and$_.before.actorId-ceq'rider'})[0]
 # This fixed case relinquishes native Swift; Standard may already be unavailable.
 Put-Value $e earlyEndAllowance ([pscustomobject]@{action='Swift';method='Kingmaker.EntitySystem.Entities.UnitEntityData.HasSwiftAction';token='06008380';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';ilSha256='65061d8c8cccff181626338782db42cc3504357af1bc2c3121fb9dc4a49a09a8'})
 function Set-SyntheticAvailability($x){
  if($null-eq$x-or$x-is[string]-or$x-is[ValueType]){return}
  if($x-is[System.Array]){foreach($v in $x){Set-SyntheticAvailability $v};return}
  if($null-ne$x.PSObject.Properties['actor']-and$null-ne$x.PSObject.Properties['swift']){Put-Value $x hasSwift ([double]$x.swift-le0)}
  foreach($prop in @($x.PSObject.Properties)){Set-SyntheticAvailability $prop.Value}
 }
 Set-SyntheticAvailability $e
 $e.afterEndInput.frame=$r.after.frame;$e.afterEndInput.gameTicks=$r.after.gameTicks;$e.afterEndInput.allocationSequence=$r.after.allocationSequence;$e.afterEndInput.status='Ending';$e.afterEndInput.riderResources=Copy-Value $r.after.resources
 if($First){$m=@($e.completion.calls|Where-Object {$_.kind-ceq'force-to-end'-and$_.before.actorId-ceq'mount'})[0];$e.afterEndInput.mountResources=Copy-Value $m.after.resources}
 $e.continuation.before.rider=Copy-Value $e.mounted.riderResources
 $e.continuation.completion=Copy-Value $e.completion
 # These immutable synthetic copies must agree with the new native observation fields.
 $e.terminalBridge.after.rider=Copy-Value $e.mounted.riderResources
 $e.terminalBridge.before.rider=Copy-Value $e.mounted.riderResources
 $e.positiveProof.samples[-1].nativeAllocation.rider=Copy-Value $e.mounted.riderResources
 $e
}
$method=$draftAssembly.GetType('KingmakerMountedCombat.Diagnostics.NativeEarlyEndMountEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
foreach($first in @($true,$false)){
 $e=New-EarlyEndFixture $first
 $params=[object[]]::new(1);$params[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 100 -Compress))
 try{$null=$method.Invoke($null,$params);Write-Output ('Producer baseline '+$first+' PASS')}catch{Write-Output $_.Exception.InnerException.ToString();throw}
 [IO.File]::WriteAllText((Join-Path $kmcTestRoot ('fixture-'+$first+'.json')),($e|ConvertTo-Json -Depth 100))
}. (Join-Path $PSScriptRoot 'runtime/EarlyEndMountEvidence.ps1')
$script:earlyChecks=0
function Check-Early($e,[bool]$Expected){
 $producer=$true;try{$params=[object[]]::new(1);$params[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 100 -Compress));$null=$method.Invoke($null,$params)}catch{$producer=$false;if($Expected){throw}}
 $external=$true;try{Assert-KmcEarlyEndMount $e}catch{$external=$false;if($Expected){throw}}
 if($producer-ne$Expected-or$external-ne$Expected){throw ('Early End producer/external '+$producer+'/'+$external+' expected '+$Expected)};$script:earlyChecks+=2
}
foreach($first in @($true,$false)){
 $p=New-EarlyEndFixture $first;Check-Early $p $true
 function Reject-Early([scriptblock]$Change){$x=Copy-Value $p;& $Change $x;try{Check-Early $x $false}catch{throw ('Mutation '+$Change.ToString()+': '+$_)}}
 foreach($endpoint in @('beforeEndInput','afterEndInput')){
  foreach($field in @('frame','gameTicks','round','turnObject','pairedSequence')){Reject-Early {param($x)$x.$endpoint.$field++}}
  Reject-Early {param($x)$x.$endpoint.currentActor='mount'}
  Reject-Early {param($x)$x.$endpoint.status='Ended'}
  foreach($role in @('riderResources','mountResources')){
   foreach($field in @('actorObject','standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','grantSequence')){Reject-Early {param($x)$x.$endpoint.$role.$field++}}
  }
 }
 Reject-Early {param($x)$x.beforeEndInput.allocationSequence=$x.completion.calls[0].before.allocationSequence}
 Reject-Early {param($x)$x.afterEndInput.allocationSequence=$x.completion.calls[0].after.allocationSequence-1}
 foreach($bad in @($false,'true',1,$null)){
  Reject-Early {param($x)$x.beforeEndInput.riderResources.hasSwift=$bad}
  Reject-Early {param($x)$x.completion.calls[0].before.resources.hasSwift=$bad;$x.continuation.completion=Copy-Value $x.completion}
 }
 foreach($bad in @($true,'false',0,$null)){
  Reject-Early {param($x)$x.afterEndInput.riderResources.hasSwift=$bad}
  Reject-Early {param($x)$x.completion.calls[0].after.resources.hasSwift=$bad;$x.continuation.completion=Copy-Value $x.completion}
 }
 foreach($field in @('action','method','token','moduleMvid','ilSha256')){
  Reject-Early {param($x)$x.earlyEndAllowance.$field='wrong'}
  Reject-Early {param($x)$x.earlyEndAllowance.PSObject.Properties.Remove($field)}
 }
 Reject-Early {param($x)$x.earlyEndAllowance=$null}
 Reject-Early {param($x)$x.completion.calls[0].setCooldowns=$false;$x.continuation.completion=Copy-Value $x.completion}
 # Paired preparation and End are still validated by the unchanged complete order gate.
 Reject-Early {param($x)$x.nextRound.pairedSequence=3}
 Reject-Early {param($x)$x.nextRound.adoptionCount=2}
 Reject-Early {param($x)$x.nextRound.round=3}
}
Write-Output ('EARLY END COMPONENT PASS='+$script:earlyChecks+' FAIL=0; no native qualification')
[ordered]@{status='PASS';level='COMPONENT ONLY';checks=$script:earlyChecks;sourceCommit=(& git -C $repo rev-parse HEAD).Trim();draftDllSha256=(Get-FileHash -LiteralPath (Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll'))).Hash.ToLowerInvariant();candidateChanged=$false;nativeQualified=$false;completedAtUtc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $kmcTestRoot 'early-end-receipt.json') -Encoding UTF8
