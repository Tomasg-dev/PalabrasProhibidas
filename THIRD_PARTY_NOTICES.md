# Third-party notices

PalabrasProhibidas itself is released under the MIT License (see `LICENSE` in the source repository).
The published package also redistributes the following third-party components, unmodified.

## Vosk speech recognition

- Files: `Vosk.dll`, `libvosk.dll`
- Source: https://github.com/alphacep/vosk-api (NuGet package `Vosk` 0.3.38)
- Copyright 2019-2022 Alpha Cephei Inc.
- License: Apache License 2.0 — https://www.apache.org/licenses/LICENSE-2.0

`libvosk.dll` statically includes Kaldi, OpenFst and OpenBLAS, under the Apache 2.0 and BSD licenses of their respective authors.

## Vosk model

- File: `vosk-model-small-es-0.42.zip`
- Source: https://alphacephei.com/vosk/models
- Copyright Alpha Cephei Inc.
- License: Apache License 2.0 — https://www.apache.org/licenses/LICENSE-2.0

## MinGW-w64 runtime libraries

Shipped with the Vosk package because `libvosk.dll` depends on them.

- `libstdc++-6.dll`, `libgcc_s_seh-1.dll` — part of GCC, Copyright Free Software Foundation, Inc. Licensed under GPL 3.0 with the GCC Runtime Library Exception 3.1, which permits distribution alongside independent programs — https://www.gnu.org/licenses/gcc-exception-3.1.html
- `libwinpthread-1.dll` — part of the MinGW-w64 winpthreads library, Copyright the mingw-w64 project. Licensed under the MIT and BSD 3-Clause licenses — https://www.mingw-w64.org/

## The game

This mod uses R.E.P.O.'s own enemies and systems at runtime. It contains no code or assets from the game.
