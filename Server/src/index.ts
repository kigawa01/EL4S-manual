import { createServer } from "node:http";
import { WebSocketServer, type WebSocket } from "ws";
import { parseClientMessage } from "./protocol.js";
import { RoomRegistry } from "./room.js";

const PORT = Number(process.env.PORT ?? 8080);

const rooms = new RoomRegistry();

const httpServer = createServer((req, res) => {
  if (req.method === "GET" && req.url === "/health") {
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({ status: "ok" }));
    return;
  }
  res.writeHead(404);
  res.end();
});

const wss = new WebSocketServer({ server: httpServer, path: "/ws" });

wss.on("connection", (ws: WebSocket) => {
  ws.on("message", (raw) => {
    const message = parseClientMessage(raw.toString());
    if (!message) {
      ws.send(JSON.stringify({ type: "error", message: "invalid message" }));
      return;
    }

    if (message.type === "match") {
      const peers = rooms.match(message.clientId, ws);
      ws.send(
        JSON.stringify({ type: "joined", clientId: message.clientId, peers }),
      );
    }
  });

  ws.on("close", () => {
    rooms.leave(ws);
  });
});

httpServer.listen(PORT, () => {
  console.log(`el4s-realtime listening on :${PORT}`);
});
