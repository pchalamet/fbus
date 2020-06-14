config ?= Debug
version ?= 0.0.0

build:
	dotnet build -c $(config)

test:
	dotnet test -c $(config)

nuget:
	dotnet pack -c $(config) /p:Version=$(version) -o nuget

publish:
	dotnet nuget push nuget/*.nupkg -k $(nugetkey) -s https://api.nuget.org/v3/index.json --skip-duplicate

client:
	cd samples/client; dotnet run -c $(config)

server:
	cd samples/server; dotnet run -c $(config)

