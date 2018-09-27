#ifndef SKEIN_H
#define SKEIN_H

#ifdef __cplusplus
extern "C" {
#endif

extern int skein(int hashbitlen, const unsigned char *input,
    size_t input_len, unsigned char *output);

#ifdef __cplusplus
}
#endif

#endif
