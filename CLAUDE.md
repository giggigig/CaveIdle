# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CaveIdle is a Unity 2D idle game where players grow stalactites in a cave environment. The game features a real-time growth system based on actual elapsed time, mushroom spawning system, touch interaction mechanics, and mobile push notifications.

## Unity Setup

- **Unity Version**: 2021.3.15f1
- **Target Platform**: Mobile (Android/iOS) with cross-platform support
- **Language**: C# (.NET Standard 2.1)

## Key Game Systems

### 1. Stalactite Growth System (`StalactiteGrowth.cs`)
- **Real-time growth**: Stalactites grow over 30 real-world days (30mm total length)
- **Crack system**: Touch interactions add cracks (0-99 levels), which naturally heal over time
- **Persistent data**: Uses PlayerPrefs to save growth state across sessions
- **Growth acceleration**: Touch interactions advance growth by 1 hour per touch

### 2. Touch Input System (`TouchManager.cs`)
- **Multi-platform input**: Handles both mobile touch and desktop mouse input
- **Touch targets**: Stalactites, mushrooms, and empty space
- **Drag harvesting**: Players can drag across mushrooms to harvest them
- **Visual feedback**: Shake effects and drag trails

### 3. Water Drop System (`SimpleWaterDrop.cs`)
- **Automatic drops**: Water drops fall every 3 seconds from stalactite tip
- **Touch-triggered drops**: Additional drops when stalactite is touched
- **Particle effects**: Splash effects when water hits ground
- **Physics simulation**: Realistic falling animation with gravity

### 4. Mushroom Ecosystem (`Mushroom.cs`, `MushroomManager.cs`)
- **Growth stages**: 3-stage growth system (Sprout → Growing → Mature)
- **Humidity system**: Water drops increase cave humidity, affecting spawn rates
- **Spawn management**: Mushrooms spawn in radius around stalactite based on humidity
- **Harvesting**: Touch mature mushrooms to collect them

### 5. Sound System (`SoundManager.cs`)
- **Singleton pattern**: Global audio management
- **Dual audio sources**: Separate BGM and SFX channels
- **Audio pooling**: Multiple SFX sources for overlapping sounds
- **Settings persistence**: Volume and enable/disable settings saved to PlayerPrefs

### 6. Notification System (`GameNotificationManager.cs`)
- **Offline growth**: Calculates and displays growth that occurred while app was closed
- **Event notifications**: Stalactite completion and destruction alerts
- **Push notifications**: Schedule growth reminders when app goes to background
- **Cross-platform**: Supports both Android and iOS notification systems

## Architecture Patterns

- **Component-based**: Each system is a separate MonoBehaviour component
- **Manager pattern**: Central managers coordinate between systems (MushroomManager, SoundManager)
- **Event-driven**: Systems communicate through method calls and FindObjectOfType
- **Data persistence**: Uses Unity's PlayerPrefs for save data with DateTime serialization

## Core Game Loop

1. **Stalactite Growth**: Continuous real-time growth over 30 days
2. **Water Drops**: Automatic drops every 3 seconds + touch-triggered drops
3. **Humidity**: Water drops increase humidity affecting mushroom spawn rates
4. **Mushroom Growth**: Mushrooms spawn and grow in 3 stages over ~5 minutes
5. **Player Interaction**: Touch stalactite to accelerate growth and add cracks
6. **Resource Management**: Harvest mature mushrooms before they despawn

## Important Unity-Specific Notes

- **PlayerPrefs**: Used extensively for persistent data storage
- **DateTime serialization**: Uses `DateTime.ToBinary()` for cross-platform compatibility
- **Coroutines**: Used for animations, timed events, and async operations
- **Physics2D**: Used for mushroom collision detection and touch area checks
- **TextMeshPro**: UI text rendering system
- **Mobile Notifications**: Platform-specific push notification implementation

## Development Commands

This is a Unity project without external build scripts. Use Unity Editor for:
- **Build**: File → Build Settings → Build (or Build and Run)
- **Play testing**: Unity Editor Play mode
- **Console debugging**: Unity Console window shows Debug.Log output

## Script Dependencies

Core system dependencies:
- `StalactiteGrowth` ← `TouchManager`, `StalactiteUIManager`
- `SimpleWaterDrop` ← `TouchManager`
- `MushroomManager` ← `SimpleWaterDrop` (humidity)
- `SoundManager` (singleton) ← All systems
- `GameNotificationManager` ← `StalactiteGrowth` (events)

## Testing and Debugging

Each script includes `[ContextMenu]` debug methods:
- Test growth acceleration, crack addition, reset systems
- Access via Component's context menu in Inspector
- Extensive Debug.Log statements throughout for runtime debugging

# CLAUDE.md

## Rules
- 모든 Unity C# 스크립트는 클래스별로 파일을 분리한다. (예: Player.cs 안에는 Player 클래스만 존재)
- MonoBehaviour를 상속하는 클래스는 반드시 PascalCase로 네이밍한다.
- private 필드는 `_camelCase`로, public/protected 필드는 `PascalCase`로 네이밍한다.
- 유니티 라이프사이클 메서드(Update, Start, Awake 등)는 클래스 상단에 배치한다.
- 주석은 한국어가 아닌 영어로 작성한다.
- 불필요한 using 문은 제거한다.
- 작업 범위는 `/Assets/Scripts` 폴더 내부만 포함한다. 이외의 파일은 무시한다.
- 생성되는 스크립트는 Unity 2021.3.15 버전 호환성을 유지한다.
