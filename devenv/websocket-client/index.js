import WebSocket from "ws";

const addr = process.argv.length > 2
	? process.argv[2]
    : "ws://localhost:5000/api/ws-dev";

const ws = new WebSocket(addr);

ws.on("open", () => {
	console.log("Connection opened.");
	ws.send(JSON.stringify({event: "subscribe", channel: "item/update{world=74}"}));
});

ws.on("close", () => {
	console.log("Connection closed.");
});

ws.on("message", data => {
    const message = JSON.parse(data);
    console.log(message);
});
