#ifndef CRYPTONIGHT_H
#define CRYPTONIGHT_H

#ifdef __cplusplus
extern "C" {
#endif

void cryptonight(void *output, const void *input, size_t len, int algo, int variant, int height);
struct cryptonight_ctx;

#ifdef __cplusplus
}
#endif

#endif
