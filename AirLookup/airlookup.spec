# -*- mode: python ; coding: utf-8 -*-
from PyInstaller.utils.hooks import collect_namespace

a = Analysis(
    ['AirLookup/__main__.py'],
    pathex=[],
    binaries=[],
    datas=[],
    hiddenimports=[
        "azure",
        "azure.identity",
        "azure.keyvault",
        "azure.keyvault.secrets",
        "azure.data",
        "azure.data.tables",
        "azure.core",
    ],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
a.datas += collect_namespace('azure')
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='airlookup',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=True,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
