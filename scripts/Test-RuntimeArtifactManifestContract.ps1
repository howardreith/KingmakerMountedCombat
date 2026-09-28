$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
$module=Join-Path $PSScriptRoot 'runtime/RuntimeArtifactManifestEvidence.ps1'
. $module
$expected=@{'Assert-KmcManifestJsonMembersUnique'='70a5f9b31c425c82e8c528256fbd7d10a898cb07856f8aeeca727638ca616142';'Test-KmcManifestJsonInteger'='3b5b6bd7fc63440d2e9ac1bc7fa922c5df3d21a064beeb2d748570692a60ab02';'Assert-KmcReadOnlyArtifactManifest'='580d67f5ce0ceff5d3fdc3f21fb0501495bfc9f78c03b0a928afb1bc6972979b'}
$names=[ordered]@{'Assert-KmcManifestJsonMembersUnique'='Assert-NoDuplicateJsonObjectProperties';'Test-KmcManifestJsonInteger'='Test-ExactJsonInteger';'Assert-KmcReadOnlyArtifactManifest'='Assert-RuntimeArtifactManifest'}
$errors=$null;$tokens=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($module,[ref]$tokens,[ref]$errors);if($errors.Count-ne0){throw 'Shared module syntax invalid'}
$checks=0
foreach($name in $names.Keys){
 $f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($f.Count-ne1){throw 'Shared helper absent or duplicated'}
 $text=$f[0].Extent.Text.Replace("`r`n","`n")
 foreach($key in $names.Keys){$text=$text.Replace($key,$names[$key])}
 $text=$text.Replace('[Parameter(Mandatory = $true)]$ExpectedSha256,'+"`n"+'        [switch]$GameResult','[Parameter(Mandatory = $true)]$ExpectedSha256')
 $text=$text.Replace("if(`$GameResult){throw 'Runtime game-result evidenceManifestSha256 is not an exact lowercase SHA-256.'}; ",'').Replace("if(`$GameResult){throw 'Runtime artifact manifest hash does not match the runtime game result.'}; ",'')
 $sha=[Security.Cryptography.SHA256]::Create();try{$actual=([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text)))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
 if($actual-cne$expected[$name]){throw ('Shared guard implementation changed: '+$name+' '+$actual)};$checks++
}
# Exact historical bytes only. This validates the read-only guard, never current qualification.
$root=[IO.Path]::GetFullPath((Join-Path $repo '../../runtime-evidence/final105-p08-mammoth-tb'));$request=Get-Content -Raw (Join-Path $root 'runtime-request.json')|ConvertFrom-Json
$hash=(Get-FileHash (Join-Path $root 'runtime-artifacts.json')).Hash.ToLowerInvariant()
foreach($game in @($false,$true)){
 Assert-KmcReadOnlyArtifactManifest $request $hash -GameResult:$game;$checks++
 foreach($bad in @('malformed',('0'*64))){
  $errorText=$null;try{Assert-KmcReadOnlyArtifactManifest $request $bad -GameResult:$game}catch{$errorText=$_.Exception.Message}
  $expectedError=if($bad-ceq'malformed'){if($game){'Runtime game-result evidenceManifestSha256 is not an exact lowercase SHA-256.'}else{'Runtime result evidenceManifestSha256 is not an exact lowercase SHA-256.'}}else{if($game){'Runtime artifact manifest hash does not match the runtime game result.'}else{'Runtime artifact manifest hash does not match the runtime result.'}}
  if($errorText-cne$expectedError){throw ('Exact legacy refusal text differs: '+$errorText)};$checks++
 }
}
foreach($leaf in @('Test-RuntimeResult.ps1','Test-RuntimeGameResult.ps1')){
 $p=Join-Path $PSScriptRoot ('runtime/'+$leaf);$errors=$null;$tokens=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($p,[ref]$tokens,[ref]$errors)
 if($errors.Count-ne0){throw 'Result gate parse failed'};$text=Get-Content -Raw $p
 foreach($old in $names.Values){if($text.Contains($old)){throw ('Retired duplicate helper remains '+$old)}}
 $call=if($leaf-ceq'Test-RuntimeResult.ps1'){'Assert-KmcReadOnlyArtifactManifest $request $result.evidenceManifestSha256'}else{'Assert-KmcReadOnlyArtifactManifest $request $game.evidenceManifestSha256 -GameResult'}
 if(-not$text.Contains($call)){throw 'Result gate lost exact manifest invocation'};$checks++
}
'SHARED MANIFEST INTEGRATION PASS='+$checks+' FAIL=0; exact old guard bodies preserved, no native qualification'
