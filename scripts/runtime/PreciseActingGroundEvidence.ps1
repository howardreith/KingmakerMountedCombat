function Assert-KmcPreciseActingGroundInput {
    param($Setup)
    function Need-Precise([bool]$Value,[string]$Why){if(-not$Value){throw ('Precise native Acting setup: '+$Why)}}
    function Number-Precise($Value){
        Need-Precise ($null-ne$Value -and ($Value -is [double] -or $Value -is [float] -or $Value -is [decimal] -or $Value -is [int] -or $Value -is [long])) 'numeric evidence absent'
        $n=[double]$Value;Need-Precise (-not[double]::IsNaN($n)-and-not[double]::IsInfinity($n)) 'nonfinite evidence';return $n
    }
    Need-Precise ($null-ne$Setup -and $null-ne$Setup.PSObject.Properties['nativeGroundInput']) 'input contract absent'
    $i=$Setup.nativeGroundInput
    Need-Precise ($null-ne$i -and $i.contract -ceq 'native-ground-input-with-one-precise-setup-command') 'input contract absent'
    Need-Precise ($i.methodToken-ceq'060093DB'-and$i.directionToken-ceq'060093D9'-and$i.constructorToken-ceq'060026FF') 'pinned native input differs'
    Need-Precise (($i.callbackCount-is[int]-or$i.callbackCount-is[long])-and$i.callbackCount-eq1) 'callback count differs'
    $rider=[string]$Setup.before.currentTurnActor
    Need-Precise (-not[string]::IsNullOrEmpty($rider)-and$i.actorId-ceq$rider) 'callback actor differs'
    Need-Precise (@($i.selectedIds).Count-eq1-and$i.selectedIds[0]-ceq$rider) 'exact selected rider absent'
    Need-Precise (($i.commandObject-is[int]-or$i.commandObject-is[long])-and$i.commandObject-ne0-and$i.commandObject-eq$Setup.admittedCommand.id-and$i.commandObject-eq$Setup.terminalCommand.id) 'command identity differs'
    Need-Precise ($i.createdByPlayer-is[bool]-and$i.createdByPlayer) 'player command absent'
    Need-Precise (($i.frame-is[int]-or$i.frame-is[long])-and$i.frame-eq$Setup.before.frame) 'input frame differs'
    foreach($field in @('approachRadius','terminalApproachRadius','agentApproachRadius')){
        Need-Precise ([Math]::Abs((Number-Precise $i.$field)-0.03)-le0.000001) ('arrival radius differs: '+$field)
    }
    Need-Precise ([Math]::Abs((Number-Precise $Setup.placementTolerance)-0.06)-le0.000001) 'arrival tolerance changed'
    foreach($axis in @('x','y','z')){
        Need-Precise ([Math]::Abs((Number-Precise $i.targetPoint.$axis)-(Number-Precise $Setup.destination.$axis))-le0.000001) ('native command destination differs: '+$axis)
    }
}
