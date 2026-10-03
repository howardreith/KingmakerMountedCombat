# External acceptance authority for the structured parent-to-child handoff snapshot persisted in
# every tranche artifact from preview.150. The compiled producer checks only the snapshot's
# structure (ChildEntryPreambleEvidence.AssertStructure); which handoffs are lawful for which
# child is decided here. Read-only.
Set-StrictMode -Version Latest
function Assert-KmcChildEntryPreamble($P,[string]$ChildScenario,[bool]$PairAlreadyMounted,[bool]$RequiresIdleParty){
 function Fail($why){throw ('Child entry preamble: '+$why)}
 function I($v){if($v-isnot[int]-and$v-isnot[long]){Fail 'integer missing'};[long]$v}
 function B($v){if($v-isnot[bool]){Fail 'boolean missing'};[bool]$v}
 function N($v){if($v-isnot[int]-and$v-isnot[long]-and$v-isnot[double]-and$v-isnot[single]-and$v-isnot[decimal]){Fail 'number missing'};$d=[double]$v;if([double]::IsNaN($d)-or[double]::IsInfinity($d)){Fail 'nonfinite number'};$d}
 function T($v){if($v-is[string]){$v}else{$null}}
 function Has($o,$n){$null-ne$o-and$null-ne$o.PSObject.Properties[$n]}
 function Prop($o,$n){if(Has $o $n){$o.$n}else{$null}}
 if($null-eq$P-or(T (Prop $P 'contract'))-cne'structured-child-entry-preamble-snapshot'){Fail 'contract differs'}
 if((T (Prop $P 'parentScenario'))-cne'horse-companion-unmounted-suite'-or[string]::IsNullOrEmpty((T (Prop $P 'parentEngine')))){Fail 'parent identity missing'}
 if([string]::IsNullOrEmpty($ChildScenario)-or(T (Prop $P 'childScenario'))-cne$ChildScenario){Fail 'child scenario differs'}
 if([string]::IsNullOrEmpty((T (Prop $P 'runId')))-or(I (Prop $P 'sessionObject'))-eq0-or[string]::IsNullOrEmpty((T (Prop $P 'areaGuid')))){Fail 'session binding missing'}
 if((I (Prop $P 'frame'))-lt0-or(I (Prop $P 'gameTicks'))-lt0-or[string]::IsNullOrEmpty((T (Prop $P 'capturedAtUtc')))){Fail 'clock missing'}
 $rider=T (Prop $P 'riderId');$mount=T (Prop $P 'mountId')
 if([string]::IsNullOrEmpty($rider)-or[string]::IsNullOrEmpty($mount)-or$rider-ceq$mount-or(I (Prop $P 'riderObject'))-eq0-or(I (Prop $P 'mountObject'))-eq0-or(I (Prop $P 'riderObject'))-eq(I (Prop $P 'mountObject'))){Fail 'pair identity missing'}
 if((B (Prop $P 'pairAlreadyMounted'))-ne$PairAlreadyMounted){Fail 'handoff mounted flag differs'}
 $expectedState=if($PairAlreadyMounted){'Mounted'}else{'Unmounted'}
 if((T (Prop $P 'relationshipState'))-cne$expectedState-or(I (Prop $P 'relationshipGeneration'))-lt0){Fail 'relationship state contradicts the handoff'}
 $admission=T (Prop $P 'admissionMode');if([string]::IsNullOrEmpty($admission)){Fail 'admission mode missing'}
 # The recorded admission mode is validated as observed: a mounted handoff may follow the
 # parent's exploration Mount or its lawful voluntary combat Mount (a mounted child never has
 # to repeat an exploration Mount); an unmounted handoff never follows a voluntary combat
 # dispatch; a saved-game restoration is not a handoff path.
 if($PairAlreadyMounted){if($admission-cnotin@('Exploration','VoluntaryCombat')){Fail ('mounted handoff was not an exploration or voluntary combat Mount: '+$admission)}}
 elseif($admission-ceq'VoluntaryCombat'){Fail 'unmounted handoff follows a voluntary combat dispatch'}
 if((B (Prop $P 'admissionModeExplicit'))-ne($admission-cne'<none>')){Fail 'admission explicitness differs'}
 $commands=Prop $P 'commands'
 if(-not(B (Prop $commands 'riderCommandsEmpty'))-or-not(B (Prop $commands 'mountCommandsEmpty'))-or(I (Prop $commands 'riderRelationshipCommands'))-ne0-or(I (Prop $commands 'mountRelationshipCommands'))-ne0){Fail 'a Mount/Dismount command is in flight at child entry'}
 $control=Prop $P 'control'
 if((B (Prop $control 'transitionInFlight'))-or(B (Prop $control 'riderOwnsUnsettledShell'))-or(I (Prop $control 'shellCount'))-lt0-or(I (Prop $control 'processBindings'))-lt0-or(I (Prop $control 'dispatchAccepted'))-lt0-or(I (Prop $control 'dispatchRejected'))-lt0-or[string]::IsNullOrEmpty((T (Prop $control 'transitionLedger')))){Fail 'a relationship shell, process or dispatch is unsettled at child entry'}
 $actors=Prop $P 'actors'
 foreach($role in @('rider','mount')){
  $actor=Prop $actors $role;$expectedId=if($role-ceq'rider'){$rider}else{$mount}
  if($null-eq$actor-or(T (Prop $actor 'id'))-cne$expectedId){Fail ($role+' resources missing')}
  foreach($field in @('standard','move','swift','initiative','reactionCooldown')){if((N (Prop $actor $field))-lt0){Fail ($role+' '+$field+' invalid')}}
  foreach($field in @('prepared','inCombat','canAct','hasMove','hasStandard')){if((Prop $actor $field)-isnot[bool]){Fail ($role+' preparation state missing')}}
  if((I (Prop $actor 'reactions'))-lt0){Fail ($role+' preparation state missing')}
  if(B (Prop $actor 'commandRunning')){Fail ($role+' has a running native command at child entry')}
 }
 $party=Prop $P 'party'
 if((Prop $party 'playerInCombat')-isnot[bool]-or(I (Prop $party 'membersInCombat'))-lt0-or(I (Prop $party 'members'))-lt2){Fail 'party state missing'}
 if($RequiresIdleParty-and((B $party.playerInCombat)-or(I $party.membersInCombat)-ne0-or-not(B (Prop $party 'idle')))){Fail 'the disposable party was not idle at child entry'}
 if((Prop $P 'turnBased')-isnot[bool]-or(Prop $P 'paused')-isnot[bool]){Fail 'mode state missing'}
}
# The tranche's own handoff rule: Chunk 6A combat-mount children start unmounted; every
# other tranche child requires the mounted preamble.
function Test-KmcChildEntryExpectsMounted([string]$Scenario){ -not ($Scenario -clike 'chunk6a-*') }
function Test-KmcChildEntryRequiresIdleParty([string]$Scenario){
 $Scenario -clike 'chunk4-*' -or $Scenario -clike 'actor-allocation-*' -or $Scenario -clike 'chunk6a-*' -or $Scenario -clike 'chunk6b-*' -or $Scenario -ceq 'unmounted-attack-controls-rt'
}
# Required of every Chunk 6A candidate from preview.150 and of every Chunk 6B candidate (the compiled
# producer stamps that version). Earlier candidates, the historical product lines and parser-only synthetic versions
# predate the snapshot; a snapshot that is present is always validated.
function Test-KmcChildEntryPreambleRequired([string]$ProductVersion){
 ([string]$ProductVersion -cmatch '^0[.]1[.]0-chunk6[ab]-preview[.]([0-9]+)$' -and [long]$Matches[1] -ge 150)
}
