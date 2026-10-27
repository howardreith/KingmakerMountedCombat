[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepositoryRoot,[Parameter(Mandatory=$true)][string]$SourceOverlayRoot,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$out=[IO.Path]::GetFullPath($OutputDirectory);if(Test-Path -LiteralPath $out){throw 'Fresh wiring output required.'};[void][IO.Directory]::CreateDirectory($out)
$inputs=[Collections.Generic.List[object]]::new()
function Source([string]$path){$p=Join-Path $SourceOverlayRoot $path;if(-not(Test-Path -LiteralPath $p -PathType Leaf)){$p=Join-Path $RepositoryRoot $path};$inputs.Add([ordered]@{path=$p;sha256=(Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant()});$p}
function Ast([string]$path){$t=$null;$e=$null;$a=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$t,[ref]$e);if($e.Count){throw 'Source parse failed.'};$a}
function FunctionText($ast,[string]$name){$n=@($ast.FindAll({param($a)$a-is[Management.Automation.Language.FunctionDefinitionAst]-and$a.Name-ceq$name},$true));if($n.Count-ne1){throw 'Exact function absent.'};$n[0].Extent.Text}
function Need($value){if(-not$value){throw 'Wiring assertion failed.'}}
$pass=0;$fail=0;$rows=[Collections.Generic.List[string]]::new()
function Check([string]$name,[scriptblock]$body){try{&$body;$script:pass++;$script:rows.Add('PASS '+$name)}catch{$script:fail++;$script:rows.Add('FAIL '+$name+': '+$_.Exception.Message)}}
function Refuse([scriptblock]$body){$caught=$false;try{&$body|Out-Null}catch{$caught=$true};if(-not$caught){throw 'Expected exact admission refusal.'}}
$runner=Source 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1';$ra=Ast $runner
$ifs=@($ra.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Extent.Text.StartsWith("if(`$PSBoundParameters.ContainsKey('ObserveWorkingDeserialization')",[StringComparison]::Ordinal)},$true));if($ifs.Count-ne1){throw 'Runner opt-in guard not unique.'}
$emits=@($ra.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Extent.Text.StartsWith('if($ObserveWorkingDeserialization){$request[',[StringComparison]::Ordinal)},$true));if($emits.Count-ne1){throw 'Runner emission not unique.'}
$guard=[ScriptBlock]::Create('param([switch]$ObserveWorkingDeserialization,[string]$Scenario,[switch]$BaseGameOnly,[switch]$SaveAccessAllowed)'+"`n"+$ifs[0].Extent.Text+"`n"+'$request=@{};'+$emits[0].Extent.Text+"`n"+'return $request')
Check 'legacy unselected runner emits no new token' {Need ((&$guard -Scenario 'export-mounted-contracts').Count-eq0)}
Check 'exact selected runner emits bool true only' {$r=&$guard -ObserveWorkingDeserialization -Scenario 'export-mounted-contracts' -BaseGameOnly -SaveAccessAllowed;Need ($r.Count-eq1-and$r.observeWorkingDeserialization-is[bool]-and$r.observeWorkingDeserialization)}
Check 'explicit false runner refuses' {Refuse {&$guard -ObserveWorkingDeserialization:$false -Scenario 'export-mounted-contracts' -BaseGameOnly -SaveAccessAllowed}}
Check 'installed-mod mode refuses observation' {Refuse {&$guard -ObserveWorkingDeserialization -Scenario 'export-mounted-contracts' -SaveAccessAllowed}}
Check 'unqualified save access refuses observation' {Refuse {&$guard -ObserveWorkingDeserialization -Scenario 'export-mounted-contracts' -BaseGameOnly}}
Check 'foreign scenario refuses observation' {Refuse {&$guard -ObserveWorkingDeserialization -Scenario 'chunk7-native-main-bootstrap' -BaseGameOnly -SaveAccessAllowed}}
$requestFile=Source 'scripts/runtime/Test-RuntimeRequest.ps1';$qa=Ast $requestFile
. ([ScriptBlock]::Create((FunctionText $qa 'Test-JsonInteger')))
$qifs=@($qa.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Extent.Text.StartsWith('if($hasWorkingObservation -and',[StringComparison]::Ordinal)},$true));if($qifs.Count-ne1){throw 'Request opt-in token guard not unique.'}
$qguard=[ScriptBlock]::Create('param($request)'+"`n"+'$hasWorkingObservation=@($request.PSObject.Properties.Name)-ccontains ''observeWorkingDeserialization'';'+$qifs[0].Extent.Text)
foreach($v in @($null,$false,1,'true',@($true),[pscustomobject]@{})){$bad=$v;Check ('wrong request opt-in '+$(if($null-eq$v){'null'}else{$v.GetType().Name})) {Refuse {&$qguard ([pscustomobject]@{schemaVersion=2;scenario='export-mounted-contracts';observeWorkingDeserialization=$bad})}}}
Check 'request opt-in true accepts' {&$qguard ([pscustomobject]@{schemaVersion=2;scenario='export-mounted-contracts';observeWorkingDeserialization=$true})}
Check 'request old omitted accepts' {&$qguard ([pscustomobject]@{schemaVersion=2;scenario='export-mounted-contracts'})}
Check 'request scenario array refuses' {Refuse {&$qguard ([pscustomobject]@{schemaVersion=2;scenario=@('export-mounted-contracts');observeWorkingDeserialization=$true})}}
Check 'request schema string refuses' {Refuse {&$qguard ([pscustomobject]@{schemaVersion='2';scenario='export-mounted-contracts';observeWorkingDeserialization=$true})}}
$hostPath=Source 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs';$hostText=[IO.File]::ReadAllText($hostPath)
Check 'normal and bootstrap-failure readers share raw token gate' {Need (([regex]::Matches($hostText,'AssertWorkingObservationToken\(')).Count-eq2)}
Check 'retained observer acquired in original loader Start route' {Need ($hostText.Contains('? (Action<Kingmaker.Game, Kingmaker.EntitySystem.Persistence.SaveManager, Kingmaker.EntitySystem.Persistence.SaveInfo>)BeginWorkingLoadObservation : null)'))}
Check 'selected artifact publication precedes final manifest' {$a=$hostText.IndexOf('PublishWorkingObservation(status);');$b=$hostText.IndexOf('evidenceManifestSha256 = PublishRuntimeArtifactManifest();',$a);$c=$hostText.IndexOf('var resultPayload = new RuntimeGameResultV2',$a);Need ($a-ge0-and$b-gt$a-and$c-gt$b)}
Check 'preexisting manifest order unchanged when absent' {Need ($hostText.Contains('var evidenceManifestSha256 = workingLoadObservation == null ? PublishRuntimeArtifactManifest() : null;'))}
Check 'shared publisher records both new leaves exactly once' {foreach($kind in @('Evidence','Pending')){Need (([regex]::Matches($hostText,[regex]::Escape('AddRuntimeArtifactIfPresent(artifacts, request.EvidenceRoot, WorkingLoadDiagnosticSession.'+$kind+'FileName, WorkingLoadDiagnosticSession.'+$kind+'Kind);'))).Count-eq1)}}
Check 'shared publisher remains used by bootstrap failure' {$a=$hostText.IndexOf('private static RuntimeGameResultV2 CreateBootstrapFailureV2(');$b=$hostText.IndexOf('
        private ', $a+1);Need ($a-ge0-and$b-gt$a);$m=$hostText.Substring($a,$b-$a);Need ($m.Contains('EvidenceManifestSha256 = PublishRuntimeArtifactManifest(request),'))}
$commonPath=Source 'scripts/runtime/RuntimeHarness.Common.ps1';$ca=Ast $commonPath
. ([ScriptBlock]::Create((FunctionText $ca 'Assert-KmcKnownRuntimeArtifactsManifested')))
$knownRoot=Join-Path $out 'known-json';[void][IO.Directory]::CreateDirectory($knownRoot)
foreach($leaf in @('working-load-deserialization-evidence.json','working-load-deserialization-pending.json')){[IO.File]::WriteAllText((Join-Path $knownRoot $leaf),'{}')}
$manifest=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='working-load-deserialization-evidence.json'},[pscustomobject]@{relativePath='working-load-deserialization-pending.json'})}
Check 'both known generated artifacts manifested' {Assert-KmcKnownRuntimeArtifactsManifested $knownRoot $manifest}
foreach($leaf in @('working-load-deserialization-evidence.json','working-load-deserialization-pending.json')){$missing=$leaf;Check ('missing manifest record refuses '+$leaf){Refuse {Assert-KmcKnownRuntimeArtifactsManifested $knownRoot ([pscustomobject]@{artifacts=@($manifest.artifacts|Where-Object{$_.relativePath-cne$missing})})}}}
foreach($relative in @('scripts/runtime/RuntimeArtifactManifestEvidence.ps1','scripts/runtime/RuntimeHarness.Common.ps1')){$p=Source $relative;$a=Ast $p;$var=if($relative.EndsWith('Common.ps1')){'$relative'}else{'$relativePath'};$nodes=@($a.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq'$allowed'-and$n.Extent.Text.Contains('working-load-deserialization-evidence')},$true));if($nodes.Count-ne1){throw 'Allowlist assignment not unique.'};$allowScript=[ScriptBlock]::Create('param($leaf,$kind)'+"`n"+$var+'=$leaf;'+$nodes[0].Extent.Text+"`n"+'return $allowed');foreach($base in @('working-load-deserialization-evidence','working-load-deserialization-pending')){$b=$base;Check ('exact allowlist '+$relative+' '+$b){Need (&$allowScript ($b+'.json') $b)};Check ('mismatched allowlist '+$relative+' '+$b){Need (-not(&$allowScript ($b+'.json') 'foreign-kind'))}}}
$project=Source 'src/KingmakerMountedCombat/KingmakerMountedCombat.csproj';[xml]$px=Get-Content -LiteralPath $project -Raw
foreach($leaf in @('WorkingLoadObservation','NativeWorkingLoadContract','NativeWorkingLoadRewrite','NativeWorkingLoadObserver','WorkingLoadDiagnosticSession','RuntimeAutomationHost.WorkingLoadObservation')){Check ('exact product Compile '+$leaf){Need (@($px.SelectNodes('//*[local-name()="Compile"]')|Where-Object{$_.Include-ceq('Diagnostics\'+$leaf+'.cs')}).Count-eq1)}}
$same=$true;foreach($pin in $inputs){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$same=$false}}
$rows.Add("WORKING LOAD WIRING PASS=$pass FAIL=$fail");[IO.File]::WriteAllLines((Join-Path $out 'checks.log'),$rows)
[ordered]@{schemaVersion=1;status=$(if($fail-eq0-and$same){'PASS'}else{'FAIL'});passed=$pass;failed=$fail;inputPins=@($inputs.ToArray());inputsUnchanged=$same;boundaries='Only exact AST guard/emission/allowlist functions execute; known-artifact paths are generated JSON. Other connections checked by exact source ordering. No runner, native, archives or full request qualification.'}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $out 'receipt.json') -Encoding UTF8
$rows;if($fail-ne0-or-not$same){throw 'Working observer wiring failed.'}
