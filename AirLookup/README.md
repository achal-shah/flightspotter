# Updating the Aircraft table

## Packages
pip install requests

## Examples
python .\airlookup.py C0153B

## Building
1. uv run pip install pyinstaller
2. uv run pyinstaller --onefile -n airlookup __main__.py