#include <stdio.h>
#include <time.h>
#include <string.h>
#include <stdlib.h>
#include <math.h>
#include <stdint.h>

#include "slow-hash.h"

  
char* hash_cn(char* hex, int light, int variant)
{
     char* output = (char*) malloc((64+1)*sizeof(char));
  
     int len = strlen(hex)/2;
     
     unsigned char val[len], *pos = hex;
     
     for(size_t count = 0; count < len; count++)  {
         sscanf(pos, "%2hhx", &val[count]);
         pos += 2;
     }
     
    //int variant = ((const uint8_t*)val)[0] >= 7 ? ((const uint8_t*)val)[0] - 6 : 0;

    unsigned char hash[32];

    cn_slow_hash(&val, len, &hash, light, variant, 0);

    char *ptr = &output[0];
    
    for (size_t i = 0; i < 32; i++) {
	ptr += sprintf (ptr, "%02x",hash[i]);
    }

    return &output[0];
}

void hash_free(void* ptr)
{
    free(ptr);
}
