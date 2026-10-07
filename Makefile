PYTHON ?= python3
DOTNET ?= dotnet
COMPATIBILITY_MODULES := MainButtons CharacterEditor MedicalIcons FoodAlert RimJobWorld SlaveSuppression
COMPATIBILITY_BUILDS := $(addprefix build-,$(COMPATIBILITY_MODULES))
.DEFAULT_GOAL := check

.PHONY: check test test-python test-core compatibility pack install preview
.PHONY: $(COMPATIBILITY_BUILDS)
preview:
	rsvg-convert Artwork/Preview.svg -o About/Preview.png
	rsvg-convert Artwork/Workshop-Preview.svg -o Artwork/Workshop-Preview.png
	rsvg-convert Artwork/GitHub-Badge.svg -w 128 -o Artwork/GitHub-Badge.png

check:
	$(PYTHON) tools/mod.py check

test: test-python test-core

test-python:
	$(PYTHON) -m unittest discover -s tests -v

test-core:
	$(DOTNET) run --project tests/CoreLogic -c Release

compatibility: $(COMPATIBILITY_BUILDS)

$(COMPATIBILITY_BUILDS): build-%:
	$(DOTNET) build Source/KoRimUtility.$* -c Release

pack: check compatibility
	$(PYTHON) tools/mod.py pack

install: check compatibility
	$(PYTHON) tools/mod.py install --mods-dir "$(MODS_DIR)"
