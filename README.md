# webminerpool 

*Complete* sources for a monero (aeon) webminer. Try it here:

#### [Monero example](https://rawgit.com/notgiven688/webminerpool/master/example/xmr/index.html)

#### [Aeon example](https://rawgit.com/notgiven688/webminerpool/master/example/aeon/index.html)

###
_The server_ is written in **C#**, **optionally calling C**-routines to check hashes calculated by the clients. It acts as a proxy server for common pools.

_The client_ runs in the browser using javascript and webassembly. 
**websockets** are used for the connection between the client and the server, **webassembly** to perform hash calculations, **web workers** for threads.

# Overview

### SDK

The SDK directory contains all client site mining scripts which allow mining in the browser.

#### Minimal working example

SDK/xmr/miner_compressed/miner.js can be used to easily set up a miner.

```html
<!DOCTYPE html>
<html>
<body>

<script src="miner.js"></script>

<script>
	startMining("minexmr.com","49kkH7rdoKyFsb1kYPKjCYiR2xy1XdnJNAY1e7XerwQFb57XQaRP7Npfk5xm1MezGn2yRBz6FWtGCFVKnzNTwSGJ3ZrLtHU"); 
</script>

</body>
</html>
```

In this example the webminerpool.com server is used. A dynamic fee (about 4%) is used to cover hosting costs. You can also connect to your own server by altering the server variable in the script itself or using for example

```html
<script>
	server = "ws://localhost:8181"
	startMining("minexmr.com","49kkH7rdoKyFsb1kYPKjCYiR2xy1XdnJNAY1e7XerwQFb57XQaRP7Npfk5xm1MezGn2yRBz6FWtGCFVKnzNTwSGJ3ZrLtHU"); 
</script>

(...)
```
The startMining function can take additional arguments

```javascript
startMining(pool, address, password, numThreads, userid);
```

- pool, this has to be a pool available at webminerpool.com.
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

Get a loginid by opening "register.html" in SDK/xmr/other. You also find a script which enumerates all available pools and a script which shows you the amount of hashes calculated by a userid. These files are quite self-explanatory.

#### What are all the *.js files?

SDK/xmr/miner_compressed/miner.js simply combines 

 1. SDK/xmr/miner_raw/miner.js
 2. SDK/xmr/miner_raw/worker.js
 3. SDK/xmr/miner_raw/cn.js

Where miner.js handles the server-client connection, worker.js are web workers calculating cryptonight hashes using cn.js -- a emscripten generated wrapped webassembly file. The webassembly file can also be compiled by you, see "hash_cn" below.

### Server

The C# server. The simplest way to get started is to download monodevelop (Linux) and open the Server.sln solution file. The solution should build server.exe out of the box, since no extern 3rd party libraries are needed.

```bash
mono server.exe
```

should run the server.  Additional information will follow soon.

### hash_cn

The monero/aeon cryptonight hashing functions in C code. With simple Makefiles for use with gcc and emcc -- the emscripten webassembly compiler. libhash should be compiled so that the server can check hashes calculated by the user.

# ToDo

Refactoring. Documentation.

# Contact

webminerpool@protonmail.com

# Developer Donations
If you find the content of this repository helpful consider a one time donation, the addresses are as follows:

-   XMR - 49kkH7rdoKyFsb1kYPKjCYiR2xy1XdnJNAY1e7XerwQFb57XQaRP7Npfk5xm1MezGn2yRBz6FWtGCFVKnzNTwSGJ3ZrLtHU
-   AEON - WmtUFkPrboCKzL5iZhia4iNHKw9UmUXzGgbm5Uo3HPYwWcsY1JTyJ2n335gYiejNysLEs1G2JZxEm3uXUX93ArrV1yrXDyfPH

