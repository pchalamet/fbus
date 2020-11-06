config ?= Debug
version ?= 0.0.0

build:
	dotnet build -c $(config)

test:
	dotnet test -c $(config)

perf:
	dotnet run -c Release --project FBus.PerformanceTests

nuget:
	dotnet pack -c $(config) /p:Version=$(version) -o out

publish: out/*.nupkg
	@for file in $^ ; do \
		dotnet nuget push $file -k $(nugetkey) -s https://api.nuget.org/v3/index.json --skip-duplicate ; \
    done

client:
	cd samples/client; dotnet run -c $(config)

server:
	cd samples/server; dotnet run -c $(config)

