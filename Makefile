config ?= Debug
version ?= 0.0.0

build:
	dotnet build -c $(config)

test:
	dotnet test -c $(config)

nuget:
	dotnet pack -c $(config) /p:Version=$(version) -o out fbus/fbus.fsproj

publish: nuget
	dotnet nuget push out/fbus.$(version).nupkg -k $NUGET_KEY -s https://api.nuget.org/v3/index.json

client:
	cd samples/client; dotnet run -c $(config)

server:
	cd samples/server; dotnet run -c $(config)

