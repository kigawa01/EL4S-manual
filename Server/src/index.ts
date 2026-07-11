import { createServer } from "node:http";
import { WebSocketServer, type WebSocket } from "ws";
import { parseClientMessage, type ServerToClientMessage } from "./protocol.js";
import { RoomRegistry } from "./room.js";

const PORT = Number(process.env.PORT ?? 8080);

const rooms = new RoomRegistry();

function send(ws: WebSocket, message: ServerToClientMessage): void {
  ws.send(JSON.stringify(message));
}

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

  function requireJoined(): boolean {
    if (!joined) {
      send(ws, { type: "error", message: "not joined" });
    }
    return joined;
  }

  ws.on("message", (raw) => {
    const message = parseClientMessage(raw.toString());
    if (!message) {
      send(ws, { type: "error", message: "invalid message" });
      return;
    }

    if (message.type === "match") {
      const result = rooms.match(message.clientId, ws);
      if (!result.ok) {
        send(ws, { type: "error", message: result.reason });
        return;
      }
      joined = true;
      send(ws, { type: "joined", clientId: message.clientId, peers: result.peers });
      return;
    }

    if (message.type === "alchemy-result") {
      if (!requireJoined()) return;
      rooms.broadcastAlchemyResult(ws, message.result);
      return;
    }

    if (message.type === "item-transfer") {
      if (!requireJoined()) return;
      rooms.broadcastItemTransfer(ws, message.item);
    }
  });

  ws.on("close", () => {
    rooms.leave(ws);
  });
});

httpServer.listen(PORT, () => {
  console.log(`el4s-realtime listening on :${PORT}`);
});
