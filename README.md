# Flying Car Game — Unity Project

A realistic flying car game built with Unity (URP), targeting PC & Android.

## Features
- Realistic Rigidbody aerodynamics with VTOL hover
- Coin collection system with combo multiplier
- Dynamic Wind & turbulence
- Procedural city generation (Kenney City Kit)
- Dynamic day/night sky (Polyhaven HDRIs)
- Apple-style liquid UI with spring animations & Dynamic Island notifications
- Full mobile touch controls (dual joystick)
- Engine VFX, atmospheric particles, speed post-processing

## Scripts
| Folder | Scripts |
|--------|---------|
| `Core/` | FlyingCarController, Coin, CoinManager, AudioManager, EngineVFXController, EngineGlowController, SpeedPostProcessing |
| `Input/` | InputHandler |
| `Camera/` | FlightCameraController |
| `UI/` | HUDController, GameHUD, SpeedometerUI, MobileUIController, VirtualJoystick, TouchButton, MobileCanvasBuilder, LiquidUIAnimator, GlassmorphismPanel, DynamicNotification, FloatingScorePopup |
| `Environment/` | WindSystem, AtmosphericParticles, CityGenerator, SkyController |

## Setup
1. Create a new Unity project (URP template, Unity 2022 LTS+)
2. Install packages: Input System, TextMeshPro
3. Copy `Assets/Scripts/` into your Unity project
4. Download free assets:
   - [Kenney Car Kit](https://kenney.nl/assets/car-kit)
   - [Kenney City Kit](https://kenney.nl/assets/city-kit-commercial)
   - [Polyhaven HDRIs](https://polyhaven.com/hdris)
   - [Freesound engine sounds](https://freesound.org) (CC0)
5. Follow the [Asset Integration Guide](docs/Asset_Integration_Guide.md)

## Build for Android
1. Install Android Build Support module in Unity Hub
2. `File → Build Settings → Android → Switch Platform`
3. Player Settings: IL2CPP, ARM64, Package name `com.yourstudio.flyingcargame`
4. Build → Android App Bundle (.aab)
5. Upload to [Google Play Console](https://play.google.com/console)

## License
Scripts: MIT License  
Art assets: CC0 (Kenney, Polyhaven) — no attribution required
