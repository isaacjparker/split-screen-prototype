# Fluid fog for a top-down Godot game

Four files:

- `FluidSolver.cs`   - pure C# fluid sim (no Godot, black box, never edit to use it)
- `FogManager.cs`    - the one node that runs the sim and uploads the texture
- `FogInfluencer.cs` - component you put on things that push or clear fog
- `fog.gdshader`     - the only shader; draws the fog on a flat plane

The whole pipeline: influencers report their position -> manager maps them to a grid
and steps the sim -> manager writes the density into a texture -> the plane's shader
samples that texture. The simulation runs once per frame no matter how many cameras
look at it.

## Setup (about 5 minutes)

1. Copy all four files into your project. Build the C# solution once so the new
   scripts compile.

2. Make sure your ground lies on the XZ plane (the Godot default for top-down 3D),
   with Y as up.

3. Create the fog plane:
   - Add a `MeshInstance3D` to your level.
   - Set its Mesh to a new `PlaneMesh`. Set the plane Size to your play area,
     e.g. `40 x 40`.
   - Position it at the centre of that area, raised slightly above the floor
     (e.g. Y = 0.2) so it sits just over the ground.

4. Create the material:
   - On that MeshInstance3D, create a new `ShaderMaterial` (use Material Override).
   - Assign `fog.gdshader` to it.
   - Save the ShaderMaterial as a resource, e.g. `fog_material.tres`
     (right-click the material -> Save As). This matters so the manager and the
     plane share the SAME material.
   - Set `fog_color`, `max_alpha`, `contrast` to taste.

5. Add the manager:
   - Add a plain `Node` to your level and attach `FogManager.cs`.
   - In its inspector set:
       - `World Size` = the SAME numbers as your plane size (e.g. 40, 40).
       - `Fog Material` = drag in `fog_material.tres` (the same resource).
       - `Follow Target` = leave empty for a fixed area, or set it to the player /
         camera rig if you want the fog field to travel with the action.
       - Leave `Grid Size`, `Dissipation`, `Ambient` at defaults for now.

6. Add influencers:
   - On your player (and anything else that should shove fog around), add a child
     `Node3D` and attach `FogInfluencer.cs`. Set Mode = Push.
   - For a spell that clears fog, spawn a `FogInfluencer` with Mode = Clear and free
     it when the effect ends. Tune `Radius` and `Strength`.

Press play. You should see a fog blanket that parts as the player moves and gets
punched out by Clear influencers, then flows back.

## Tuning

- No fog visible at all -> raise `Ambient` (e.g. 0.4) or `Density Scale`.
- Fog never clears / refills too fast -> lower `Ambient` or raise `Dissipation`.
- Push too weak / too strong -> change the influencer `Strength` (1-5 is the useful range).
- Fog looks blocky -> raise `Grid Size` to 96 or 128 (costs more CPU; 64 is cheap).
- Fog appears mirrored -> in `FogManager.UploadTexture`, swap the loop so it writes
  `_floatBuf[i + n * (n - 1 - j)]` instead of `_floatBuf[i + n * j]`.

## Split-screen

The simulation cost is paid once, regardless of player count. Two cases for the visual:

- All cameras share one World3D (multiple Cameras / one scene): you already have one
  fog plane, and every camera sees it. Nothing to add.
- Each split uses its own World3D / SubViewport: put a fog plane in each world, but
  assign the SAME `fog_material.tres` to all of them. The single manager updates that
  one material's texture, so every plane shows the same simulation. Still one sim.

## Performance

A 64x64 grid is well under a millisecond per step in C# and the texture is ~16 KB.
The sim is capped to `Sim Rate` steps/sec (default 30) independent of framerate.
Avoid Godot's built-in volumetric (FogVolume) fog for this - it is computed per camera
and would multiply across split-screen, especially on Steam Deck.
