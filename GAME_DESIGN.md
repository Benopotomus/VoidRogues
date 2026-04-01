# VoidRogues — Game Design Document
> **Instruction Prompt — Senior Game Designer**
>
> Use this document as the authoritative design reference when creating content, balancing systems, writing dialogue, or evaluating new features. Every design decision should serve the core pillars below. When in doubt, ask: *"Does this make the run feel meaningful and the death feel fair?"*

---

## 1. Vision Statement

VoidRogues is a **2D top-down action roguelike** set in the collapsing remnants of a dead universe. Players embody Void-touched rogues — scavengers of cursed power — fighting through procedurally generated dungeon sectors to reach the Void Core before entropy destroys everything. Runs are short (20–40 minutes), deaths are permanent, and every item combination tells a unique story.

**Three words that define the experience:** *Dangerous. Rewarding. Replayable.*

---

## 2. Core Design Pillars

| # | Pillar | Description |
|---|--------|-------------|
| 1 | **Risk vs. Reward** | Every room, item, and curse presents a visible trade-off. Players are never punished by hidden systems. |
| 2 | **Build Synergy** | Items and abilities combine in emergent, discoverable ways. The best runs feel self-discovered. |
| 3 | **Responsive Action** | Combat feels immediate. Attacks, rolls, and hits have clear feedback (audio, screen-shake, freeze-frames). |
| 4 | **Thematic Cohesion** | Every mechanic reinforces the "consuming void" aesthetic — health is Sanity, gold is Fragments, XP is Corruption. |

---

## 3. Target Player Profile

- **Primary:** Fans of *Enter the Gungeon*, *The Binding of Isaac*, *Hades*
- **Playtime per session:** 20–40 minutes (one run)
- **Skill floor:** Accessible controls; depth in itemization and enemy reading
- **Platforms:** PC (Steam), with mobile stretch goal

---

## 4. Core Loop

```
Start Run
  └─> Enter Sector (floor)
        └─> Clear Rooms (combat / events)
              └─> Choose Reward (item / curse / upgrade)
                    └─> Boss Room
                          ├─> Defeat Boss → Next Sector
                          └─> Die → Meta-progression unlock → Restart
```

### Sub-loop: Room Encounter
1. Enter room → doors lock
2. Eliminate all enemies (or complete event objective)
3. Doors unlock → reward chest/shop/event spawns
4. Choose a path to the next room on the sector map

---

## 5. Sectors (Floors)

| # | Name | Theme | Enemy Faction | Boss |
|---|------|-------|---------------|------|
| 1 | The Shattered Halls | Crumbling space station | Drones & Sentinels | OVERSEER-7 |
| 2 | The Fungal Abyss | Bio-organic overgrowth | Spore Walkers | The Bloom Mother |
| 3 | The Void Rift | Pure void energy | Rift Shades | The Harbinger |
| 4 | The Void Core | Final sector | All factions | The Void |

Each sector has **8–12 rooms**: 6–8 combat rooms, 1–2 event rooms, 1 shop, 1 boss room.

---

## 6. Player Character

### Stats
| Stat | Default | Description |
|------|---------|-------------|
| Sanity (HP) | 100 | Reaches 0 → death. No regen between rooms by default. |
| Speed | 5 m/s | Movement speed. |
| Damage | 10 | Base weapon damage multiplier. |
| Fire Rate | 1× | Attacks per second multiplier. |
| Dodge Roll | 0.4 s i-frames | Invincibility frames on roll. |
| Corruption | 0 | Increases as cursed items are collected. See §10. |

### Weapons
- Each run starts with one **Primary Weapon** (chosen from 3 options)
- Weapon types: Pistol, Shotgun, SMG, Charged Blaster, Melee Blade
- Weapons have their own stats: damage, fire rate, projectile speed, range, ammo

### Abilities
- One active ability slot (default: Void Dash — short teleport)
- One passive ability slot (starts empty, filled by items)

---

## 7. Enemies

### Design Rules
1. Every enemy has a **readable telegraph** before their dangerous attack (wind-up animation ≥ 0.5 s)
2. Enemies have at most **2 attack patterns** (basic enemies) or **4 attack patterns** (elites/bosses)
3. Death always produces a clear visual/audio cue and a loot drop chance

### Enemy Archetypes
| Archetype | Role | Behavior |
|-----------|------|----------|
| Grunt | Filler / damage sponge | Slow melee rush |
| Shooter | Ranged pressure | Strafe + fire, predictable pattern |
| Charger | Burst aggression | Telegraphed charge attack, very fast |
| Shielder | Cover for allies | High defense, creates safe zone for enemies |
| Elite | Mid-boss | Combination of 2 archetypes + unique mechanic |
| Boss | Sector finale | 3-phase fight, arena hazards, unique drops |

