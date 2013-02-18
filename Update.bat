xcopy ..\Watsonia\Watsonia.Data\*.* Watsonia.Data\ /s /y /d /exclude:exclude.txt
xcopy ..\Watsonia\Watsonia.Data.SqlServer\*.* Watsonia.Data.SqlServer\ /s /y /d /exclude:exclude.txt
xcopy ..\Watsonia\Watsonia.Data.SqlServerCe\*.* Watsonia.Data.SqlServerCe\ /s /y /d /exclude:exclude.txt
xcopy ..\Watsonia\Watsonia.Data.Tests\*.* Watsonia.Data.Tests\ /s /y /d /exclude:exclude.txt

pause
