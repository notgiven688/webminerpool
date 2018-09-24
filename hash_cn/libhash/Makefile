TARGET = prog
LIBS = -lm
CC = gcc -O3 -shared -pthread -fPIC
CFLAGS = -g -Wall -std=gnu99 -maes

.PHONY: default all clean

default: $(TARGET)
all: default

OBJECTS = $(patsubst %.c, %.o, $(wildcard *.c))
HEADERS = $(wildcard *.h)

%.o: %.c $(HEADERS) $(CC) $(CFLAGS) -c $< -o $@

.PRECIOUS: $(TARGET) $(OBJECTS)

$(TARGET): $(OBJECTS)
	$(CC) $(OBJECTS) -Wall $(LIBS) -o libhash.so

clean:
	-rm -f *.o
	-rm -f $(TARGET)