### Enemy Stats Template
```
HP:          [sector baseline × difficulty modifier]
Damage:      [hit damage on contact or projectile]
Speed:       [movement speed m/s]
Attack Rate: [attacks per second]
Loot Table:  [list of possible drops with weights]
```

---

## 8. Items & Relics

### Item Tiers
| Tier | Color | Rarity | Notes |
|------|-------|--------|-------|
| Common | Grey | 50% | Small, straightforward bonuses |
| Uncommon | Green | 30% | Moderate effect, some conditions |
| Rare | Blue | 15% | Strong effect, often conditional |
| Cursed | Purple | 4% | Powerful but increases Corruption |
| Void | Gold | 1% | Game-altering, always increases Corruption |

### Item Design Rules
1. **One clear effect** per item. Multi-effect items must show both clearly in the tooltip.
2. **Visual identity** — each item has a unique 16×16 sprite and a 2-line lore description.
3. **No trap items** — the trade-off must be visible before pickup (cursed items show the Corruption increase).
4. Items should create **build paths**, not just stat bumps. Ask: *"What other items does this combo with?"*

### Sample Items
| Name | Tier | Effect |
|------|------|--------|
| Void Shard | Common | +15% damage |
| Echo Rounds | Uncommon | Projectiles pierce 1 extra enemy |
| Heartless Core | Rare | +50 max Sanity, but no Sanity pickups drop |
| The Parasite | Cursed | +30% damage, +20 Corruption, lose 2 Sanity per second |
| Void Eye | Void | All projectiles home onto the nearest enemy; +50 Corruption |

---

## 9. Shops & Events

### Shop
- Appears once per sector
- Sells 3 items (random tier, weighted toward sector tier)
- Currency: **Fragments** (dropped by enemies, breakables)
- One re-roll available per shop (costs 20 Fragments)

### Events (Void Shrines)
Players encounter a shrine with a choice — a risk/reward gamble:

| Event Type | Offer | Cost |
|------------|-------|------|
| Blessing | Gain a Rare item | Lose 20 Sanity |
| Curse | Gain +25% damage for the floor | Gain 15 Corruption |
| Gamble | Random item (any tier) | Pay 30 Fragments |
| Healing Font | Restore 30 Sanity | Destroy 1 held item |

---

## 10. Corruption System

Corruption is the game's **risk escalation mechanic**. It increases from cursed items and certain events.

| Corruption Level | Threshold | Effect |
|-----------------|-----------|--------|
| Clean | 0–24 | No effect |
| Tainted | 25–49 | Elite enemies appear in regular rooms |
| Corrupted | 50–74 | All enemy damage +20%; Sanity pickups are rarer |
| Void-Touched | 75–99 | Void enemies can spawn; random room hazards activate |
| Full Void | 100 | Instant death at end of current room |

---

## 11. Meta-Progression (Runs unlock permanently)

| Unlock Category | Examples |
|-----------------|---------|
| Starting Loadouts | New weapon options at run start |
| Void Echoes | Passive stat bonuses that persist across runs (limited) |
| New Items | Expands the item pool |
| Lore Fragments | Story entries revealed over multiple runs |
| New Characters | Additional rogues with unique starting stats/abilities |

---

## 12. Audio & Feel Guidelines

- **Music:** Evolves dynamically — ambient drone in safe rooms → aggressive electronic/industrial in combat → distorted ambient for boss arenas
- **SFX Rules:** Every player action has audio feedback within 1 frame. Hits must feel impactful (short pitch-shifted noise + screen flash).
- **Screen Shake:** Applied on: player hit, player death, boss slam, explosion. Never on regular projectile fire.
- **Freeze Frame:** 3–5 frame freeze on boss hits and player death.

---

## 13. UI / UX Principles

1. **HUD is minimal** — Sanity bar (top left), Fragments (top right), active ability icon (bottom center). No clutter.
2. **Item tooltips appear on hover** — show name, tier, effect, Corruption cost if any.
3. **Death screen** — show run stats (rooms cleared, items collected, damage dealt, cause of death). One-click restart.
4. **Pause menu** — show current items, stats, sector map. No in-run saving.

---

## 14. Content Scope (v1.0)

| Category | Count |
|----------|-------|
| Sectors | 4 |
| Enemy Types | 20 (5 per sector) |
| Boss Fights | 4 |
| Items | 60 |
| Events | 10 |
| Player Characters | 2 |
| Weapons | 10 |

---

## 15. Out of Scope (v1.0)

- Multiplayer
- Controller support (stretch goal post-launch)
- Procedural music generation
- Steam Workshop / mod support
