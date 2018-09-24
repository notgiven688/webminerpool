#ifndef GROESTL_H
#define GROESTL_H

#ifdef __cplusplus
extern "C" {
#endif

extern void groestl(const unsigned char *input,
    unsigned long long len,
    unsigned char *output);

#ifdef __cplusplus
}
#endif

#endif
