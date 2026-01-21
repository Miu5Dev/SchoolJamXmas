# Mario Odyssey-Style Movement System for Unity

A comprehensive, modular movement system inspired by Super Mario Odyssey and Super Mario 64.

## Changes in This Version

### Fixes Applied
1. **No run button** - Speed now increases automatically while moving (from minSpeed to maxSpeed over accelerationTime)
2. **Camera stops when analog released** - Fixed input system to properly reset look input to zero
3. **Triple jump window increased** - Default is now 1.0 seconds (configurable)
4. **CharacterController center.y** - Now configurable via `colliderCenterY` parameter (default 0)
5. **Debug gizmos** - All systems now have toggleable gizmos per category

## Features

### Core Movement
- **Auto Acceleration**: Speed increases from minSpeed to maxSpeed over time while moving
- **Crouch**: Standard crouch with reduced speed and height
- **Crouch Slide**: Press crouch while moving to slide with momentum
- **Slope Sliding**: Automatic sliding on steep slopes with lateral control

### Jump System
- **Normal Jump**: Basic jump
- **Double Jump**: Second jump in sequence (requires minimal speed, generous time window)
- **Triple Jump**: Third jump for maximum height
- **Long Jump**: Press jump during crouch slide for horizontal distance
- **Backflip**: Press jump while crouching stationary, pressing back, or after quick turn
- **Slope Jump**: Maintains slope momentum when jumping on steep slopes
- **Ground Pound Jump**: High jump after landing a ground pound

### Special Moves
- **Ground Pound**: Press crouch in air to freeze and slam down
- **Dive**: Press jump during ground pound freeze to dive forward
- **Ledge Grab**: Automatically grab ledges while falling
- **Ledge Shimmy**: Move left/right while grabbing a ledge (follows ledge curves)
- **Ledge Climb**: Press up to climb onto ledge
- **Ledge Jump**: Press jump to wall jump off ledge

### Momentum System
- Centralized momentum handling for fluid movement
- Momentum fighting: Moving against momentum gradually reduces it
- Momentum transfer between states (landing, jumping, sliding)
- Configurable decay rates based on movement direction

## Files Overview

| File | Description |
|------|-------------|
| `PlayerController.cs` | Main controller that coordinates all systems |
| `PlayerGroundDetection.cs` | Ground detection, slope detection, ground stick force |
| `MomentumSystem.cs` | Centralized momentum handling |
| `EventBus.cs` | Event system for decoupled communication |
| `EventStructs.cs` | All event definitions |
| `InputSystem.cs` | Input handling with controller support |
| `OdysseyCamera.cs` | Third-person camera with controller support |

## Setup Guide

### 1. Add Required Components to Player

Add these components to your player GameObject:
- `CharacterController`
- `PlayerController`
- `PlayerGroundDetection`
- `MomentumSystem`

### 2. Configure CharacterController Center

If your model needs `center.y = 0`, set the `Collider Center Y` parameter in PlayerController to `0`.

### 3. Configure Input System

Required actions in your Input Actions asset:
- **Move** (Vector2): WASD / Left Stick
- **Look** (Vector2): Mouse Delta / Right Stick
- **Jump** (Button): Space / South Button
- **Crouch** (Button): C/Ctrl / East Button
- **Interact** (Button): E / North Button

### 4. Setup Camera

1. Add `OdysseyCamera` to your camera
2. Assign the player transform as the target
3. Configure distance and sensitivity settings

## Configuration Reference

### PlayerController Settings

#### Movement (Auto Acceleration)
| Setting | Default | Description |
|---------|---------|-------------|
| Min Speed | 2 | Starting speed when you begin moving |
| Max Speed | 12 | Maximum speed reached over time |
| Acceleration Time | 2 | Seconds to reach max speed |
| Crouch Speed | 3 | Movement speed while crouching |
| Turn Speed | 720 | Degrees per second rotation |

#### Air Control
| Setting | Default | Description |
|---------|---------|-------------|
| Air Control Fraction | 0.125 | Air control as fraction of ground (1/8) |
| Air Acceleration | 15 | Acceleration rate in air |

#### Jumps
| Setting | Default | Description |
|---------|---------|-------------|
| Jump Force | 12 | Normal jump height |
| Double Jump Force | 10 | Second jump height |
| Triple Jump Force | 15 | Third jump height |
| Triple Jump Window | 1.0 | **INCREASED** - Time window for multi-jumps |
| Min Speed For Multi Jump | 2 | Very low - easy to trigger |
| Jump Speed Bonus | 1.5 | Speed added per jump in chain |
| Coyote Time | 0.15 | Time after leaving ground you can still jump |

#### Collider
| Setting | Default | Description |
|---------|---------|-------------|
| Normal Height | 2 | Standing height |
| Crouch Height | 1 | Crouching height |
| Collider Center Y | 0 | **NEW** - Y offset for CharacterController center |

### Debug Gizmos (Per System)

#### PlayerController Gizmos
| Setting | Description |
|---------|-------------|
| Show Movement Gizmos | Direction, speed bar, acceleration bar |
| Show Jump Gizmos | Jump count, momentum, coyote time, triple jump window |
| Show State Gizmos | Current state indicator, ground pound |
| Show Collider Gizmos | CharacterController capsule |
| Show Ledge Gizmos | Ledge detection rays and position |

#### PlayerGroundDetection Gizmos
| Setting | Description |
|---------|-------------|
| Show Ground Rays | Ground check raycasts |
| Show Slope Normal | Surface normal direction |
| Show Slide Direction | Downhill slide direction |
| Show Ground Stick Force | Downward force indicator |

#### MomentumSystem Gizmos
| Setting | Description |
|---------|-------------|
| Show Momentum Vector | Current momentum direction and magnitude |
| Show Fight Indicator | When fighting against momentum |
| Show Alignment Indicator | Input direction vs momentum alignment |

#### OdysseyCamera Gizmos
| Setting | Description |
|---------|-------------|
| Show Gizmos | Camera target, collision path |
| Show Input Debug | Log look input to console |

## Triple Jump Tips

The triple jump window is now **1.0 seconds** by default. This means:
- After landing from jump 1, you have 1 second to press jump for double jump
- After landing from jump 2, you have 1 second to press jump for triple jump
- You only need minimal speed (2 units) to trigger multi-jumps
- Each jump adds bonus speed

The green bar above the player shows the remaining time window.

## Events Reference

All events are raised automatically and can be subscribed to for animations and sounds:

```csharp
void OnEnable()
{
    EventBus.Subscribe<OnPlayerStateChangeEvent>(OnStateChange);
    EventBus.Subscribe<OnPlayerJumpEvent>(OnJump);
}

void OnStateChange(OnPlayerStateChangeEvent ev)
{
    Debug.Log($"State: {ev.PreviousState} -> {ev.NewState}");
}

void OnJump(OnPlayerJumpEvent ev)
{
    Debug.Log($"Jump type: {ev.JumpType}, Count: {ev.JumpCount}");
}
```

## Common Issues

### Camera keeps rotating after releasing stick
- Make sure you're using the new `InputSystem.cs` which properly resets lookInput to zero in OnLookCanceled

### Character floats above ground
- Set `Collider Center Y` to 0 in PlayerController

### Can't get triple jump
- Check the green bar above player - it shows time remaining
- Default window is 1.0 seconds
- You need minimal speed (2 units)
- Each jump must be a separate button press

### Can't see debug info
- Enable the appropriate gizmo toggles in each component's inspector
- Make sure Gizmos are enabled in the Scene view
