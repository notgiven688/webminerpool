#ifndef BLAKE_H
#define BLAKE_H

#ifdef __cplusplus
extern "C" {
#endif

void blake(const uint8_t *input, uint64_t len, uint8_t *output);

#ifdef __cplusplus
}
#endif

#endif
