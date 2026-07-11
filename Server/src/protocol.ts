// WebSocket message protocol shared conceptually with the Unity client
// (Assets/Scripts/Realtime/RealtimeConnection.cs mirrors these shapes as JSON).

export interface MatchMessage {
  type: "match";
  clientId: string;
}

export interface JoinedMessage {
  type: "joined";
  clientId: string;
  peers: string[];
}

export interface PeerJoinedMessage {
  type: "peer-joined";
  clientId: string;
}

export interface PeerLeftMessage {
  type: "peer-left";
  clientId: string;
}

export interface ErrorMessage {
  type: "error";
  message: string;
}

export type ClientToServerMessage = MatchMessage;

export type ServerToClientMessage =
  | JoinedMessage
  | PeerJoinedMessage
  | PeerLeftMessage
  | ErrorMessage;

export function parseClientMessage(raw: string): ClientToServerMessage | null {
  let data: unknown;
  try {
    data = JSON.parse(raw);
  } catch {
    return null;
  }

  if (typeof data !== "object" || data === null || !("type" in data)) {
    return null;
  }

  const msg = data as Record<string, unknown>;

  switch (msg.type) {
    case "match":
      if (typeof msg.clientId === "string") {
        return { type: "match", clientId: msg.clientId };
      }
      return null;
    default:
      return null;
  }
}
