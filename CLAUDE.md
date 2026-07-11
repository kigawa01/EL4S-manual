# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

EL4S-manual は Unity 6 (`6000.3.13f1`) の 2D ゲームクライアントと、それとは独立にデプロイされる
Node.js/TypeScript 製リアルタイム中継サーバー (`Server/`) の2つで構成されています。両者はネットワーク
(WebSocket) 経由でのみ疎結合しており、リポジトリ内にビルド依存関係はありません。

Unity 側は現時点ではシーンとレンダーパイプライン設定のみで、C# スクリプトはまだ1つも存在しません
(`Assets/` にはコードなし)。実装作業を行う際はこの前提を踏まえてください。

## コマンド

### リアルタイムサーバー (`Server/` ディレクトリ内で実行)

```
npm install          # 依存関係インストール
npm run dev           # tsx watch src/index.ts でホットリロード開発
npm run build          # tsc -p tsconfig.json で dist/ にビルド
npm start              # node dist/index.js でビルド済みサーバーを起動
```

lint・test の npm script は定義されていません。単体テストの仕組みは未整備です。

### Unity クライアント

CLI ビルドスクリプトはリポジトリに含まれていません。Unity Editor `6000.3.13f1` で
プロジェクトルートを直接開いて作業してください（バージョン不一致は大量の再インポートを招くため注意）。
`com.unity.test-framework` パッケージは依存関係に含まれていますが、テストコードはまだ存在しません。

## アーキテクチャ

### リアルタイム中継サーバー (`Server/src/`)

役割ごとに3ファイルのみの小さなサーバーです。

- `index.ts` — `http.createServer` で `/health` エンドポイントを提供しつつ、同じ HTTP サーバーに
  `path: "/ws"` で `ws.WebSocketServer` を重ねている。受信メッセージを `parseClientMessage` で検証し、
  `join` / `state` を `RoomRegistry` に委譲するだけの薄いハンドラ。
- `protocol.ts` — クライアント⇔サーバーのメッセージ型定義と `parseClientMessage` によるランタイム検証。
  サーバー→クライアントは `joined` / `peer-joined` / `peer-left` / `state` / `error` の5種類。
  ファイル冒頭のコメントに Unity 側で `Assets/Scripts/Realtime/RealtimeConnection.cs` としてこの型を
  ミラーする想定が書かれているが、**現状そのファイルは未実装**。
- `room.ts` — `RoomRegistry` が状態のすべて。`Map<roomId, Map<clientId, Member>>` と
  `WebSocket -> {roomId, clientId}` の逆引き Map をメモリ上に保持するのみで、永続化なし・単一プロセス前提。
  - `join()`: 同じ `clientId` で既存接続があれば古い方を close code `4000` で切断してから差し替える
    （再接続の横取り）。戻り値は自分以外の既存 peer 一覧。
  - `broadcastState()` / `broadcast()`: room 内の自分以外全員に fan-out。送信者を除外するのは呼び出し側で
    `excludeClientId` を渡す設計。
  - room が空になったら自動的に `rooms` から削除される。

水平スケール（複数プロセス/複数インスタンス）は現状サポートされていません（インメモリ状態のため）。

### デプロイ

- `.github/workflows/deploy-realtime-server.yml` — `main` への push で `Server/**` に変更があった場合のみ、
  外部リポジトリ `kigawa-net/kigawa-net-k8s` の再利用可能ワークフロー (`release.yml`) を呼び出し、
  `Server/Dockerfile` をビルドして Harbor (`private` プロジェクト) 経由で k8s にデプロイする
  (`el4s-realtime`, manifest: `el4s-realtime/deployment.yaml`)。Unity クライアント側の変更ではこのワークフローは
  発火しない。
- `Server/Dockerfile` — build ステージで `npm install` + `tsc`、runtime ステージで `npm install --omit=dev` の
  マルチステージビルド。非 root ユーザー (`app`) で実行し、`8080` を listen（`PORT` 環境変数で変更可）。

### Unity クライアント (`Assets/`)

- Universal Render Pipeline の 2D Renderer を使用 (`com.unity.render-pipelines.universal` 17.3.0)。
  設定は `Assets/Settings/UniversalRP.asset` / `Assets/Settings/Renderer2D.asset` /
  `Assets/UniversalRenderPipelineGlobalSettings.asset`。
- 入力は新 Input System (`com.unity.inputsystem`)。アクション定義は
  `Assets/InputSystem_Actions.inputactions`。
- シーンは `Assets/Scenes/Player1.unity`, `Assets/Scenes/Player2.unity` の2つのみで、いずれもまだ
  Main Camera 程度しか置かれていない空シーン。
