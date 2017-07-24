param(
	[switch]$Clean,
	[switch]$Package,
	[switch]$Compress,
	[switch]$WithGpg
)

$PKGDIR="bin/Release-Packaged"
$ZIPDIR="bin/pass-winmenu.zip"
$INCLUDEDIR="$PKGDIR/lib"
$ZIPNAME="pass-winmenu.zip"

if($Clean){
	if(Test-Path "$PKGDIR"){
		rm -recurse "$PKGDIR"
	}
	mkdir "$PKGDIR"
}else{
	if(Test-Path "$PKGDIR/pass-winmenu.exe"){
		rm "$PKGDIR/pass-winmenu.exe"
	}
	if(Test-Path "$PKGDIR/pass-winmenu.yaml"){
		rm "$PKGDIR/pass-winmenu.yaml"
	}
}

cp -recurse "bin/Release/lib" "$PKGDIR/lib"
# Linux and OSX are not supported, so their libraries do not have to be included.
rm -recurse "$PKGDIR/lib/osx"
rm -recurse "$PKGDIR/lib/linux"
# The PDB files aren't used either, so they can be removed as well.
rm -recurse "$PKGDIR/lib/win32/x64/*.pdb"
rm -recurse "$PKGDIR/lib/win32/x86/*.pdb"

cp "bin/Release/pass-winmenu.exe" "$PKGDIR/pass-winmenu.exe"

if($WithGpg){
	tools/7za.exe x -aos "include/GnuPG.zip" "-o$INCLUDEDIR"
	cp "include/packaged-config.yaml" "$PKGDIR/pass-winmenu.yaml"
}else{
	$ZIPNAME="pass-winmenu-nogpg.zip"
	cp "include/packaged-config-nogpg.yaml" "$PKGDIR/pass-winmenu.yaml"
}

if($Package){
	if(Test-Path "$ZIPDIR"){
		echo "Removing old package: $ZIPDIR"
		rm "$ZIPDIR"
	}
	cd bin
	if($Compress){
		../tools/7za.exe a -mm=Deflate -mfb=258 -mpass=15 "$ZIPNAME" "Release-Packaged/*"
	}else{
		../tools/7za.exe a "$ZIPNAME" "Release-Packaged/*"
	}
	../tools/7za.exe rn "$ZIPNAME" "Release-Packaged" "pass-winmenu"
	cd ..
}
