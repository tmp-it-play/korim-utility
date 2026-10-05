PYTHON ?= python3
DOTNET ?= dotnet
.DEFAULT_GOAL := check

.PHONY: check test test-compatibility compatibility pack install preview
preview:
	rsvg-convert Artwork/Preview.svg -o About/Preview.png

check:
	$(PYTHON) tools/mod.py check

test:
	$(PYTHON) -m unittest discover -s tests -v

compatibility:
	$(DOTNET) build Source/KoRimUtility.MainButtons -c Release
	$(DOTNET) build Source/KoRimUtility.CharacterEditor -c Release
	$(DOTNET) build Source/KoRimUtility.MedicalIcons -c Release

test-compatibility:
	$(DOTNET) run --project tests/MainButtonsCompatibility -c Release
	$(DOTNET) run --project tests/CharacterEditorCompatibility -c Release -- "$(CURDIR)"
	$(DOTNET) run --project tests/MedicalIconsCompatibility -c Release

pack: check compatibility
	$(PYTHON) tools/mod.py pack

install: check compatibility
	$(PYTHON) tools/mod.py install --mods-dir "$(MODS_DIR)"
