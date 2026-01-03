1. High Concept
Title: The Observer
Genre: Sci‑fi psychological horror roguelike, first‑person, room‑based trials
Platform: PC (Windows, WebGL optional)
Core Pillars:

Tight, readable FPS controls with clear movement states (walk, sprint, crouch, jump).

Elevator‑driven progression through three distinct, AI‑adapted trials.

In‑game AI “Observer” that models player performance and dynamically adjusts difficulty across the run.
​

2. Core Loop & Flow
High‑level loop:

Wake up in Lab Hub → Briefing by The Observer → Enter elevator.

Play Floor 1 (Procedural Maze) → success or death.

Play Floor 2 (Horror House) → success or death.

Play Floor 3 (Boss Arena) → success or death → Escape sequence.
​

Failure loop (per floor):

Player dies or times out in a floor.

Screen fades to black; The Observer delivers a contextual line referencing mistakes and performance.

AIModel updates difficulty metrics using room performance data.

Player respawns inside that floor’s elevator, doors open, room is idle.

Room only “starts” (timer, enemies, hazards) when the player walks out of the elevator trigger again.
​
​

3. Game States & Structure
Global Game States (conceptual):

LabHub: Intro, first awakening and initial elevator access.

Elevator_Floor1 / Elevator_Floor2 / Elevator_Floor3: Player is inside that floor’s elevator, doors open, no timers running.

Floor1_Maze / Floor2_Horror / Floor3_Boss: Active trial; timers, enemies, hazards running.

EscapeSequence: Post‑boss escape and ending.
​

Scene Strategy:

LabHub scene: Lab room + initial elevator.

Floor1 scene: Maze layout, entry elevator, exit elevator, MazeRoomController, MazeGenerator.

Floor2 scene: Horror house layout, elevator, HorrorRoomController, ghost girl, teddy bear.

Floor3 scene: Boss arena, elevator, BossRoomController, BossController, hack console, exit.

GameManager is a persistent singleton (DontDestroyOnLoad) coordinating AIModel, overall run progress, deaths, and room completion counts.
​

4. Controls & Input
Input Layer:

Implemented via GameInputManager (Unity Input System, PlayerInputActions).

Provides: Move (WASD), Look (mouse), Jump, Sprint, Crouch (toggle), Interact (E), Shoot (LMB).

Exposes:

State accessors: GetMoveVector, GetLookVector, GetSprintInput, GetCrouchInput, IsJumpPressed, IsJumpHeld.

Events: OnJump, OnJumpCanceled, OnInteract, OnShoot, OnSprintStart, OnSprintEnd, OnCrouchStart, OnCrouchEnd.
​

Player Movement:

PlayerController (CharacterController‑based):

Walk / sprint / crouch speeds with acceleration & deceleration smoothing.

Jump system with coyote time, jump buffering, air jumps, and jump‑cut (short hops by releasing jump).

Air control blends current horizontal velocity towards input vector.

Gravity with separate multipliers for normal fall, fast fall, and low jump.

Crouch as a toggle with smooth height transition, ceiling check (OverlapCapsule), and head‑bob reduction.
​

Camera & Feel:

FPSCameraController:

Smoothed look input, clamped vertical rotation.

Dynamic FOV (base / sprint / future aim) and lerp speed.

Head bob with frequency, amplitude, curve, and multiplier.

Camera sway from mouse delta and optional Perlin noise‑based shake for jumps and feedback.
​

5. Interaction & Weapons
Interaction:

PlayerInteractor:

Forward raycast from FPS camera up to interactionDistance, with LayerMask filtering.

Highlights target via MaterialPropertyBlock emission.

On GameInputManager.OnInteract → calls Interact(GameObject interactor) on current Interactable.

Interactable (abstract):

prompt text, range, LayerMask.

Subclasses:

ElevatorConsole (enter next trial, floor transitions).

LabConsole (start run, maybe UI hints).

HackConsole (Boss stun).

TeddyBear, doors, other room‑specific objects.
​

Weapon:

WeaponController:

Subscribed to GameInputManager.OnShoot.

Raycast from FPS camera with configurable range and ammo.

Muzzle flash particle system and debug ray.

Future: records hit/miss for Boss/Maze accuracy metrics feeding AIModel.
​
​

6. Levels & Trials
6.1 Lab Hub (Starting Area)
Visual: Sterile sci‑fi laboratory, minimal props, cold lighting.

