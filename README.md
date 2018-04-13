# webminerpool 

**Complete sources** for a monero (aeon) webminer. **Hard fork (v7) ready**.  Try it here:

#### [Monero example](https://webminerpool.com/)

#### [Aeon example](https://webminerpool.com/aeon)

###
_The server_ is written in **C#**, **optionally calling C**-routines to check hashes calculated by the clients. It acts as a proxy server for common pools.

_The client_ runs in the browser using javascript and webassembly. 
**websockets** are used for the connection between the client and the server, **webassembly** to perform hash calculations, **web workers** for threads.

The project exists since September 2017 and is now fully open sourced, i.e. the server and the client (including the webassembly miner) can be build from the codebase.

# Repository Content

### SDK

The SDK directory contains all client side mining scripts which allow mining in the browser.

#### Minimal working example

```html
<!DOCTYPE html>
<html>
<body>

<script src="https://webminerpool.com/webmr.js"></script>
<!-- for aeon use: https://webminerpool.com/aeon/webmr.js -->

<script>
	startMining("minexmr.com","49kkH7rdoKyFsb1kYPKjCYiR2xy1XdnJNAY1e7XerwQFb57XQaRP7Npfk5xm1MezGn2yRBz6FWtGCFVKnzNTwSGJ3ZrLtHU"); 
</script>

</body>
</html>
```

In this example the webminerpool.com server is used. A dynamic fee (about 4%) is used to cover hosting costs. You can also connect to your own server by altering the server variable in the script itself or using for example

```html
<script src="webmr.js"></script>

<script>
	server = "ws://localhost:8181"
	startMining("minexmr.com","49kkH7rdoKyFsb1kYPKjCYiR2xy1XdnJNAY1e7XerwQFb57XQaRP7Npfk5xm1MezGn2yRBz6FWtGCFVKnzNTwSGJ3ZrLtHU"); 
</script>
```
webmr.js can be found under SDK/xmr/miner_compressed (or for Aeon SDK/aeon/miner_compressed).

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

If you do not want to show the user your address or even the password you have to create  a "loginid". With the loginid you can start mining with

```javascript
startMiningWithId(loginid)
```

or with optional input parameters:

```javascript
startMiningWithId(loginid, numThreads, userid)
```

Get a loginid by opening *register.html* in SDK/xmr/other. You also find a script which enumerates all available pools and a script which shows you the amount of hashes calculated by a userid. These files are quite self-explanatory.

#### What are all the *.js files?

SDK/xmr/miner_compressed/webmr.js simply combines 

 1. SDK/xmr/miner_raw/miner.js
 2. SDK/xmr/miner_raw/worker.js
 3. SDK/xmr/miner_raw/cn.js

Where *miner.js* handles the server-client connection, worker.js are web workers calculating cryptonight hashes using *cn.js* - a emscripten generated wrapped webassembly file. The webassembly file can also be compiled by you, see "hash_cn" below.

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

 Without a **SSL certificate** the server will open a regular websocket (ws://0.0.0.0:8181). To use websocket secure (ws**s**://0.0.0.0:8181) you should place *certificate.pfx* (a  pkcs12 file) into the server directory. The default password which the server uses to load the certificate is "miner". To create a pkcs12 from the certificates you get for example from [*Let's Encrypt*](https://letsencrypt.org/) use the command

```bash
openssl pkcs12 -export -out certificate.pfx -inkey privkey.pem -in cert.pem -certfile chain.pem
```

The server should autodetect the certificate on startup and create a secure websocket.

### hash_cn

The monero/aeon cryptonight hashing functions in C-code. With simple Makefiles for use with gcc and emcc - the emscripten webassembly compiler (use the "make" command to compile). libhash should be compiled so that the server can check hashes calculated by the user.

# ToDo

Refactoring. Documentation.

# Contact

webminerpool@protonmail.com

# Developer Donations
If you find the content of this repository helpful consider a one time donation, the addresses are as follows:
- BTC - 175jHD6ErDhZHoW4u54q5mr98L9KSgm56D
-   XMR - 49kkH7rdoKyFsb1kYPKjCYiR2xy1XdnJNAY1e7XerwQFb57XQaRP7Npfk5xm1MezGn2yRBz6FWtGCFVKnzNTwSGJ3ZrLtHU
-   AEON - WmtUFkPrboCKzL5iZhia4iNHKw9UmUXzGgbm5Uo3HPYwWcsY1JTyJ2n335gYiejNysLEs1G2JZxEm3uXUX93ArrV1yrXDyfPH

