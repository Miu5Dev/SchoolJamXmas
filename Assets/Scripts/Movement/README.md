# Mario Odyssey-Style Movement System for Unity

A comprehensive, modular movement system inspired by Super Mario Odyssey and Super Mario 64.

## Features

### Core Movement
- **Walk/Run**: Gradual acceleration system (hold accelerate button to speed up over time)
- **Crouch**: Standard crouch with reduced speed and height
- **Crouch Slide**: Press crouch while moving to slide with momentum
- **Slope Sliding**: Automatic sliding on steep slopes with lateral control

### Jump System
- **Normal Jump**: Basic jump
- **Double Jump**: Second jump in sequence (requires some speed)
- **Triple Jump**: Third jump for maximum height (requires some speed)
- **Long Jump**: Press jump during crouch slide for horizontal distance
- **Backflip**: Press jump while crouching stationary, or during quick direction change
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

### Visual Features
- Automatic slope alignment (player tilts on slopes)
- Air control with configurable fraction (default 1/8 of ground control)

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

The components will automatically find each other.

### 2. Configure Input System

1. Make sure you have Unity's Input System package installed
2. Create a new Input Actions asset or use the existing one
3. Ensure you have these actions defined:
   - **Move** (Vector2): WASD / Left Stick
   - **Look** (Vector2): Mouse Delta / Right Stick
   - **Jump** (Button): Space / South Button
   - **Crouch** (Button): C/Ctrl / East Button
   - **Sprint** (Button): Shift / West Button
   - **Interact** (Button): E / North Button

### 3. Setup Camera

1. Add `OdysseyCamera` to your camera
2. Assign the player transform as the target
3. Configure distance and sensitivity settings

### 4. Configure Layers

1. Create a "Ground" layer for walkable surfaces
2. Assign the layer to your ground objects
3. Set the Ground Layer in PlayerGroundDetection
4. Create a "Ledge" layer if you want specific ledge surfaces

## Configuration Reference

### PlayerController Settings

#### Movement
| Setting | Default | Description |
|---------|---------|-------------|
| Walk Speed | 6 | Base walking speed |
| Max Speed | 12 | Maximum speed when accelerating |
| Crouch Speed | 3 | Movement speed while crouching |
| Acceleration | 8 | How fast player accelerates |
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
| Long Jump Force | 8 | Long jump vertical force |
| Long Jump H. Boost | 15 | Long jump horizontal speed |
| Backflip Force | 14 | Backflip vertical force |
| Min Speed For Multi Jump | 4 | Minimum speed for double/triple jump |
| Jump Speed Bonus | 1.5 | Speed added per jump in chain |
| Coyote Time | 0.15 | Time after leaving ground you can still jump |
| Jump Buffer Time | 0.1 | Pre-jump input buffer |

#### Crouch Slide
| Setting | Default | Description |
|---------|---------|-------------|
| Crouch Slide Boost | 8 | Speed boost when starting slide |
| Crouch Slide Min Speed | 4 | Minimum speed to start sliding |
| Crouch Slide Friction | 5 | How fast slide slows down |
| Crouch Slide Max Duration | 1.5 | Maximum slide time |

#### Ground Pound & Dive
| Setting | Default | Description |
|---------|---------|-------------|
| Ground Pound Delay | 0.2 | Freeze time before falling |
| Ground Pound Speed | 30 | Falling speed during ground pound |
| Dive Force | 10 | Dive vertical force |
| Dive Horizontal Speed | 15 | Dive forward speed |

### MomentumSystem Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Base Decay | 3 | Normal momentum decay rate |
| Opposed Decay | 8 | Decay when moving against momentum |
| Side Decay | 5 | Decay when moving perpendicular |
| Aligned Decay | 2 | Decay when moving with momentum |
| Fight Strength | 0.7 | How strongly input fights momentum |
| Fight Ramp Time | 0.3 | Time to reach full fight strength |

## Events Reference

