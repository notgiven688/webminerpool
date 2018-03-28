#include <stdio.h>
#include <time.h>
#include "cryptonight.h"

#include <string.h>
#include <stdlib.h>
#include <math.h>
#include "jh.h"

#include <stdint.h>

char* tohex(unsigned char * in)
{
  size_t size = 32;
  
  char output[(size * 2) + 1];
  
  char *ptr = &output[0];
  
  int i;

  for (i = 0; i < size; i++)
  { 
      ptr += sprintf (ptr, "%02x",in[i]);
  }
  
  return &output[0];
}


char* hash_cn(char* hex, char* nonce)
{
    unsigned char inp[76];

    char *pos = hex;
    for( size_t i = 0; i < 76; i++)  { sscanf(pos, "%2hhx", &inp[i]); pos += 2; }

    pos = nonce;

    for(size_t i = 39; i < 43; i++)  { sscanf(pos, "%2hhx", &inp[i]); pos += 2; }

    unsigned char hash[76];
    cryptonight(hash, inp, 76);
 

    return tohex(hash);
}


int main (void)
{
  return 0;
}
