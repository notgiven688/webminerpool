#include <stdio.h>
#include <time.h>
#include "slow-hash.h"
#include <string.h>
#include <stdlib.h>
#include <math.h>
#include <stdint.h>

char output[200];

char* tohex(unsigned char * in, size_t len)
{
  char *ptr = &output[0];
  
  for (size_t i = 0; i < len; i++)
  {
      ptr += sprintf (ptr, "%02x",in[i]);
  }
  
  return &output[0];
}

char* hash_cn(char* hex)
{
     size_t len = strlen(hex)/2;
     size_t count = 0;
     
     unsigned char val[100], *pos = hex;

     for(count = 0; count < len; count++)  {
         sscanf(pos, "%2hhx", &val[count]);
         pos += 2;
     }

    unsigned char hash[32];
   
    cn_slow_hash(&val,len,&hash);


    return tohex(hash,32);
}

int main (void)
{
    return 0;
}