### Input Events
- `onMoveInputEvent` - Movement input changed
- `onLookInputEvent` - Camera input changed
- `onJumpInputEvent` - Jump button pressed/released
- `onCrouchInputEvent` - Crouch button pressed/released
- `onRunInputEvent` - Accelerate button pressed/released

### Player Events
- `OnPlayerStateChangeEvent` - Player state changed (for animations)
- `OnPlayerMoveEvent` - Player is moving
- `OnPlayerStopEvent` - Player stopped
- `OnPlayerJumpEvent` - Player jumped (includes jump type)
- `OnPlayerLandEvent` - Player landed
- `OnPlayerCrouchEvent` - Crouch state changed
- `OnPlayerCrouchSlideStartEvent` - Started crouch slide
- `OnPlayerCrouchSlideEndEvent` - Ended crouch slide
- `OnPlayerGroundPoundEvent` - Ground pound phase changed
- `OnPlayerDiveEvent` - Player performed dive
- `OnPlayerLedgeGrabEvent` - Ledge grab state changed
- `OnPlayerLedgeMoveEvent` - Player moved on ledge
- `OnPlayerLedgeClimbEvent` - Ledge climb progress
- `OnPlayerMomentumEvent` - Momentum changed
- `OnPlayerSpeedEvent` - Speed changed
- `OnPlayerSlopeEvent` - Slope angle changed
- `OnPlayerGroundedEvent` - Grounded state changed
- `OnPlayerAirborneEvent` - Airborne state update

## Bug Fixes from Original

1. **Jump not working**: Fixed ground stick force preventing jumps
2. **Momentum persisting**: Fixed jump momentum not clearing on land
3. **Double/Triple jumps**: Fixed jump count resetting too early
4. **Ledge grab positioning**: Fixed player positioning inside walls
5. **Camera with controllers**: Fixed camera not updating with constant input
6. **Backflip conditions**: Added stationary, back press, and quick turn triggers
7. **Long jump**: Now triggers from crouch slide + jump

## Common Issues

### Player won't jump
- Check that Ground Layer is configured correctly
- Ensure ground objects have the correct layer
- Verify ground check distance is appropriate for your scale

### Camera doesn't rotate with controller
- This is fixed in the new InputSystem - it sends look input every frame
- Check Controller Sensitivity setting on camera

### Ledge grab not working
- Ensure Ledge Layer is configured
- Check ledge grab distance and height settings
- Make sure there's space above the ledge to stand

### Multi-jumps not triggering
- Player needs some horizontal speed (configurable)
- Must jump within time window of previous jump
- Each jump in chain is a separate press (not hold)

## Animation Integration

Subscribe to events for animation triggers:

```csharp
void OnEnable()
{
    EventBus.Subscribe<OnPlayerStateChangeEvent>(OnStateChange);
    EventBus.Subscribe<OnPlayerJumpEvent>(OnJump);
    EventBus.Subscribe<OnPlayerLandEvent>(OnLand);
}

void OnStateChange(OnPlayerStateChangeEvent ev)
{
    // Update animator state
    animator.SetInteger("State", (int)ev.NewState);
}

void OnJump(OnPlayerJumpEvent ev)
{
    // Play jump animation based on type
    animator.SetTrigger($"Jump_{ev.JumpType}");
}

void OnLand(OnPlayerLandEvent ev)
{
    // Play land animation
    animator.SetTrigger(ev.HardLanding ? "HardLand" : "Land");
}
```

## Sound Integration

Same event system for sounds:

```csharp
void OnEnable()
{
    EventBus.Subscribe<OnPlayerJumpEvent>(OnJump);
    EventBus.Subscribe<OnPlayerGroundPoundEvent>(OnGroundPound);
}

void OnJump(OnPlayerJumpEvent ev)
{
    AudioClip clip = ev.JumpType switch
    {
        JumpType.Normal => normalJumpSound,
        JumpType.Double => doubleJumpSound,
        JumpType.Triple => tripleJumpSound,
        JumpType.Long => longJumpSound,
        JumpType.Backflip => backflipSound,
        _ => normalJumpSound
    };
    audioSource.PlayOneShot(clip);
}
```

## License

Free to use and modify for your projects.
