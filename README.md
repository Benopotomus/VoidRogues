# VoidRogues

A fast-paced **2D action roguelike** set in the collapsing remnants of a dead universe. Players battle through procedurally generated void-dungeons, collecting cursed relics and corrupted power to survive increasingly hostile encounters — until the Void consumes them, or they consume it.

## Tech Stack
| Area | Choice |
|------|--------|
| Engine | Unity 2022 LTS (2D URP) |
| Language | C# (.NET Standard 2.1) |
| IDE | JetBrains Rider / VS Code |
| Version Control | Git + GitHub |
| Art | Aseprite (pixel art, 16×16 tile grid) |
| Audio | FMOD (integration via FMOD Unity plugin) |

## Quick Start

1. Install [Unity Hub](https://unity.com/download) and **Unity 2022.3 LTS**
2. Clone this repo: `git clone https://github.com/Benopotomus/VoidRogues.git`
3. Open the project folder in Unity Hub → *Add project from disk*
4. Open `Assets/Scenes/MainMenu.unity`
5. Press **Play** ▶

## Project Layout

```
Assets/
├── Animations/        # Animator controllers and clips
├── Audio/
│   ├── Music/
│   └── SFX/
├── Materials/         # URP materials and shaders
├── Prefabs/
│   ├── Enemies/
│   ├── Items/
│   ├── Player/
│   └── Rooms/
├── Resources/         # Runtime-loaded assets (ItemDatabase, etc.)
├── Scenes/
│   ├── MainMenu.unity
│   ├── Game.unity
│   └── GameOver.unity
├── Scripts/
│   ├── Combat/
│   ├── Core/
│   ├── Dungeon/
│   ├── Enemies/
│   ├── Items/
│   ├── Player/
│   └── UI/
└── Sprites/
    ├── Enemies/
    ├── Player/
    ├── Tiles/
    └── UI/
```

## Documentation

| Document | Audience |
|----------|----------|
| [GAME_DESIGN.md](GAME_DESIGN.md) | Game designers, artists, writers |
| [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) | Programmers, technical contributors |

## Contributing

1. Fork and create a feature branch: `git checkout -b feat/your-feature`
2. Follow the coding conventions in [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)
3. Open a pull request — include a short description and any relevant screenshots

## License

All source code is MIT licensed. Art and audio assets are proprietary — do not redistribute.
