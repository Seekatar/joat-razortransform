
<#
.Synopsis
    Generate an RtValues file based on RtObject
    file to allow editing of it with Artie

.Parameter rtObjectFile
    Input file that is usually RtObject.  Default value is RtThySelfValues.xml

.Parameter outputFile
    Output filename.  Default value is RtThySelfValues.xml
#>
[CmdletBinding()]
param(
[Parameter(Mandatory=$true,Position=0)]
[ValidateScript({Test-Path $_ -PathType Leaf})]
[string] $rtObjectFile="RtObject.xml",
[Parameter(Mandatory=$true,Position=1)]
[string] $outputFile="RtThySelfValues.xml"
)

Set-StrictMode -Version Latest

if ( -not $(Test-Path $rtObjectFile) )
{
    Write-Error "$rtObjectFile not found"
    exit 1 
}

# helper function to add all attributes of an object as values in rtValues
function addAllAttributes( $root, $item, $parent )
{
    # add all the attributes of the group to it
    foreach ( $a in $item.Attributes )
    {
        $attr = $root.CreateElement("value")
        $attr.SetAttribute("name", $a.Name )
        $attr.SetAttribute("value", $a.Value )
        $null = $parent.AppendChild($attr)
    }
}


# helper function to add all the groups in the Xml, and attributes, and subgroups
function addGroups($root, $parent, $groups )
{
    if ( $groups -eq $null )
    {
        return
    }

    foreach ( $g in $groups )
    {
        $group = $root.CreateElement("value")
        $null = $parent.AppendChild( $group )

        $group.SetAttribute("name", "group" )

        addAllAttributes $root $g $group 

        # now get the items
        foreach ( $i in $g.SelectNodes( "./item" ))
        {
            $item = $root.CreateElement("value")
            $item.SetAttribute("name","item")
            $null = $group.AppendChild($item)
        
            addAllAttributes $root $i $item
        }

        addGroups $root $group $g.SelectNodes( "./group" ) 
    }
}


###################################################################
# MAIN
###################################################################
$rtobject = [xml](Get-Content $rtObjectFile)
Write-Host "Loaded $rtObjectFile"

$groups = $rtobject.SelectNodes("/RtObject/group")
$enums = $rtobject.SelectNodes("/RtObject/enum")
$regexes = $rtobject.SelectNodes("/RtObject/regex")
$customs = $rtobject.SelectNodes("/RtObject/custom")

Write-Verbose ("Found {0} groups {1} enums and {2} customs" -f ($groups.Count, $enums.Count, $customs.Count))


$root = [xml]"<RtValues/>"

# do groups 
addGroups  $root $root.FirstChild $groups

# do enums
foreach ( $e in $enums )
{
    $enum = $root.CreateElement("value")
    $enum.SetAttribute("name","enumeration")

    addAllAttributes $root $e $enum

    $null = $root.FirstChild.AppendChild($enum)
    
    $name = $root.CreateElement("value")
    $name.SetAttribute("name", $e.name )
    $null = $enum.AppendChild($name)

    foreach ( $v in $e.SelectNodes("./value") )
    {
        $value = $root.CreateElement("value")
        $value.SetAttribute("name",$v.name)
        $null = $enum.AppendChild($value)

        addAllAttributes $root $v $value
    }
}

# do regexes
foreach ( $r in $regexes )
{
    $regex = $root.CreateElement("value")
    $regex.SetAttribute("name","regex")

    addAllAttributes $root $e $regex

    $null = $root.FirstChild.AppendChild($regex)
    
    $name = $root.CreateElement("value")
    $name.SetAttribute("name", $e.name )
    $null = $regex.AppendChild($name)

    foreach ( $v in $e.SelectNodes("./value") )
    {
        $value = $root.CreateElement("value")
        $value.SetAttribute("name",$v.name)
        $null = $regex.AppendChild($value)

        addAllAttributes $root $v $value
    }
}

# do customs
foreach ( $c in $customs )
{
    $custom = $root.CreateElement("value")
    $custom.SetAttribute("name","custom")

    addAllAttributes $root $c $custom

    $null = $root.FirstChild.AppendChild($custom)
}     

$root.Save($(Join-Path $PWD $outputFile))
Write-Host "Saved file to $(Join-Path $PWD $outputFile)"
