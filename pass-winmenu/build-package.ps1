param(
	[switch]$full,
	[switch]$package
)

$PKGDIR="bin/Release-Packaged"
$ZIPDIR="bin/pass-winmenu.zip"
$INCLUDEDIR="$PKGDIR/include"

if($full){
	rm -recurse "$PKGDIR"
	mkdir "$PKGDIR"
	mkdir "$INCLUDEDIR"
}else{
	if(-Not (Test-Path "$INCLUDEDIR")){
		mkdir "$INCLUDEDIR"
	}
	if(Test-Path "$PKGDIR/pass-winmenu.exe"){
		rm "$PKGDIR/pass-winmenu.exe"
	}
	if(Test-Path "$PKGDIR/pass-winmenu.yaml"){
		rm "$PKGDIR/pass-winmenu.yaml"
	}
}

cp "bin/Release/pass-winmenu.exe" "$PKGDIR/pass-winmenu.exe"
cp "include/packaged-config.yaml" "$PKGDIR/pass-winmenu.yaml"

tools/7za.exe x -aos "include/GnuPG.zip" "-o$INCLUDEDIR"
tools/7za.exe x -aos "include/PortableGit.zip" "-o$INCLUDEDIR"

if($package){
	if(Test-Path "$ZIPDIR"){
		rm "$ZIPDIR"
	}
	tools/7za.exe a "bin/pass-winmenu.zip" "./bin/Release-Packaged/*"
}