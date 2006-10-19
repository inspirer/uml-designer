@echo off
rmdir /q /s UMLDes.StaticView\bin > nul 2> nul
rmdir /q /s UMLDes.StaticView\obj > nul 2> nul
rmdir /q /s UMLDes.View\bin > nul 2> nul
rmdir /q /s UMLDes.View\obj > nul 2> nul
rmdir /q /s UMLDes.Controls\bin > nul 2> nul
rmdir /q /s UMLDes.Controls\obj > nul 2> nul
rmdir /q /s UMLDes.Model.CSharp\bin > nul 2> nul
rmdir /q /s UMLDes.Model.CSharp\obj > nul 2> nul
rmdir /q /s UMLDes.Model.Tests\bin > nul 2> nul
rmdir /q /s UMLDes.Model.Tests\obj > nul 2> nul
rmdir /q /s UMLDes.Model\bin > nul 2> nul
rmdir /q /s UMLDes.Model\obj > nul 2> nul
rmdir /q /s UMLDes.Gui\bin > nul 2> nul
rmdir /q /s UMLDes.Gui\obj > nul 2> nul
del /q arch.rar > nul 2> nul
winrar a arch.rar todo pack.bat UMLDes.Gui UMLDes.Model UMLDes.Model.CSharp UMLDes.Model.Tests UMLDes.StaticView UMLDes.View UMLDes.Controls UmlDes.sln
echo ($min,$h,$d,$m,$y) = ((localtime)[1..5]); rename("arch.rar",sprintf( "save\\UmlDes.%%02d%%02d%%02d(%%02d%%02d).rar", $y-100, $m+1, $d, $h, $min) ); > rename_it.pl
perl rename_it.pl
del /q rename_it.pl
