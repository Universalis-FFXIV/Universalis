import WebSocket from "ws";

const addr = process.argv.length > 2
	? process.argv[2]
    : "ws://localhost:5000/api/ws-dev";

const ws = new WebSocket(addr);

ws.on("open", () => {
	console.log("Connection opened.");
	ws.send(JSON.stringify({message: "Hello, world!"}));
});

let ev = [];
ws.on("message", data => {
    ev.push(Date.now().valueOf());
    while (ev[ev.length - 1] - ev[0] > 1000) {
        ev.shift();
    }

	console.log(`RECV: ${data}`);
    console.log(`AGG: ${ev.length} ev/s`);
});
