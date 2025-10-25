# GDD – Duelcremental

![Logo](IsoCombat/Assets/Art/UI/Logo.png)

## Project Breakdown

**Title:** duelcremental  
**Genre:** Competitive Online Action Prototype  
**Players:** 1v1 (2-4 planned)  
**Platform:** PC (Unity)  
**Engine:** Unity 6000.0.50 f1  
**Version:** v0.1  
**Stage:** Networking prototype

### Concept Summary
duelcremental is a small-scale online prototype where players control simple avatars that move and collide to reduce each other's health while avoiding obstacles on the map.  
The core focus of this version is to establish a stable and scalable networking model using TCP + UDP, with minimal gameplay.

---

## Core Pillars

1. Responsiveness: low-latency multiplayer interactions.  
2. Simplicity: minimal visuals and inputs to isolate networking.  
3. Scalability: clean code and modular networking ready for future gameplay.

---

## Game Objectives

- Implement client-server communication (TCP + UDP).  
- Synchronize positions and rotations between players.  
- Create a functional lobby with connection and chat.  
- Prepare a structure ready to integrate future gameplay systems.

---

## Gameplay Overview

### Match Flow
1. Player connects to the lobby (via TCP).  
2. Enters or creates a room.  
3. Once both players are ready, the match starts.  
4. During gameplay, players move in real-time (UDP).  
5. Collisions and interactions will be implemented in later versions.  
6. After the session, the server disconnects all players and returns to the lobby.

### Current Build Limitations
- No damage or win condition.  
- No obstacle interaction.  
- Only basic movement synchronization.

---

## Controls

| Action | Input |
|--------|-------|
| Move   | WASD / Arrow Keys |

---

## Visual Style

- Minimal and functional.  
- Prototype UI and placeholder art.  
- Focus on clarity of movement and synchronization.

---

## Audio

- Not implemented in v0.1.  
- Future versions will include connection sounds, hits, and ambient background.

---

## Technical Design

### Networking Overview
- TCP handles lobby, chat, and reliable events.  
- UDP handles real-time gameplay data (position, rotation).  
- JSON is used for message serialization.

### Synchronization Model
- Each client sends its position and rotation periodically.  
- The server receives and broadcasts this data to all connected clients.  
- Clients interpolate received positions for smoother visuals.  
- No prediction or lag compensation yet.

### Server Architecture
- **TCP Server:** manages player connections, lobby state, and chat messages.  
- **UDP Server:** receives gameplay packets and forwards state updates to clients.  
- **Client Manager:** keeps track of connected clients, their IDs, and current state.  
- **Message Parser:** parses JSON messages into internal data structures and validates fields.  
- **State Broadcaster:** bundles and forwards state updates at the configured tick rate.  
- **Connection Handler:** handles new connections, disconnections, and basic timeouts.  
- **Logging Module:** writes server events and network messages to log files for debugging.

### How to Run

1. Install Unity 6000.0.50 f1.  
2. Clone the repository.  
3. Open the project in Unity Hub.  
4. Launch the **Lobby Scene**.  
5. Run one instance in the Editor (server) and build standalone clients, or run multiple Editor instances.  
6. Connect clients to localhost or the server IP.  
7. Use the chat and move to verify synchronization.

### Next Steps

- Add combat and health system.  
- Implement obstacles with collision and damage logic.  
- Improve client-side prediction and lag compensation.  
- Add server-side validation and basic anti-cheat measures.  
- Add UI indicators for ping and sync quality.

### Credits

- Aleix Botella  
- Guillem Montes  
- Eduard Garcia  
- JiaJie Lin  
- Martí Sabaté  
- Raül Sánchez
