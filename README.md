# webminerpool 

**Complete sources** for a Cryptonight (diverse variants, without randomX) webminer.


###
_The server_ is written in **C#**, **optionally calling C**-routines to check hashes calculated by the clients. 
It acts as a proxy server for common pools.


_The client_ runs in the browser using javascript and webassembly. 
**websockets** are used for the connection between the client and the server, **webassembly** to perform hash calculations, **web workers** for threads.

There is a **docker** file available. See below.

# Is RandomX supported?

No. At the moment there is no efficient way to implement this in the browser.

The strategy is to rely on coins which are more easily mined in the browser. Pools like moneroocean.stream let you mine them in direct exchange for Monero.

# Supported algorithms

| #  |  xmrig short notation | webminerpool internal | description |
| -- | --------------| --------------------------------- | ------------------------------------------------ |
| 1  | cn            | algo="cn", variant=-1             | autodetect cryptonight variant (block.major - 6) |
| 2  | cn/0          | algo="cn", variant=0              | original cryptonight                             |
| 3  | cn/1          | algo="cn", variant=1              | also known as monero7 and cryptonight v7         |
| 4  | cn/2          | algo="cn", variant=2 or 3         | cryptonight variant 2                            |
| 5  | cn/r          | algo="cn", variant=4              | cryptonight variant 4 also known as cryptonightR |
| 6  | cn-lite       | algo="cn-lite", variant=-1        | same as #1 with memory/2, iterations/2           |
| 7  | cn-lite/0     | algo="cn-lite", variant=0         | same as #2 with memory/2, iterations/2           |
| 8  | cn-lite/1     | algo="cn-lite", variant=1         | same as #3 with memory/2, iterations/2           |
| 9  | cn-pico/trtl  | algo="cn-pico", variant=2 or 3    | same as #4 with memory/8, iterations/8           |
| 10 | cn-half       | algo="cn-half", variant=2 or 3    | same as #4 with memory/1, iterations/2           |
| 11 | cn/rwz       | algo="cn-rwz", variant=2 or 3    | same as #4 with memory/1, iterations*3/4           |


# Repository Content

### SDK

The SDK directory contains all client side mining scripts which allow mining in the browser.

#### Minimal working example

```html
<script src="webmr.js"></script>

<script>
	server = "ws://localhost:8181"
	startMining("moneroocean.stream","49kkH7rdoKyFsb1kYPKjCYiR2xy1XdnJNAY1e7XerwQFb57XQaRP7Npfk5xm1MezGn2yRBz6FWtGCFVKnzNTwSGJ3ZrLtHU"); 
</script>
```
webmr.js can be found under SDK/miner_compressed.

The startMining function can take additional arguments

```javascript
startMining(pool, address, password, numThreads, userid);
```

- pool, this has to be a pool registered at the server.
- address, a valid XMR address you want to mine to.
- password, password for your pool. Often not needed.
- numThreads, the number of threads the miner uses. Use "-1" for auto-config.
- userid - not used anymore but still available at the server side.

To **throttle** the miner just use the global variable "throttleMiner", e.g. 

```javascript
startMining(..);
throttleMiner = 20;
```

If you set this value to 20, the cpu workload will be approx. 80% (for 1 thread / CPU). Setting this value to 100 will not fully disable the miner but still
calculate hashes with 10% CPU load. 

If you do not want to show the user your address or even the password you have to create  a *loginid* (see logins.json). With the *loginid* you can start mining with

```javascript
startMiningWithId(loginid)
```

or with optional input parameters:

```javascript
startMiningWithId(loginid, numThreads, userid)
```

#### What are all the *.js files?

SDK/miner_compressed/webmr.js simply combines 

 1. SDK/miner_raw/miner.js
 2. SDK/miner_raw/worker.js
 3. SDK/miner_raw/cn.js

