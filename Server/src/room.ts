import type { WebSocket } from "ws";
import type { ServerToClientMessage } from "./protocol.js";

interface Member {
  clientId: string;
  ws: WebSocket;
}

export class RoomRegistry {
  private rooms = new Map<string, Map<string, Member>>();
  private memberOf = new Map<WebSocket, { roomId: string; clientId: string }>();

  join(roomId: string, clientId: string, ws: WebSocket): string[] {
    let room = this.rooms.get(roomId);
    if (!room) {
      room = new Map();
      this.rooms.set(roomId, room);
    }

    const existing = room.get(clientId);
    if (existing && existing.ws !== ws) {
      existing.ws.close(4000, "replaced by new connection");
      this.memberOf.delete(existing.ws);
    }

    const peers = [...room.keys()].filter((id) => id !== clientId);

    room.set(clientId, { clientId, ws });
    this.memberOf.set(ws, { roomId, clientId });

    this.broadcast(roomId, clientId, { type: "peer-joined", clientId });

    return peers;
  }

  leave(ws: WebSocket): void {
    const info = this.memberOf.get(ws);
    if (!info) return;

    this.memberOf.delete(ws);

    const room = this.rooms.get(info.roomId);
    if (!room) return;

    const member = room.get(info.clientId);
    if (member && member.ws === ws) {
      room.delete(info.clientId);
      this.broadcast(info.roomId, info.clientId, {
        type: "peer-left",
        clientId: info.clientId,
      });
    }

    if (room.size === 0) {
      this.rooms.delete(info.roomId);
    }
  }

  broadcastState(ws: WebSocket, payload: unknown): void {
    const info = this.memberOf.get(ws);
    if (!info) return;

    this.broadcast(info.roomId, info.clientId, {
      type: "state",
      clientId: info.clientId,
      payload,
    });
  }

  private broadcast(roomId: string, excludeClientId: string, message: ServerToClientMessage): void {
    const room = this.rooms.get(roomId);
    if (!room) return;

    const data = JSON.stringify(message);
    for (const member of room.values()) {
      if (member.clientId === excludeClientId) continue;
      if (member.ws.readyState === member.ws.OPEN) {
        member.ws.send(data);
      }
    }
  }
}
