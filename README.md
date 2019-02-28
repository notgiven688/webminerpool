# webminerpool 

**Complete sources** for a Monero (cryptonight and variants) webminer. **Hard fork ready**.


###
_The server_ is written in **C#**, **optionally calling C**-routines to check hashes calculated by the clients. It acts as a proxy server for common pools.


_The client_ runs in the browser using javascript and webassembly. 
**websockets** are used for the connection between the client and the server, **webassembly** to perform hash calculations, **web workers** for threads.

Thanks to [nierdz](https://github.com/notgiven688/webminerpool/pull/62) there is a **docker** file available. See below.

# Will the hardfork (<span style="color:red">March 2019</span>) be supported?

Yes. Update to the current Master branch and you should be fine. Much work was put into optimizing the miner
once again. 

Unfortunately the newest version of cryptonight, cn/r (cnv4), does perform poorly on the browser. To partly compensate for this I added cn-pico/trtl and cn-half. If you mine to a pool
which allows autoswitching algorithms (at the moment [moneroocean.stream](https://moneroocean.stream)) webminerpool will automatically
switch to an algorithm which is most profitable at the moment.

## Update notes: It is beneficial to first update your clients (step_A) to the newest mining script (Version 7, the version number can be found in the "handshake-data" within the source code). Wait a few days till your user base followed (because of browser caching) and then update to the newest server version (step_B). This is recommended because of the possibility that the new server negotiates a mining algorithm with the pool, which is not supported by an old client (and therefore is not forwarded by the server). 

## step_A and step_B have to be performed before March 9th!

# Currently supported algorithms

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

 # What is new?

- **March 1, 2019** 
	- Added cryptonight v4. Hard fork ready! Added support for cn/half and cn-pico/trtl. Added support for auto-algo switching. (**client-side** / **server-side**)

- **September 27, 2018** 
	- Added cryptonight v2. Hard fork ready! (**client-side** / **server-side**)

- **June 15, 2018** 
	- Support for blocks with more than 2^8 transactions. (**client-side** / **server-side**)

- **May 21, 2018** 
	- Support for multiple open tabs. Only one tab is constantly mining if several tabs/browser windows are open. (**client-side**)

- **May 6, 2018** 
	- Check if webasm is available. Please update the script. (**client-side**)

- **May 5, 2018** 
	- Support for multiple websocket servers in the client script (load-distribution).

- **April 26, 2018** 
	- A further improvement to fully support the [extended stratum protocol](https://github.com/xmrig/xmrig-proxy/blob/dev/doc/STRATUM_EXT.md#mining-algorithm-negotiation).  (**server-side**)
	- A simple json config-file holding all available pools. (**server-side**)

- **April 22, 2018** 
	- All cryptonight and cryptonight-light based coins are supported in a single miner. [Stratum extension](https://github.com/xmrig/xmrig-proxy/blob/dev/doc/STRATUM_EXT.md#mining-algorithm-negotiation) were implemented: The server now takes pool suggestions (algorithm and variant) into account. Defaults can be specified for each pool - that makes it possible to mine coins like Stellite, Turtlecoin,.. (**client/server-side**)
	- Client reconnect time gets larger with failed attempts. (**client-side**)

# Repository Content

### SDK

The SDK directory contains all client side mining scripts which allow mining in the browser.

#### Minimal working example

```html
<script src="webmr.js"></script>

<script>
	server = "ws://localhost:8181"
	startMining("minexmr.com","49kkH7rdoKyFsb1kYPKjCYiR2xy1XdnJNAY1e7XerwQFb57XQaRP7Npfk5xm1MezGn2yRBz6FWtGCFVKnzNTwSGJ3ZrLtHU"); 
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
- userid, allows you to identify the number of hashes calculated by a user. Can be any string with a length < 200 characters.

To **throttle** the miner just use the global variable "throttleMiner", e.g. 

```javascript
startMining(..);
throttleMiner = 20;
```

If you set this value to 20, the cpu workload will be approx. 80% (for 1 thread / CPU). Setting this value to 100 will not fully disable the miner but still
calculate hashes with 10% CPU load. 

If you do not want to show the user your address or even the password you have to create  a *loginid*. With the *loginid* you can start mining with

```javascript
startMiningWithId(loginid)
```

or with optional input parameters:

```javascript
startMiningWithId(loginid, numThreads, userid)
```

Get a *loginid* by opening *register.html* in SDK/other. You also find a script which enumerates all available pools and a script which shows you the amount of hashes calculated by a *userid*. These files are quite self-explanatory.

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

The following compilation instructions apply for linux systems. Windows users have to use Visual Studio to compile the sources.

 To compile under linux (with mono and msbuild) use
 ```bash
./build
```
and follow the instructions. No additional libraries are needed.

```bash
mono server.exe
```

should run the server.

 Optionally you can compile the C-library **libhash**.so found in *hash_cn*. Place this library in the same folder as *server.exe*. If this library is present the server will make use of it and check hashes which gets submitted by the clients. If clients submit bad hashes ("low diff shares"), they get disconnected. The server occasionally writes ip-addresses to *ip_list*. These addresses should get (temporarily) banned on your server for example by adding them to [*iptables*](http://ipset.netfilter.org/iptables.man.html). The file can be deleted after the ban. See *Firewall.cs* for rules when a client is seen as malicious - submitting wrong hashes is one possibility.

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
docker run -d -p 80:80 -p 8181:8181 -e DOMAIN=mydomain.com webminerpool
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
    args:
      - DONATION_LEVEL=${WEBMINER_DONATION_LEVEL}
  restart: always
  ports:
    - ${WEBMINER_IP}:80:80
    - ${WEBMINER_IP}:8181:8181
  environment:
    DOMAIN: ${WEBMINER_DOMAIN}
  networks:
    - my-network
```

To use this snippet, you need to define `$WEBMINER_DONATION_LEVEL`, `$WEBMINER_DOMAIN` and `$WEBMINER_IP` in a `.env` file.

# Developer Donations

contact: webminerpool@protonmail.com

By default a server-side 3% dev-donation is configured. Leaving this fee at the current level is highly appreciated. If you want
to turn it off or just find the content of this repository helpful consider a one time donation, the addresses are as follows:


```
BTC - 175jHD6ErDhZHoW4u54q5mr98L9KSgm56D
XMR - 49kkH7rdoKyFsb1kYPKjCYiR2xy1XdnJNAY1e7XerwQFb57XQaRP7Npfk5xm1MezGn2yRBz6FWtGCFVKnzNTwSGJ3ZrLtHU
AEON - WmtUFkPrboCKzL5iZhia4iNHKw9UmUXzGgbm5Uo3HPYwWcsY1JTyJ2n335gYiejNysLEs1G2JZxEm3uXUX93ArrV1yrXDyfPH
```
