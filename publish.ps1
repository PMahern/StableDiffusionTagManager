$versionsuffix = "beta8"
$projectpath = "src/StableDiffusionTagManager/StableDiffusionTagManager.csproj"
$platforms = "win10-x64", "linux-x64", "osx-x64"

function publish($platform) {
	dotnet publish $projectpath -c Release -r $platform /p:PublishSingleFile=true --version-suffix $versionsuffix  -o "publish\$($platform)"
}

foreach($platform in $platforms) {
	publish($platform)
}


$osxzip = "publish\sdtm-osx-x64-1.0.0-$($versionsuffix).zip"
$windowszip = "publish\sdtm-win10-x64-1.0.0-$($versionsuffix).zip"
$linuxtemptar = "publish\temp.tar"
$linuxtargz = "publish\sdtm-linux-x64-1.0.0-$($versionsuffix).tgz"

if(Test-Path $osxzip) {
	Remove-Item $osxzip
}
if(Test-Path $windowszip) {
	Remove-Item $windowszip
}
if(Test-Path $linuxtemptar) {
	Remove-Item $linuxtemptar
}
if(Test-Path $linuxtargz) {
	Remove-Item $linuxtargz
}

7z a -tzip $osxzip "publish\osx-x64"
7z a -tzip $windowszip "publish\win10-x64"
7z a $linuxtemptar "publish\linux-x64"
7z a $linuxtargz $linuxtemptar
Remove-Item $linuxtemptar