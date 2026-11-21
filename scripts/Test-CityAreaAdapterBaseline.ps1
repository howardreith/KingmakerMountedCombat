[CmdletBinding()]
param([string]$BaseCommit='dbf63702')
# Matched base/candidate comparison of the area-restore actual-method adapter's compile step
# (scripts/chunk7/Test-Chunk7AreaRestorePreflightAdapter.ps1). That adapter's facade does not compile against the current
# product; this check classifies the failure. For each tree it performs the adapter's own compile step exactly: extract
# 'internal void RestoreAreaPair()' from MountedAreaPersistence.cs by brace matching, substitute it for __METHOD__ in
# tests/Chunk7Diagnostics/AreaRestorePreflightAdapterTemplate.cs, and compile with the same csc, flags and source list.
# Base inputs are read with 'git show <BaseCommit>:<path>'; candidate inputs with 'git show HEAD:<path>' (the committed
# candidate). Diagnostics are normalized (tree root and line/column removed) and compared as multisets.
# PASS: the candidate adds no compiler diagnostic the base does not have (a pre-existing failure stays documented).
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$adapterScript='scripts/chunk7/Test-Chunk7AreaRestorePreflightAdapter.ps1'
$inputs=@('src/KingmakerMountedCombat/Integration/MountedAreaPersistence.cs','tests/Chunk7Diagnostics/AreaRestorePreflightAdapterTemplate.cs',
    'src/KingmakerMountedCombat/Domain/MountedPairCandidate.cs','src/KingmakerMountedCombat/Domain/MountedRelationshipAdmission.cs',
    'src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs','src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs',
    'src/KingmakerMountedCombat/Domain/PoseVector3.cs','src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs')
function Invoke-RepoGit([string[]]$Arguments){$out=@(& git.exe -C $repo @Arguments 2>&1);if($LASTEXITCODE-ne0){throw ('git '+($Arguments-join' ')+' failed: '+($out-join' '))};,$out}
$baseFull=(Invoke-RepoGit @('rev-parse',$BaseCommit))[0].Trim();$headFull=(Invoke-RepoGit @('rev-parse','HEAD'))[0].Trim()
& git.exe -C $repo merge-base --is-ancestor $baseFull $headFull;if($LASTEXITCODE-ne0){throw 'The base is not an ancestor of the candidate.'}
# The adapter script's own compile step must be what this check reproduces: its source list and flags are pinned here.
$adapterText=[IO.File]::ReadAllText((Join-Path $repo $adapterScript))
foreach($needle in @("'internal void RestoreAreaPair()'","Replace('__METHOD__', `$method)","/nologo /target:exe /langversion:7.3 '/nowarn:0649,0414'",
    "'MountedPairCandidate.cs'","'MountedRelationshipAdmission.cs'","'MountedRiderProfileCatalog.cs'","'DeclaredSmallRiderBody.cs'","'PoseVector3.cs'","'MountedRiderPoseProfile.cs'")){
    if(-not$adapterText.Contains($needle)){throw "The adapter's compile step changed; this comparison no longer matches it: $needle"}
}
function Compile([string]$Commit,[string]$Label){
    $root=Join-Path ([IO.Path]::GetTempPath()) ('kmc-area-adapter-'+$Label+'-'+[Guid]::NewGuid().ToString('N'))
    $identities=@()
    foreach($path in $inputs){
        $blob=(Invoke-RepoGit @('rev-parse',($Commit+':'+$path)))[0].Trim()
        $text=(Invoke-RepoGit @('show',($Commit+':'+$path)))-join"`n"
        $target=Join-Path $root $path;[void][IO.Directory]::CreateDirectory((Split-Path $target -Parent))
        [IO.File]::WriteAllText($target,$text,[Text.UTF8Encoding]::new($false));$identities+=($path+' '+$blob)
    }
    $body=[IO.File]::ReadAllText((Join-Path $root $inputs[0]));$signature='internal void RestoreAreaPair()'
    $start=$body.IndexOf($signature,[StringComparison]::Ordinal)
    if($start-lt0-or$start-ne$body.LastIndexOf($signature,[StringComparison]::Ordinal)){throw "$Label RestoreAreaPair is absent or ambiguous."}
    $open=$body.IndexOf('{',$start);$depth=1;$end=$open+1
    while($depth-gt0-and$end-lt$body.Length){if($body[$end]-ceq'{'){$depth++}elseif($body[$end]-ceq'}'){$depth--};$end++}
    $actual=Join-Path $root 'ActualAreaRestorePreflight.cs'
    [IO.File]::WriteAllText($actual,[IO.File]::ReadAllText((Join-Path $root $inputs[1])).Replace('__METHOD__',$body.Substring($start,$end-$start)),[Text.UTF8Encoding]::new($false))
    $sources=@($actual)+@($inputs|Select-Object -Skip 2|ForEach-Object{Join-Path $root $_})
    $output=@(& $compiler /nologo /target:exe /langversion:7.3 '/nowarn:0649,0414' ('/out:'+(Join-Path $root 'adapter.exe')) $sources 2>&1);$exit=$LASTEXITCODE
    $escaped=[regex]::Escape($root)
    $diagnostics=@($output|ForEach-Object{[string]$_}|Where-Object{$_-match'error CS\d+'}|ForEach-Object{($_-replace$escaped,'<tree>')-replace'\(\d+,\d+\)',''}|Sort-Object)
    [pscustomobject]@{Label=$Label;Commit=(Invoke-RepoGit @('rev-parse',$Commit))[0].Trim();Exit=$exit;Diagnostics=$diagnostics;Identities=$identities}
}
$base=Compile $baseFull 'base';$candidate=Compile $headFull 'candidate'
foreach($run in @($base,$candidate)){
    Write-Output ("AREA ADAPTER "+$run.Label+" commit="+$run.Commit+" cscExit="+$run.Exit+" errors="+$run.Diagnostics.Count)
    $run.Identities|ForEach-Object{Write-Output ('  input '+$_)}
}
function Subtract([string[]]$Left,[string[]]$Right){
    $counts=New-Object 'System.Collections.Generic.Dictionary[string,int]' ([StringComparer]::Ordinal)
    foreach($item in @($Right)){if($null-eq$item){continue};if($counts.ContainsKey($item)){$counts[$item]++}else{$counts[$item]=1}}
    $rest=New-Object 'System.Collections.Generic.List[string]'
    foreach($item in @($Left)){if($null-eq$item){continue};if($counts.ContainsKey($item)-and$counts[$item]-gt0){$counts[$item]--}else{$rest.Add($item)}}
    $rest.ToArray()
}
$added=@(Subtract $candidate.Diagnostics $base.Diagnostics)
$removed=@(Subtract $base.Diagnostics $candidate.Diagnostics)
$added|ForEach-Object{Write-Output ('  ONLY IN CANDIDATE '+$_)}
$removed|ForEach-Object{Write-Output ('  ONLY IN BASE '+$_)}
$distinct=@($base.Diagnostics|ForEach-Object{([regex]::Match($_,'error (CS\d+): (.*)$')).Value}|Sort-Object -Unique)
$distinct|Select-Object -First 12|ForEach-Object{Write-Output ('  base diagnostic '+$_)}
$ok=$added.Count-eq0
Write-Output ('AREA ADAPTER BASELINE '+$(if($ok){'PASS'}else{'FAIL'})+': base errors='+$base.Diagnostics.Count+', candidate errors='+$candidate.Diagnostics.Count+
    ', only in candidate='+$added.Count+', only in base='+$removed.Count+'; compile step only, the adapter executes in neither tree')
if(-not$ok){exit 1}