Where *miner.js* handles the server-client connection, *worker.js* are web workers calculating cryptonight hashes using *cn.js* - a emscripten generated wrapped webassembly file. The webassembly file can also be compiled by you, see section hash_cn below.

### Server

The C# server. It acts as proxy between the clients (the browser miners) and the pool server. Whenever several clients use the same credentials (pool, address and password) they get "bundled" into a single pool connection, i.e. only a single connection is seen by the pool server. This measure helps to prevent overloading regular pool servers with many low-hash web miners.

The server uses asynchronous websockets provided by the
[FLECK](https://github.com/statianzo/Fleck) library. Smaller fixes were applied to keep memory usage low. The server code should be able to handle several thousand connections with modest resource usage.

Install .NET 5.0 (https://dotnet.microsoft.com/download/dotnet/5.0) on your system and follow these instructions:

 To compile change to the server directory and execute
 ```bash
dotnet build -c Release
```
Run the server with

```bash
dotnet run -c Release
```

 Optionally you can compile the C-library **libhash**.so found in *hash_cn*. Place this library in the same folder as the server executable. If this library is present the server will make use of it and check hashes which gets submitted by the clients. If clients submit bad hashes ("low diff shares"), they get disconnected. The server occasionally writes ip-addresses to *ip_list*. These addresses should get (temporarily) banned on your server for example by adding them to [*iptables*](http://ipset.netfilter.org/iptables.man.html). The file can be deleted after the ban. See *Firewall.cs* for rules when a client is seen as malicious - submitting wrong hashes is one possibility.

 Without a **SSL certificate** the server will open a regular websocket (ws://0.0.0.0:8181). To use websocket secure (ws**s**://0.0.0.0:8181) you should place *certificate.pfx* (a  pkcs12 file) into the server directory. The default password which the server uses to load the certificate is "miner". To create a pkcs12 file from regular certificates, e.g. from [*Let's Encrypt*](https://letsencrypt.org/), use the command

```bash
openssl pkcs12 -export -out certificate.pfx -inkey privkey.pem -in cert.pem -certfile chain.pem
```

The server should autodetect the certificate on startup and create a secure websocket.

**Attention:** Most linux based systems have a (low) fixed limit of
available file-descriptors configured ("ulimit"). This can cause an
unwanted upper limit for the users who can connect (typical 1000). You
should change this limit if you want to have more connections.

### hash_cn

The cryptonight hashing functions in C-code. With simple Makefiles (use the "make" command to compile) for use with gcc and emcc - the [emscripten](https://github.com/kripken/emscripten) webassembly compiler. *libhash* should be compiled so that the server can check hashes calculated by the user.

# Dockerization

Find the original pull request with instructions by nierdz [here](https://github.com/notgiven688/webminerpool/pull/62).

Added Dockerfile and entrypoint.sh.
Inside entrypoint.sh, if `$DOMAIN` is provided, a certificate is registered and packed in pkcs12 format to be used with server.exe.

```bash
cd webminerpool
docker build -t webminerpool .
```

To run it: 

```bash
docker run -d -p 80:80 -p 8181:8181 -e DOMAIN="" webminerpool
```

The 80:80 bind is used to obtain a certificate.
The 8181:8181 bind is used for server itself.

If you want to bind these ports to a specific IP, you can do this:

```bash
docker run -d -p xx.xx.xx.xx:80:80 -p xx.xx.xx.xx:8181:8181 -e DOMAIN=mydomain.com webminerpool
```

You can even use docker-compose, here is a sample snippet:

```
webminer:
  container_name: webminer
  image: webminer:1.0
  build:
    context: ./webminerpool
  restart: always
  ports:
    - ${WEBMINER_IP}:80:80
    - ${WEBMINER_IP}:8181:8181
  environment:
    DOMAIN: ${WEBMINER_DOMAIN}
  networks:
    - my-network
```

To use this snippet, you need to define `$WEBMINER_DOMAIN` and `$WEBMINER_IP` in a `.env` file.
