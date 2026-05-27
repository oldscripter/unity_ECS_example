# ⚔️ Unity ECS example

**RTS prototype on Unity ECS (DOTS)** with thousands of units that can be built and sent into battle with a single click.

![Unity Version](https://img.shields.io/badge/Unity-2022.3_LTS-blue)
![DOTS](https://img.shields.io/badge/DOTS-1.0+-green)
![License](https://img.shields.io/badge/License-MIT-yellow)

---

## 🎮 About the project

This is my experiment with **Entity Component System (ECS)** — an architecture that allows Unity to handle **10,000+ units** without losing performance.

The project is inspired by *Diplomacy is Not an Option* — an RTS where massive armies clash on the battlefield in real time.

**Goal:** Build a foundation for an RTS with large-scale unit control, learn DOTS, and understand how modern strategy games are made.

### 🎯 What's already implemented

| System | Description |
|--------|-------------|
| **Unit Movement** | Units smoothly move to the click point with configurable speed |
| **Rotation** | Each unit rotates (demonstrating animations working with ECS) |
| **Mass Spawning** | Create 100+ units in a neat grid with adjustable parameters |
| **Group Control** | One click — the entire army moves to the target point |
| **ECS Architecture** | All logic is built on components and systems for maximum performance |

### 🚧 Future plans

- [ ] Rectangle selection for units
- [ ] Health indicators
- [ ] Simple enemy AI
- [ ] Combat system
- [ ] Economy and resources
- [ ] Building construction
- [ ] Fog of war
- [ ] Flow Field Pathfinding for thousands of units

---

## 🛠️ Tech stack

| Technology | Version | Purpose |
|------------|---------|---------|
| **Unity** | 2022.3 LTS | Game engine |
| **Entities** | 1.0+ | ECS core |
| **Entities.Graphics** | 1.0+ | ECS entity rendering |
| **Burst** | 1.8+ | Performance compiler |
| **Collections** | 1.2+ | Native containers |
| **Input System** | 1.5+ | Modern input handling |

---

## 📁 Project structure

```
Assets/Scripts/
├── MoveToAuthoring.cs          # Movement component + authoring + baker
├── MoveSystem.cs               # Movement system (Burst)
├── RotationSpeedAuthoring.cs   # Rotation component + authoring + baker
├── RotationSystem.cs           # Rotation system (Burst)
├── PlayerInputSystem.cs        # Mouse click handling

Assets/Scripts/Spawn 100
├── SpawnerData.cs              # Spawner component data
├── SpawnerAuthoring.cs         # Spawner authoring + baker
└── SpawnSystem.cs              # Spawn system (Burst)
├── UnitTagAuthoring.cs         # Tag component for unit identification
```

---

## 🎮 How to play

| Action | Result |
|--------|--------|
| **Right mouse button** on the ground | All units move to the click point |

**In the current version:** Units are spawned automatically when the game starts via `SpawnSystem`.

---

## 🚀 Installation and setup

### Requirements
- Unity 2022.3 LTS or newer
- DOTS packages installed (see below)

### Setup steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/oldscripter/Last-Argument-ECS.git
   ```

2. **Open the project in Unity Hub**
   - Add the project
   - Make sure Unity version is 2022.3 LTS

3. **Install required packages** (if not installed automatically)
   - `Window > Package Manager`
   - Add by name:
     - `com.unity.entities`
     - `com.unity.entities.graphics`
     - `com.unity.burst`
     - `com.unity.collections`
     - `com.unity.inputsystem`

4. **Configure Input System**
   - `Edit > Project Settings > Player > Other Settings`
   - `Active Input Handling` → **Both**

5. **Run the scene**
   - Open `Assets/Scenes/MainScene.unity`
   - Press **Play**

---

## 🧠 How it works

### ECS in a nutshell

```
┌─────────────────────────────────────────────────────────────┐
│                      TRADITIONAL OOP                        │
│   GameObject → MonoBehaviour → Update() → scattered data    │
│                     ❌ slow, ❌ cache misses               │
└─────────────────────────────────────────────────────────────┘
                              ⬇
┌─────────────────────────────────────────────────────────────┐
│                         ECS (DOTS)                          │
│   Entity (ID) → Component (struct data) → System (logic)    │
│              ✅ contiguous data, ✅ multithreading         │
└─────────────────────────────────────────────────────────────┘
```

### Components (data only)

```csharp
public struct MoveTo : IComponentData
{
    public float3 TargetPosition;
    public float MoveSpeed;
    public bool IsMoving;
}
```

### Systems (logic only)

```csharp
[BurstCompile]
public partial struct MoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, moveTo) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<MoveTo>>())
        {
            // Movement logic here
        }
    }
}
```

---

## 📊 Performance

| Number of units | FPS (tested) |
|-----------------|--------------|
| 100 | 120+ |
| 500 | 90+ |
| 1000 | 60+ |
| 5000 | 30-40 |

*Tested on Intel i7-10700, RTX 2060*

---

## 🤝 How to contribute

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📝 License

Distributed under the **MIT** license. See `LICENSE` file for details.

---

## 🙏 Acknowledgments

- Inspired by *Diplomacy is Not an Option* from Door 407 studio
- [Unity DOTS Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html)
- CodeMonkey and the Unity community for excellent tutorials

---

## 📬 Contact

Author: **oldscripter**  
Telegram: [@oldscripter](https://t.me/oldscripter)
