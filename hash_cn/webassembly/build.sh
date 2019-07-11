#!/bin/bash
emcc *.c argon2/*.c -O3 \
    -s SINGLE_FILE=1 \
    -s 'EXTRA_EXPORTED_RUNTIME_METHODS=["ccall", "cwrap"]' \
    --llvm-lto 1 \
    -s WASM=1 \
    -s "BINARYEN_TRAP_MODE='allow'" \
    -s EXPORTED_FUNCTIONS="['_hash_cn']" \
    -o cryptonight.js
