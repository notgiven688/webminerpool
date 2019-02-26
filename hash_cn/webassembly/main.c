#include <stdio.h>
#include <time.h>
#include <string.h>
#include <stdlib.h>
#include <math.h>
#include <stdint.h>

#include "cryptonight.h"

char output[(32 * 2) + 1];
  
char* hash_cn(char* hex,int lite, int variant, int height)
{
    int len = strlen(hex) / 2;
    
    unsigned char inp[len];

    char *pos = hex;
    for( size_t i = 0; i < len; i++)  { sscanf(pos, "%2hhx", &inp[i]); pos += 2; }

    unsigned char hash[32];

    if(variant == -1)
    variant = ((const uint8_t *)inp)[0] >= 7 ? ((const uint8_t *)inp)[0] - 6 : 0;

    cryptonight(hash, inp, len, lite, variant, height);
    
    char *ptr = &output[0];
    
    for (size_t i = 0; i < 32; i++) { ptr += sprintf (ptr, "%02x",hash[i]); }
 
    return &output[0];
}
