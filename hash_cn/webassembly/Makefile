TARGET = prog
LIBS = -lm

# Looks like there is a regression in the emcc compiler. In my tests
# the compiled code made with current versions are slower than the 
# 1.37.40 reference.

CC = ~/.emcc_wrapper/1.37.40/emscripten/emcc -O3  -s SINGLE_FILE=1 -s  NO_FILESYSTEM=1 -s 'EXTRA_EXPORTED_RUNTIME_METHODS=["ccall", "cwrap"]'  --llvm-lto 1 -s TOTAL_MEMORY=67108864  -s WASM=1 -s "BINARYEN_TRAP_MODE='allow'" -s EXPORTED_FUNCTIONS="['_hash_cn']"  --shell-file html_template/shell_minimal.html
CFLAGS =  -Wall 


# SINGLE_FILE=1

# -s ASSERTIONS=1
#  -s SINGLE_FILE=1
.PHONY: default all clean

default: $(TARGET)
all: default

OBJECTS = $(patsubst %.c, %.o, $(wildcard *.c))
HEADERS = $(wildcard *.h)

%.o: %.c $(HEADERS) $(CC) $(CFLAGS) -c $< -o $@

.PRECIOUS: $(TARGET) $(OBJECTS)

$(TARGET): $(OBJECTS)
	$(CC) $(OBJECTS) -Wall $(LIBS) -o cn.html

clean:
	-rm -f *.o
	-rm -f $(TARGET)
