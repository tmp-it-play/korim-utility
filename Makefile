PYTHON ?= python3
.DEFAULT_GOAL := check

.PHONY: check test pack install preview
preview:
	rsvg-convert Artwork/Preview.svg -o About/Preview.png

check:
	$(PYTHON) tools/mod.py check

test:
	$(PYTHON) -m unittest discover -s tests -v

pack: check
	$(PYTHON) tools/mod.py pack

install: check
	$(PYTHON) tools/mod.py install --mods-dir "$(MODS_DIR)"
