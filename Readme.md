# The Observer: Sci-Fi Horror Architecture Prototype 👁️

[![Unity Version](https://img.shields.io/badge/Unity-6000.5.1f1-blue.svg)](https://unity.com/)
[![Language](https://img.shields.io/badge/Language-C%23-green.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Graphics](https://img.shields.io/badge/Graphics-URP-lightgrey.svg)]()

An experimental First-Person Sci-Fi Horror prototype built in Unity. This project serves as an R&D framework exploring advanced gameplay engineering principles, including **Dynamic Difficulty Adjustment (DDA)** via adaptive AI models, and **Procedural Environment Generation**.

> 🔬 **Project Context:** Originally conceptualized as a high-pressure Game Jam prototype, this repository has been adapted into a specialized computer science research framework focusing on Information Retrieval logic, dynamic text-processing architectures, and component-based game scalability.

---

## 📸 In-Engine Screenshots & Analytics

*System layout showcasing first-person environment integration and procedural navigation blueprints.*

| Main Menu UI | Dynamic Navigation |
| --- | --- |
| ![Main Menu](https://i.ibb.co/TB5GsPPG/The-Observer-Main-Menu.png) | ![Gameplay 1](https://i.ibb.co/JW7pvMxb/The-Observer-Gameplay2.png) |
| **Enemy AI** | **Procedural Maze Generation** |
| ![Gameplay 2](https://i.ibb.co/XZr2yncX/The-Observer-Gameplay3.png) | ![Maze](https://i.ibb.co/Rk11GTgB/The-Observer-Map.png) |

---

## 🧠 Technical Architecture & Deep Dive

### 🤖 Adaptive AI & Dynamic Difficulty Adjustment (DDA)
- **Performance Monitoring Model:** Implemented an intelligence system framework ("The Observer") designed to track real-time player telemetry and scale target difficulty parameters dynamically.
- **State Machine Separation:** Uses distinct C# architectures to handle isolated entity states, safely separating AI sensing logic from core player simulation routines.

### 📐 Procedural Generation & Level Routing
- **Procedural Maze Generation:** Assembles randomized, path-based modular room arrangements on scene load, optimizing asset instantiation pipelines.
- **ScriptableObject-Driven Design:** Leverages Unity `ScriptableObjects` to hold global asset configurations, dynamic room rules, and difficulty weight matrices, allowing seamless design tuning.

### 🎮 Gameplay Engineering & Component Flow
- **First-Person Physics Controller:** Built a clean modular framework for sprinting, crouching, vertical locomotion, and unified interaction triggers (elevators, hardware consoles).
- **Unity New Input System:** Decoupled multi-device mapping handling, allowing smooth abstraction between logical input events and actual execution layers.

---

## 🛠️ Technologies Used

- **Engine Ecosystem:** Unity 6000.5.1f1 (Universal Render Pipeline compatibility)[cite: 1]
- **Languages & Libraries:** C# Object-Oriented Framework, Unity Input System Package[cite: 1]
- **UI Architecture:** Unity UI / UGUI modular Canvas components[cite: 1]

---

## 📂 Project Structure

```text
Assets/Scripts/
├── AI/          # Core adaptive difficulty models, data weights, and agent behaviors
├── Player/      # Physics kinematics, raw input routers, camera controllers, and interaction raycasts
├── Rooms/       # Procedural generation engines, room controllers, and node progression matrices
├── Managers/    # Global Singletons driving persistent gameplay loops, audio routing, and states
└── UI/          # Dynamic text processing, HUD updates, and Canvas layout feedback systems
