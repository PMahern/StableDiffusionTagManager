dotnet publish -c Release -r win10-x64 /p:PublishSingleFile=true --version-suffix beta1
dotnet publish -c Release -r linux-x64 /p:PublishSingleFile=true --version-suffix beta1
dotnet publish -c Release -r osx-x64 /p:PublishSingleFile=true --version-suffix beta1
