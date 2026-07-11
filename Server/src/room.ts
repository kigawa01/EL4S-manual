import { randomUUID } from "node:crypto";
import type { WebSocket } from "ws";
import type { ServerToClientMessage } from "./protocol.js";

interface Member {
  clientId: string;
  ws: WebSocket;
}

export class RoomRegistry {
  private rooms = new Map<string, Map<string, Member>>();
  private memberOf = new Map<WebSocket, { roomId: string; clientId: string }>();
  private waiting: Member | null = null;

  // Pairs the caller with whoever is waiting into a freshly-generated room,
  // or parks them as the waiting party until the next caller arrives. Reuses
  // join()'s existing peer-joined broadcast to notify the waiting party once
  // a match is made, so callers just send back whatever peers this returns.
  match(clientId: string, ws: WebSocket): string[] {
    if (this.waiting && this.waiting.ws.readyState === this.waiting.ws.OPEN) {
      const partner = this.waiting;
      this.waiting = null;

      const roomId = `auto-${randomUUID()}`;
      this.join(roomId, partner.clientId, partner.ws);
      return this.join(roomId, clientId, ws);
    }

    this.waiting = { clientId, ws };
    return [];
  }

  private join(roomId: string, clientId: string, ws: WebSocket): string[] {
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
    if (this.waiting?.ws === ws) {
      this.waiting = null;
    }

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
