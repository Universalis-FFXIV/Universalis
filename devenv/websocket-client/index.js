const WebSocket = require("ws");

const ws = new WebSocket("ws://localhost:5000/api/ws");

ws.on("open", () => {
	console.log("Connection opened.");
	ws.send(JSON.stringify({message: "Hello, world!"}));
});

ws.on("message", data => {
	console.log(`RECV: ${data}`);
});
