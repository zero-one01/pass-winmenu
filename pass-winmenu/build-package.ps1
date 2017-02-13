param(
	[switch]$full,
	[switch]$package,
	[switch]$compress
)

$PKGDIR="bin/Release-Packaged"
$ZIPDIR="bin/pass-winmenu.zip"
$INCLUDEDIR="$PKGDIR/lib"

if($full){
	rm -recurse "$PKGDIR"
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
cp "bin/Release/pass-winmenu.exe" "$PKGDIR/pass-winmenu.exe"
cp "include/packaged-config.yaml" "$PKGDIR/pass-winmenu.yaml"

tools/7za.exe x -aos "include/GnuPG.zip" "-o$INCLUDEDIR"

if($package){
	if(Test-Path "$ZIPDIR"){
		echo "Removing old package: $ZIPDIR"
		rm "$ZIPDIR"
	}
	cd bin
	if($compress){
		../tools/7za.exe a -mm=Deflate -mfb=258 -mpass=15 "pass-winmenu.zip" "Release-Packaged/*"
	}else{
		../tools/7za.exe a "pass-winmenu.zip" "Release-Packaged/*"
	}
	../tools/7za.exe rn "pass-winmenu.zip" "Release-Packaged" "pass-winmenu"
	cd ..
}
