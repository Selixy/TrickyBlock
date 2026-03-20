# Audit Complet Projet TrickyBlock
## État du projet: Multijoueur Tetris Split-Screen
**Date**: 2026-03-20 | **Version**: Proto Temps Réel

---

## 📊 Vue d'ensemble Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    TRICKBLOCK                           │
│  Tetris Multijoueur P1 vs P2 (Split-Screen)            │
└─────────────────────────────────────────────────────────┘
         │
    ┌────┴────┐
    ▼         ▼
  SCENES    GAMEPLAY
    │         │
    ├─ Core       ├─ PlayerControl (keypresses + OSC)
    ├─ Lobby      ├─ Pieces (pièces Tetris)
    ├─ Game       ├─ PieceCollisionListener
    ├─ MainPlayer ├─ GolRunUp
    │            ├─ SoftBones2D (animation)
    │            └─ CameraControleur
    │
    ├─ NETWORK
    │   ├─ OscSender (envoi UDP)
    │   ├─ OscReceiver (réception UDP)
    │   ├─ SceneSender (sérialise pièces → JSON)
    │   ├─ SceneReconstructor (reçoit JSON → objets)
    │   └─ VideoTcpSender/CrossVideoNetworkManager
    │       (webcam TCP + video routing)
    │
    └─ UI
        ├─ VideoDisplayUI (affiche webcams)
        ├─ SceneStreamCapture (capture scène)
        ├─ SceneReconstructor (capture scène recon.)
        ├─ ServerIPInput (config IP)
        ├─ LocalIPDisplay (affiche mon IP)
        ├─ Button (UI custom)
        ├─ Title
        ├─ PieceDebugVisual
        ├─ ButtonsEditor (éditeur)
        └─ VideoDisplayUI
```

---

## 🎮 Système Gameplay

### PlayerControl.cs
**Status**: ✅ Fonctionnel

**Responsabilités**:
- Spawn de pièces Tetris depuis Resources/Pieces
- Input System (clavier) + OSC (réseau)
- Mouvement: LEFT/RIGHT (avec repeat/cooldown)
- Dash (mouvement rapide)
- Rotation LEFT/RIGHT (snap rotation 90°)
- Fast fall (chute rapide avec multiplicateur)
- Drop (placement immédiat)
- Collision detection via raycast

**Paramètres configurables (Inspector)**:
- Fall Speed: 4 m/s
- Step Size: 0.5 (déplacement horizontal)
- Move Cooldown: 0.08s
- Dash Size: 1
- Fast Fall Multiplier: 2x

### Pieces.cs, PieceCollisionListener.cs
**Status**: ✅ Existants

---

## 📡 Système Réseau

### OscSender.cs
**Status**: ✅ Implémenté

Envoi messages OSC via UDP vers serveur

### OscReceiver.cs
**Status**: ✅ Complet

- Écoute UDP ports 9005/9006 (P1/P2)
- Parse OSC packets (float + string JSON)
- Events: OnMoveLeft, OnMoveRight, OnRotateLeft, OnRotateRight, OnDashLeft, OnDashRight, OnFastFallStart, OnFastFallStop, OnDrop, OnSceneDataReceived
- Getters: GetServerIP(), GetLastSenderIP(), GetLastSenderPort()

### SceneSender.cs
**Status**: ✅ Implémenté - 30 fps

- Scrape scene 30 fps
- Sérialise: Pieces + Camera
- Envoie: /scene (JSON) via OscSender

**Format JSON**:
```json
{
  "objects": [
    {
      "name": "Pieces_I_0",
      "position": { "x": 0.5, "y": 10.2, "z": 0 },
      "rotation": { "x": 0, "y": 0, "z": 90 },
      "scale": { "x": 1, "y": 1, "z": 1 }
    }
  ]
}
```

### SceneReconstructor.cs
**Status**: ✅ Implémenté - Split-Screen Prêt

- Écoute OscReceiver.OnSceneDataReceived
- Parse JSON → Create/Update GameObjects
- Supprime Rigidbody (physique local seulement)
- Capture sous RenderTexture
- Affiche sur RawImage (avec crop automatique)

### VideoTcpSender.cs (CrossVideoNetworkManager)
**Status**: ✅ Fonctionnel

- Singleton
- 2 connexions TCP par client (send + receive)
- Webcam capture → JPEG → TCP
- Events: OnPlayer1VideoReceived, OnPlayer2VideoReceived, OnMyVideoCapture
- Reconnection automatique (3s retry)

---

## 🎨 Système UI

### VideoDisplayUI.cs
**Status**: ✅ Fonctionnel

Affiche vidéos webcam (P1, P2, ou debug)

### SceneStreamCapture.cs
**Status**: ✅ Fonctionnel

Charge scène additive + capture en texture

### ServerIPInput.cs
**Status**: ✅ Fonctionnel

InputField TMP pour configurer IP serveur

---

## ✅ Ce qui fonctionne

| Système | Status |
|---------|--------|
| **Gameplay local** | ✅ Pièces tombent, contrôle OK |
| **Keyboard input** | ✅ InputSystem configuré |
| **OSC input** | ✅ Commandes reçues via réseau |
| **Webcam capture** | ✅ TCP streaming 30fps |
| **Scene reconst.** | ✅ JSON → objets vides affichés |
| **Video display** | ✅ RawImage + crop automatique |
| **Split-screen UI** | ✅ Layout prêt pour 2 vidéos |
| **IP configuration** | ✅ ServerIPInput → OscReceiver |

---

## ❌ Ce qui MANQUE (CRITIQUE)

### 1. **Serveur Rust**
- Aucun serveur n'existe
- Clients en attente de réception

### 2. **Physique synchronisée**
- Chaque client calcule sa propre physique
- Pas de validation serveur

### 3. **Gestion des matchs**
- Pas de concept de "match"
- Pas de score/winner detection

### 4. **Audio**
- ❌ Aucun système audio

---

## 📋 Résumé État

| Catégorie | % | Status |
|-----------|---|--------|
| **Gameplay** | 85% | Tetris local fonctionne bien |
| **Network** | 50% | Infrastructure OK, serveur manque |
| **UI** | 70% | Layout prêt, config OK |
| **Multijoueur** | 30% | Sync vidéo prête, gameplay non testé |
| **Production** | 10% | Aucun polish/UX |

---

## 🚀 Roadmap vers Viabilité

### Phase 1: Serveur Minimum (CRITIQUE)
- [ ] Serveur Rust TCP (video routing)
- [ ] Serveur OSC (scene routing)
- [ ] Test P1 + P2 sur LAN

### Phase 2: Gameplay Stable
- [ ] Debug et fix bugs locaux
- [ ] Physics deterministic

### Phase 3: Polish
- [ ] Audio
- [ ] Animations
- [ ] Visual effects

### Phase 4: Production
- [ ] Matchmaking
- [ ] Stats/leaderboard
