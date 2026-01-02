# The Observer - Game Design Document (GDD)

## 1. Overview

### 1.1 Title
**The Observer** - A sci-fi psychological horror roguelike with adaptive AI

### 1.2 Genre
Sci-fi Psychological Horror Roguelike with linear progression and adaptive challenges

### 1.3 Core Loop
**Lab Hub** (awaken) → **Elevator** (progressive floors) → **Floor 1: Procedural Maze** → **Return to Elevator** → **Floor 2: Horror** → **Return to Elevator** → **Floor 3: Adaptive Boss** → **Escape

### 1.4 Win Condition
Defeat adaptive boss → Escape cinematic with score-based endings

### 1.5 Replay Value
- Multiple endings based on playstyle
- Achievements system
- Endless mode stub
- Personalized boss variants based on player behavior

## 2. Narrative & Atmosphere

### 2.1 Setting
Player awakens as "Subject" in a sterile Lab Hub. The Observer (cold, adaptive AI Facility voice) narrates: "Welcome. I observe. Survive three trials. I learn from your failures."

### 2.2 Room Progression
- **Lab Hub**: Initial awakening area with elevator access
- **Elevator**: Progressive floor access (unlocks after completing previous floor)
- **Floor 1 (Maze)**: Procedural maze with combat (precision/speed) - gun enabled
- **Floor 2 (Horror)**: Dark house layout, stealth (find teddy bear) - no jump/crouch
- **Floor 3 (Boss)**: Combat arena with personalized hybrid enemy

### 2.3 AI Voice Feedback
AI voice (10+ clips) triggers on adaptations: "Predictable. Adapting." "Your mistakes teach me." "Pattern detected. Adjusting."

## 3. Runtime AI System: "Observer Model" (SBU AI Jam Compliant)

### 3.1 Core Concept
ScriptableObject AIModel with runtime weights that adapt from real player performance data, with measurable impact on gameplay.

### 3.2 Data Sources
- `deaths_count` (maze failures)  
- `detections_count` (horror room detections)
- `solve_time_normalized` (time to complete rooms)

### 3.3 AI Decisions
- **Maze Generation**: Complexity → denser mazes, more enemies, dead-ends
- **Horror Difficulty**: Girl speed, detection range, jumpscare frequency  
- **Boss Phases**: Personalized attack patterns based on player style

### 3.4 Update System
- Event-driven updates (on death, detection, completion)
- Periodic updates (every 10 seconds)
- Post-death adaptation

### 3.5 Persistence
PlayerPrefs system saves AI weights between sessions

### 3.6 Debug System
F1 key toggles overlay showing current AI weights and complexity

### 3.7 Impact Verification
Remove AI → static prefab maze/basic girl/fixed boss (gameplay degrades significantly)

## 4. Rooms & Mechanics

### 4.1 Lab Hub (Starting Area)
- Player awakens in sterile lab environment
- Initial briefing from The Observer
- Elevator at far end opens after initial awakening
- Simple interaction to enter elevator

### 4.2 Elevator (Progressive Access)
- Floor buttons only show available floors
- Current floor button is highlighted
- "Enter Next Trial" interact prompt (not floor selection)
- After failure: returns to current floor's entrance
- After success: progresses to next floor
- No ability to return to previous floors
- AI voice feedback between floors

### 4.3 Floor 1: Procedural Maze (90-120s timer)
- Square room layout generated procedurally
- Gun, crouch, and sprint enabled
- Find exit amid traps and enemies
- Death/timeout → AI regenerates new maze (harder: more walls/dead-ends/enemies)
- Exit leads to elevator return point
- AI adapts maze complexity based on performance

### 4.4 Floor 2: Horror (120s, dark house layout)
- Flashlight toggle (Interact key)
- Jump/crouch disabled
- Find teddy bear objective
- Ghost girl patrol with raycast detection → jumpscare
- Sprint noise → mimic voice echo
- Exit leads to elevator return point
- AI adapts girl speed and detection range

