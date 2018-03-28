// keccak.h
// 19-Nov-11  Markku-Juhani O. Saarinen <mjos@iki.fi>

#ifndef KECCAK_H
#define KECCAK_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

void keccak(const uint8_t *in, int inlen, uint8_t *md, int mdlen);

void keccakf(uint64_t st[25], int norounds);

void keccak1600(const uint8_t *in, int inlen, uint8_t *md);

#ifdef __cplusplus
}
#endif

#endif
