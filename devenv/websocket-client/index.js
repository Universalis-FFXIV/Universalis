import WebSocket from "ws";

const addr = process.argv.length > 2
	? process.argv[2]
    : "ws://localhost:5000/api/ws-dev";

const ws = new WebSocket(addr);

ws.on("open", () => {
	console.log("Connection opened.");
	ws.send(JSON.stringify({message: "Hello, world!"}));
});

const counts = new Map();
ws.on("message", data => {
    const message = JSON.parse(data);

    if (counts[message.event] == null) {
        counts[message.event] = 0;
    }

    counts[message.event]++;
    console.log(counts);
});
