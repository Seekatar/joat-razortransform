param($extra)
$x = [ordered]@{'<None>'='<None>'}
$x["extra"] = $extra
foreach( $i in gci -file)
{
	$x[(Split-Path $i -Leaf)] = $i
}
$x 
