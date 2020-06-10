config ?= Debug

build:
	dotnet build -c $(config)

test:
	dotnet test -c $(config)

client:
	cd samples/client; dotnet run -c $(config)

server:
	cd samples/server; dotnet run -c $(config)

