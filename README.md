# GDD – DuelCremental

![Logo](IsoCombat/Assets/Art/UI/Logo.png)

## Project Breakdown

**Title:** DuelCremental  
**Genre:** Competitive Online Action Prototype  
**Players:** 1v1 (2–4 supported)  
**Platform:** PC (Unity)  
**Engine:** Unity 6000.0.50 f1  
**Version:** v0.2  

### Concept Summary

DuelCremental is a small-scale online prototype where players control simple avatars that move, dash and shoot to reduce each other's health while avoiding hazards on the map.  
The core focus of this version is to establish a stable and scalable networking model using TCP + UDP, with minimal but functional gameplay.

---

## System Requirements

- OS: Windows 10/11 (other platforms not tested)
- CPU: Dual-core 2.0 GHz or better
- RAM: 4 GB
- Network: Stable internet connection or local network
- Additional: Two clients required to properly test online matches (up to four supported)

---

## Game Objectives

For this prototype (v0.2), DuelCremental aims to:

- Provide online 1v1 matches (with support for up to 4 connected clients).
- Implement client–server communication using **TCP + UDP**.
- Offer a functional lobby with:
  - Player connection and basic room flow.
  - Text chat between connected players.
- Synchronize player movement in real time:
  - Position and rotation updates over UDP.
  - Basic interpolation on clients for smoother visuals.
- Implement a complete combat loop:
  - Health, damage and death handling.
  - Bullets, spike balls and a time-based storm as hazards.
- Provide a round-based progression system:
  - Upgrade pool of ~35 possible bonuses.
  - Post-round selection from 3 random options.
  - Incremental builds across multiple rounds.
- Use a JSON-based message format for all network communication.
- Include an end-of-session ranking screen and summary.

---

## Gameplay Overview

### Match Flow

1. The player connects to the lobby (via TCP).  
2. They enter or create a room.  
3. Once both players are ready, the match starts.  
4. During gameplay, players move in real time while the server replicates their state over UDP.  
5. Collisions, damage and hazards (bullets, spike balls, storm) are resolved and synchronized by the server.  
6. When only one player remains alive, the game transitions to the skill selection scene.  
7. The players who lost choose an upgrade from 3 options drawn from a pool of around 35 possible upgrades.  
8. The next round starts with the new builds; after one player wins 3 rounds (configurable), the session ends.  
9. After the session, the server disconnects all players from the match and returns them to the lobby.

---

## Controls

| Action  | Input        |
|---------|-------------|
| Move    | Mouse (aim + forward move) |
| Shoot   | Right mouse button |
| Dash    | Left mouse button |
| Select  | Mouse (UI interaction) |

---

## Visual Style

- Minimal and functional.  
- Prototype UI and placeholder art.  
- Focus on clarity of movement, combat feedback and synchronization.

---

## Audio

- Not implemented in v0.2.  
- Future versions will include connection sounds, hits, and ambient background.

---

## Technical Design

### Networking Overview

- **TCP** handles lobby, chat, upgrades scene, ranking scene and other reliable events.  
- **UDP** handles real-time gameplay data (position, rotation, spawning, storm and hazards).  
- **JSON** is used for message serialization.

### Synchronization Model

- Each client sends its input and local state (position, rotation, actions) periodically.  
- The server receives and broadcasts authoritative state updates to all connected clients.  
- Clients interpolate received positions for smoother visuals.  
- No advanced client-side prediction or lag compensation is implemented yet.

### Replication Model

- The server is fully authoritative over the world state (players, bullets, spike balls, storm and other networked entities).  
- Clients send their input/state over UDP at a fixed rate.  
- On each server tick, the server:
  - Updates the world state.
  - Builds a snapshot of all relevant entities.
  - Broadcasts this snapshot to every connected client.
- Clients apply the replicated state and interpolate positions to smooth out movement.

### Server Architecture

- **TCP Server:** manages player connections, lobby state, chat messages and scene flow.  
- **UDP Server:** receives gameplay packets and forwards state updates to clients.  
- **Client Manager:** keeps track of connected clients, their IDs and current state.  
- **Message Parser:** parses JSON messages into internal data structures and validates fields.  
- **State Broadcaster:** bundles and forwards state updates at the configured tick rate.  
- **Connection Handler:** handles new connections, disconnections and basic timeouts.  
- **Logging Module:** writes server events and network messages to log files for debugging.

---

## Project Structure

- `IsoCombat/Assets/Scenes/`  
  Core game scenes (Lobby, Match, Upgrade/Ranking scenes).
- `IsoCombat/Assets/Scripts/Networking/`  
  TCP and UDP client/server implementations, replication manager and message serialization.
- `IsoCombat/Assets/Scripts/Game/`  
  Player controller, hazards, storm logic and match flow.
- `IsoCombat/Assets/Scripts/UI/`  
  Lobby UI, chat, upgrade selection and basic HUD.
- `IsoCombat/Assets/Art/`  
  Placeholder art, logo and UI elements.

---

## How to Run

1. Install Unity 6000.0.50 f1.  
2. Clone the repository.  
3. Open the project in Unity Hub.  
4. Launch the **Lobby Scene**.  
5. Run one instance in the Editor (server) and build standalone clients, or run multiple Editor instances.  
6. Connect clients to `localhost` or the desired server IP.  
7. Use the chat, move, shoot and dash to verify synchronization and combat.

---

## Next Steps

- Implement proper client-side prediction and lag compensation on top of the current replication.  
- Improve balancing of upgrades, damage values and storm pacing.  
- Add more obstacle types and hazard patterns.  
- Strengthen server-side validation and basic anti-cheat measures.  
- Add richer UI indicators for ping, sync quality and round progression.  
- Add audio feedback for connections, hits, dashes and storm events.

---

## Credits

- Aleix Botella  
- Guillem Montes  
- Eduard Garcia  
- JiaJie Lin  
- Martí Sabaté  
- Raül Sánchez