Flow:

Player wakes at a central spot; controls available.

The Observer (AI voice) delivers an initial briefing.

At briefing end, the far‑end elevator doors open.

Player approaches and sees “Enter Next Trial” interact prompt (no floor selection).
​
​

6.2 Elevator Behavior (All Floors)
Function: Single “hub” piece per floor, including LabHub.

Behavior:

When a floor loads, player spawns inside its elevator; doors open; room idle.

When player exits the elevator trigger:

Elevator doors close.

Corresponding RoomController.StartRoom() is called.

On success: player reaches exit condition (exit elevator in Maze, teddy bear + elevator in Horror, boss kill + exit in Boss).

On death/timeout:

Room stops; DeathFlowController triggers black screen and Observer message.

AIModel updates difficulty.

Player is respawned inside the same floor’s elevator; doors open again.
​
​

No backtracking: Once player leaves an elevator into a floor, doors close; returning to previous floors is not allowed.

6.3 Floor 1 – Procedural Maze
Duration target: 90–120 seconds per attempt.

Mechanics:

Movement: gun, crouch, sprint enabled; full FPS mobility.

Objective: navigate a procedurally generated maze from entry elevator (A) to exit elevator (B).

Hazards: sparse enemies and traps along the main path; optional ambient props.

Failure: death or timer expiry → death screen + AI update → respawn in entry elevator.

Success: reach the exit elevator, interact, transition to Floor 2.
​

Implementation:

MazeGenerator on a central object in Floor1 scene.

Uses grid (width × height, cellSize) with MazeCell data and a combination of:

Greedy backbone path from A to B.

Prim’s algorithm to fill remaining cells, ensuring a perfect maze.

Entrance/exit wall cuts based on border cells closest to A/B.

Wall instantiation around each cell, props placement, BFS to compute main path, and hazard spawning along that main path using density settings and spacing constraints.
​

Enemies/traps counts and densities are driven by AIModel complexity (see AI section).

6.4 Floor 2 – Horror House
Duration target: ~120 seconds.

Mechanics:

Movement: flashlight toggle on interact; jump and crouch disabled; sprint allowed but noisy.

Objective: navigate a dark, claustrophobic house layout to find a teddy bear, then bring it back to the elevator.

Ghost Girl: patrolling enemy with raycast‑based line‑of‑sight detection that triggers jumpscare.

Audio: sprinting generates noise events that can be used by mimic voice echoes, reinforcing tension.

Elevator doors: closed once player leaves; reopen only after teddy bear is collected; then allow exit.

Failure: caught by ghost or time runs out → death screen + AI update → respawn inside the same floor elevator.

Success: teddy returned to elevator → transition to Floor 3.
​

6.5 Floor 3 – Adaptive Boss Arena
Duration target: up to 150 seconds.

Mechanics:

Movement: gun and jump enabled; crouch optional.

Boss: multi‑phase enemy whose stats and patterns are derived from AIModel’s aggregated view of player performance on Maze and Horror floors.

Hack Console: temporary stun; implemented as an Interactable in the arena, enabling short windows for damage.

Objective: defeat boss and reach the exit to trigger the escape sequence.

Failure: death or timeout → death screen + AI update → respawn in elevator, re‑enter with updated boss difficulty.

Success: victory + exit → EscapeSequence and game end.
​
​

7. In‑Game AI System (AIObserverModel)
7.1 Role and Constraints
AIModel (ScriptableObject) acts as the core Observer: a lightweight player‑modeling and dynamic difficulty adjustment (DDA) system.

Requirements satisfied (per jam rules):

Uses real runtime data: solve time, mistakes, detections, failures, successes.

Produces numeric decisions (complexity scalar, per‑room difficulty parameters).

Directly influences level generation, enemy behavior, and boss parameters.

Removing it forces static difficulty values and degrades the experience.
​

7.2 Data Tracked
In AIModel:

Base & bounds: baseComplexity, minComplexity, maxComplexity.

Performance weights: solveTimeWeight, mistakesWeight, detectionWeight.

Targets: targetSolveTime, targetMistakes, targetDetections.

Runtime state:

currentComplexity – scalar used by rooms.
​

totalRoomsCompleted, totalFailures.

lastRoomSolveTime, lastRoomMistakes, lastRoomDetections.
​

Persisted via PlayerPrefs so the AI “remembers” across runs.
​