### 4.5 Floor 3: Adaptive Boss Arena (150s)
- Gun and jump enabled
- Boss variants/phases driven by total AI weights
- Mid-fight hack console for stun
- Exit leads to escape sequence
- Personalized attacks based on player behavior patterns



## 5. 4-Day Implementation Roadmap (Solo Developer)

### Day 1: Lab Hub, Elevator System, AI Model, Maze Generation
- [ ] URP pipeline with post-processing (vignette, bloom, color grading)
- [ ] Complete AI Model with real-time adaptation
- [ ] Procedural maze generator (recursive backtracker algorithm)
- [ ] Lab Hub with initial awakening sequence
- [ ] Elevator with progressive floor access
- [ ] Maze room controller with AI adaptation
- [ ] Complete loop: Lab → Elevator → Floor 1 (Maze) → Return to Elevator

### Day 2: Horror Floor Implementation
- [ ] Horror room layout with dynamic lighting
- [ ] Flashlight toggle system
- [ ] Ghost girl AI with patrol/detection
- [ ] Teddy bear collection objective
- [ ] Sprint noise system and mimic voice
- [ ] Jumpscare implementation
- [ ] Complete loop: Lab → Elevator → Floor 1 (Maze) → Return to Elevator → Floor 2 (Horror) → Return to Elevator

### Day 3: Boss Arena & Combat
- [ ] Boss arena layout
- [ ] Combat system (gun raycast)
- [ ] Multiple boss variants based on AI weights
- [ ] Hack terminal mini-game
- [ ] Win condition and escape sequence
- [ ] Complete loop: Lab → Elevator → Floor 1 (Maze) → Return to Elevator → Floor 2 (Horror) → Return to Elevator → Floor 3 (Boss) → Escape

### Day 4: Polish & Final Build
- [ ] AI voice clips integration
- [ ] Post-processing effects (Cinemachine shake, particles)
- [ ] UI timer/score/achievements system
- [ ] Balance playtesting
- [ ] Endless mode stub
- [ ] WebGL/PC builds
- [ ] Submission preparation (README with AI explanation)

## 6. Technical Requirements

### 6.1 Architecture
- Singletons: GameManager, RoomManager
- ScriptableObject: AIModel for persistence
- Event-driven interactions via GameInputManager
- Clean separation of room controllers

### 6.2 Performance Targets
- 60+ FPS target
- URP with baked lighting and occlusion culling
- <2k polys per room
- Optimized maze generation algorithm

### 6.3 Input System
- Complete GameInputManager with Move/Look/Jump/Crouch/Sprint/Shoot/Interact events
- Public accessors for all input states
- PlayerInteractor for raycast-based interactions

## 7. Asset Requirements

### 7.1 Mandatory Free Assets
- Free Sci-Fi Office Pack (Terresquall) - crates/doors/consoles/elevator
- Free Lowpoly Scifi Objects (Black Rose) - traps/plates
- Sci-Fi Effects (FORGE3D particles) - melt/lasers/explosions  
- Free Horror FPS Kit - girl model, teddy, flashlight VFX, jumpscare SFX

### 7.2 Visual Style
- Clean, performant visuals using URP
- Baked lighting and static batching
- Sci-fi horror aesthetic
- Particle effects for feedback

## 8. SBU AI Game Jam Compliance

### 8.1 Runtime AI System
- ✅ Data-driven in-game AI system
- ✅ Player modeling from real metrics (deaths, detections, solve time)
- ✅ Adaptive enemy behavior with measurable impact
- ✅ Explainable in under 2 minutes: "Runtime lerp-based adaptation from player performance data → dynamic maze generation, enemy behaviors, and personalized boss phases"

### 8.2 Removability Test
- ✅ If AI system removed → static prefab maze, basic girl, fixed boss
- ✅ Gameplay degrades to static difficulty, proving AI impact