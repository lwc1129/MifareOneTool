macOS NFC Tools (nfc-bin-macos)
================================

To bundle macOS binaries with the app, copy the following files here
after installing libnfc via Homebrew on a Mac:

  brew install libnfc mfoc

Then copy the binaries:

  cp $(which nfc-scan-device) nfc-bin-macos/
  cp $(which nfc-mfclassic)   nfc-bin-macos/
  cp $(which nfc-list)        nfc-bin-macos/
  cp $(which mfoc)            nfc-bin-macos/
  cp $(which nfc-mfsetuid)    nfc-bin-macos/

IMPORTANT — dylib dependencies:
  Homebrew binaries link against dylibs in /opt/homebrew/lib (Apple Silicon)
  or /usr/local/lib (Intel). If you want a fully self-contained bundle,
  use `dylibbundler` to collect and re-link dylibs:

    brew install dylibbundler
    dylibbundler -od -b -x nfc-bin-macos/mfoc -d nfc-bin-macos/libs/ -p @executable_path/libs/

  Alternatively, users can install libnfc via Homebrew and the app will
  fall back to system tools automatically (no bundled binaries needed).

Apple Silicon path : /opt/homebrew/bin/
Intel Mac path     : /usr/local/bin/
