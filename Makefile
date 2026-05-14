.PHONY: run build test check clean

build:
	dotnet build

test:
	dotnet test

check: build test

help:
	dotnet run --project src\StructuralSolver2D.Cli -- help
	
run: 
	dotnet run --project src\StructuralSolver2D.Cli -- example simple-supported-beam
	
analyse:
	dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json

clean:
	@echo "Removing bin and obj folders..."
ifeq ($(OS),Windows_NT)
	@if exist src for /d /r src %%d in (bin,obj) do @if exist "%%d" rmdir /s /q "%%d"
	@if exist tests for /d /r tests %%d in (bin,obj) do @if exist "%%d" rmdir /s /q "%%d"
else
	find src tests -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
endif
	@echo "Clean completed."