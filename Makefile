config ?= Debug

build:
	dotnet build -c $(config)

test:
	dotnet test -c $(config)

