rmdir /S /Q src\bin
dir ".\src\bin"
pause
dotnet build ".\src\Brimborium.Extensions.Logging.LocalFile.csproj"
dotnet pack ".\src\Brimborium.Extensions.Logging.LocalFile.csproj" --include-symbols --include-source --configuration Release
dir ".\src\bin\Release\*.*"
pause
dotnet nuget push ".\src\bin\Release\Brimborium.Extensions.Logging.LocalFile.1.0.0.nupkg"  --api-key %githubFlorianGrimmAccessToken% --source "githubFlorianGrimm"
dotnet nuget push ".\src\bin\Release\Brimborium.Extensions.Logging.LocalFile.1.0.0.snupkg"  --api-key %githubFlorianGrimmAccessToken% --source "githubFlorianGrimm"
