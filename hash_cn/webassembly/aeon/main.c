#include <stdio.h>
#include <time.h>
#include <string.h>
#include <stdlib.h>
#include <math.h>
#include <stdint.h>

#include "cryptonight.h"

char* tohex(unsigned char * in)
{
  size_t size = 32;
  
  char output[(size * 2) + 1];
  
  char *ptr = &output[0];

  for (size_t i = 0; i < size; i++)
  { 
      ptr += sprintf (ptr, "%02x",in[i]);
  }
  
  return &output[0];
}

char* hash_cn(char* hex, char* nonce, int variant)
{
    unsigned char inp[76];

    char *pos = hex;
    for( size_t i = 0; i < 76; i++)  { sscanf(pos, "%2hhx", &inp[i]); pos += 2; }

    pos = nonce;

    for(size_t i = 39; i < 43; i++)  { sscanf(pos, "%2hhx", &inp[i]); pos += 2; }

    unsigned char hash[76];

    if(variant == -1)
    variant = ((const uint8_t *)inp)[0] >= 7 ? ((const uint8_t *)inp)[0] - 6 : 0;

    cryptonight(hash, inp, 76, variant);
 
    return tohex(hash);
}

int main (void)
{
  return 0;
}
