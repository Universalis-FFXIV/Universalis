const WebSocket = require("ws");

const ws = new WebSocket("https://localhost:5001");

ws.on("open", () => {
	console.log("Connection opened.");
	ws.send(JSON.stringify({message: "Hello, world!"}));
});

ws.on("message", data => {
	console.log(`RECV: ${data}`);
});
