PYTHON ?= python3
DOTNET ?= dotnet
.DEFAULT_GOAL := check

.PHONY: check test test-compatibility compatibility pack install preview
preview:
	rsvg-convert Artwork/Preview.svg -o About/Preview.png
	rsvg-convert Artwork/Workshop-Preview.svg -o Artwork/Workshop-Preview.png
	rsvg-convert Artwork/GitHub-Badge.svg -w 128 -o Artwork/GitHub-Badge.png

check:
	$(PYTHON) tools/mod.py check

test:
	$(PYTHON) -m unittest discover -s tests -v

compatibility:
	$(DOTNET) build Source/KoRimUtility.MainButtons -c Release
	$(DOTNET) build Source/KoRimUtility.CharacterEditor -c Release
	$(DOTNET) build Source/KoRimUtility.MedicalIcons -c Release
	$(DOTNET) build Source/KoRimUtility.FoodAlert -c Release
	$(DOTNET) build Source/KoRimUtility.RimJobWorld -c Release
	$(DOTNET) build Source/KoRimUtility.SlaveSuppression -c Release

test-compatibility:
	$(DOTNET) run --project tests/MainButtonsCompatibility -c Release
	$(DOTNET) run --project tests/CharacterEditorCompatibility -c Release -- "$(CURDIR)"
	$(DOTNET) run --project tests/MedicalIconsCompatibility -c Release
	$(DOTNET) run --project tests/FoodAlertCompatibility -c Release -- "$(CURDIR)"
	$(DOTNET) run --project tests/RimJobWorldCompatibility -c Release -- "$(CURDIR)"
	$(DOTNET) run --project tests/SlaveSuppressionCompatibility -c Release -- "$(CURDIR)"

pack: check compatibility
	$(PYTHON) tools/mod.py pack

install: check compatibility
	$(PYTHON) tools/mod.py install --mods-dir "$(MODS_DIR)"