7.3 Update Logic
On room end, RoomController calls:

csharp
aiModel.RegisterRoomResult(
    success: bool,
    solveTimeSeconds: float,
    mistakes: int,
    detections: int
);
RegisterRoomResult:

Stores the last metrics.

Increments totalRoomsCompleted or totalFailures.

Computes a normalized performance score based on ratios of actual vs. target solve time, mistakes, and detections, using configured weights; failures are down‑weighted.

Converts performance into a targetComplexity and smoothly lerps currentComplexity toward it with adaptationLerpSpeed, clamped between minComplexity and maxComplexity.

Saves updated state back to PlayerPrefs.
​

7.4 How Rooms Use AI Output
Each Room uses AIModel.CurrentComplexity in its own mapping function:

Maze:

Maps complexity to width/height (optional), enemyDensityOnPath, trapDensityOnPath, propSpawnChance, and possibly maze length/branchiness.

MazeRoomController.StartRoom() calls a ApplyDifficulty(float complexity) on MazeGenerator before generating.

Horror:

Maps complexity to ghost speed, detection range, reaction time, and frequency of mimic voice echoes.

HorrorRoomController.StartRoom() applies these to NavMeshAgent speed, vision cone, and audio triggers.

Boss:

Computes BossDifficultyParams from aggregated Maze and Horror performance (e.g., skill scores per floor).

Adjusts boss HP multipliers, movement speed, projectile speed, phase count, and aggression.
​
​

7.5 Observer Messaging & Death Flow
DeathFlowController:

On Fail, receives RoomType and metrics from the RoomController.

Calls AIModel.RegisterRoomResult(success:false, ...) to update complexity.

Requests a contextual death message from an Observer message factory (e.g., referencing dead‑ends in Maze, detection count in Horror).

Plays a black‑screen fade in/out and shows the Observer line, then respawns player into the appropriate elevator with updated difficulty.
​
​

This ensures clear cause → effect: play, fail or succeed, see the Observer respond, then re‑enter a visibly changed challenge.
​

8. Technical Architecture
8.1 Core Systems
GameManager (Singleton):

Holds AIModel.

Tracks rooms completed, deaths, run score.

Provides methods: RegisterRoomCompleted, RegisterDeath, ResetRun.

AIModel (ScriptableObject):

Central AI Observer logic and persistence.

DebugAIOverlay:

On F1 toggle, displays AIModel’s current complexity, total rooms completed, failures, and last room metrics for debugging and judge demonstration.
​

8.2 Room & State Interfaces (Conceptual)
To keep code event‑driven and testable:

IRoom:

void Initialize(AIModel aiModel);

void StartRoom();

void OnPlayerEnter();

void OnPlayerExit();

bool CanExit();

RoomMetrics GetMetrics();

IRoomConstraints:

bool AllowJump { get; }

bool AllowCrouch { get; }

bool AllowWeapon { get; }

PlayerController reads from IRoomConstraints to decide if jump/crouch/weapon input should be honored, allowing Horror to disable jump/crouch cleanly.
​

8.3 Event Flow (Examples)
Input → Player: GameInputManager broadcasts events, PlayerController/WeaponController/PlayerInteractor subscribe, decoupling input from logic.

Elevator → GameManager/Rooms: ElevatorConsole Interactable fires events or calls into GameManager to change state and load floors.

Rooms → AI: RoomControllers invoke AIModel.RegisterRoomResult and DeathFlowController on success/fail.

9. UI & Feedback
In‑world prompts: simple “Press [E] to Interact” style prompts per Interactable.

HUD: timer, simple ammo count, floor name.

Debug: F1 overlay for AI, optional log messages for transitions and state changes.

Audio:

The Observer: synthetic, cold voice on intro, between floors, and on death messages.

Maze: low ambient drones, occasional mechanical sounds.

Horror: reverb, whispers, mimic voice echo when sprinting.

Boss: tense combat track with intensifying phases.
​

10. Scope & Jam Readiness
Minimal, well‑bounded: three trials, one core AI system, one main character controller.

AI system is simple, explainable, and clearly in‑game (no external LLM at runtime).

Failure/respawn loop is unified across floors, reusing the same DeathFlow and AIModel integration.

Debug overlay and strong separation of concerns (Input, Player, Camera, AI, Rooms) make it easy to debug and present to judges.
​