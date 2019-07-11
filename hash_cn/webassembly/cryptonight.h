#ifndef CRYPTONIGHT_H
#define CRYPTONIGHT_H
#include "argon2.h"
#ifdef __cplusplus
extern "C" {
#endif

void cryptonight(void *output, const void *input, size_t len, int algo, int variant, int height);
struct cryptonight_ctx;

void chukwa_slow_hash(const void *data, size_t length, void *output, uint64_t mem, uint64_t hashlen, uint64_t saltlen, uint64_t iters, uint64_t threads) {
    uint8_t salt[16];
    memcpy(salt, data, sizeof(salt));
    argon2id_hash_raw(iters, mem, threads, data, length, salt, saltlen, output, hashlen);
}

#ifdef __cplusplus
}
#endif

#endif
