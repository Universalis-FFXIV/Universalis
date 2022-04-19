import WebSocket from "ws";

const addr = process.argv.length > 2
	? process.argv[2]
    : "ws://localhost:5000/api/ws-dev";

const ws = new WebSocket(addr);

ws.on("open", () => {
	console.log("Connection opened.");
	ws.send(JSON.stringify({message: "Hello, world!"}));
});

let counts = new Map();
ws.on("message", data => {
    const message = JSON.parse(data);

    if (!counts.has(message.event)) {
        counts.set(message.event, 0);
    }

    const currentCount = counts.get(message.event);
    if (message.event.startsWith("sales")) {
        counts.set(message.event, currentCount + message.sales.length);
    } else if (message.event.startsWith("listings")) {
        counts.set(message.event, currentCount + message.listings.length);
    }

    console.log(counts);
});
