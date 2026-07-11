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
  let joined = false;

  ws.on("message", (raw) => {
    const message = parseClientMessage(raw.toString());
    if (!message) {
      ws.send(JSON.stringify({ type: "error", message: "invalid message" }));
      return;
    }

    if (message.type === "match") {
      const peers = rooms.match(message.clientId, ws);
      joined = true;
      ws.send(
        JSON.stringify({ type: "joined", clientId: message.clientId, peers }),
      );
      return;
    }

    if (message.type === "state") {
      if (!joined) {
        ws.send(JSON.stringify({ type: "error", message: "not joined" }));
        return;
      }
      rooms.broadcastState(ws, message.payload);
    }
  });

  ws.on("close", () => {
    rooms.leave(ws);
  });
});

httpServer.listen(PORT, () => {
  console.log(`el4s-realtime listening on :${PORT}`);
});
