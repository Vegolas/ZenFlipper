# Gothic 2 .ZEN File Coordinate Flipper

This tool flips coordinates in Gothic 2 `.zen` world files to help with map mirroring modifications.

## Features

- Flips `trafoOSToWSPos` (position vectors) across selected axes
- Flips `position` and `direction` (waypoint data)
- Flips `bbox3DWS` (bounding boxes) to maintain correct bounds
- Flips `trafoOSToWSRot` (rotation matrices) when using `-rotation` flag
- Supports X, Y, Z axis flipping individually or in combination
- Preserves original file formatting and encoding (UTF-8, Windows-1252, ASCII)
- **Dry run mode** to test file encoding without transformations

## Building

Requires .NET 6.0 or later.

```bash
dotnet build -c Release
```

The executables will be in `bin/Release/net6.0/`

## Verified Transformation

This tool has been verified against manual transformations. Here's a real example:

**Original object:**
```
bbox3DWS: 15557.8027 1032.73633 -27.5603027 16030.5684 1442.10681 465.1138
trafoOSToWSRot: 86972e3f00000000173a3b3f000000000000803f00000000173a3bbf0000000086972e3f
trafoOSToWSPos: 15794.1846 1215.23633 218.777618
```

**After X-axis flip with `-x -rotation`:**
```
bbox3DWS: -16030.5684 1032.73633 -27.5603027 -15557.8027 1442.10681 465.1138
trafoOSToWSRot: [negated first column of rotation matrix]
trafoOSToWSPos: -15794.1846 1215.23633 218.777618
```

The tool matches the manual transformation process exactly.

## Usage

```bash
ZenFlipper <input.zen> <output.zen> [options]
```

### Options

- `-x` - Flip X axis only (default if no option specified)
- `-y` - Flip Y axis only
- `-z` - Flip Z axis only
- `-xy` - Flip X and Y axes
- `-xz` - Flip X and Z axes
- `-yz` - Flip Y and Z axes
- `-xyz` - Flip all three axes

### Examples

**Flip along X axis (for mirroring left-right):**
```bash
ZenFlipper NEWWORLD.ZEN NEWWORLD_FLIPPED.ZEN -x
```

**Flip along Z axis:**
```bash
ZenFlipper NEWWORLD.ZEN NEWWORLD_FLIPPED.ZEN -z
```

**Flip along both X and Z axes:**
```bash
ZenFlipper NEWWORLD.ZEN NEWWORLD_FLIPPED.ZEN -xz
```

## What Gets Modified

The tool handles three types of coordinate data in .zen files:

### 1. Object Position Vectors (`trafoOSToWSPos`)

Used for regular game objects (items, sounds, NPCs, etc.)

Original:
```
trafoOSToWSPos=vec3:-1135.65906 -420.442902 5063.6123
```

After X-axis flip:
```
trafoOSToWSPos=vec3:1135.65906 -420.442902 5063.6123
```

### 2. Waypoint Positions (`position`)

Used for navigation waypoints (where NPCs walk, spawn points, etc.)

Original:
```
position=vec3:-153.670609 -125.461304 -1740.34607
```

After X-axis flip:
```
position=vec3:153.670609 -125.461304 -1740.34607
```

### 3. Waypoint Directions (`direction`)

Direction vectors that NPCs face at waypoints

Original:
```
direction=vec3:-0.615661502 0 0.788010716
```

After X-axis flip:
```
direction=vec3:0.615661502 0 0.788010716
```

### 4. Bounding Boxes (`bbox3DWS`)

Original:
```
bbox3DWS=rawFloat:-1735.65906 -1020.44287 4463.6123 -535.659058 179.557098 5663.6123
```

After X-axis flip (min/max values are swapped and negated):
```
bbox3DWS=rawFloat:535.659058 -1020.44287 4463.6123 1735.65906 179.557098 5663.6123
```

## Gothic 2 Coordinate System

In Gothic 2's coordinate system:
- **X axis**: Left-Right (typically flipped for mirroring)
- **Y axis**: Up-Down (vertical)
- **Z axis**: Forward-Backward (depth)

Since you mentioned flipping the 3D world via X axis, use the `-x` option.

## Troubleshooting Crashes

If Gothic 2 crashes when loading your flipped .zen file, try these steps:

### Step 0: DRY RUN TEST (DO THIS FIRST!)

Before trying any transformations, test if file encoding/format is the issue:

```bash
# Build the dry run tool
cd ZenDryRun
dotnet build -c Release

# Test: Read and write the ORIGINAL file without changes
ZenDryRun ORIGINAL.ZEN TEST_OUTPUT.ZEN
```

This will:
- Detect the file encoding (UTF-8, Windows-1252, ASCII, etc.)
- Analyze line endings (CRLF vs LF)
- Write the file back unchanged
- Compare byte-for-byte to verify no corruption

**Then test in Gothic 2:**
1. If `TEST_OUTPUT.ZEN` loads fine → File format is OK, transformation is the issue
2. If `TEST_OUTPUT.ZEN` crashes → Original file may be corrupted OR encoding issue

**If files are byte-identical but game still crashes:**
Your transformation logic needs adjustment (continue to Step 1 below)

**If files differ:**
Encoding conversion occurred. The tool will suggest encoding options to try.

### Step 1: Run the Diagnostic Tool

First, analyze your flipped .zen file to identify issues:

```bash
ZenDiagnostic NEWWORLD_FLIPPED.ZEN
```

This will show you:
- Invalid bounding boxes (where min > max)
- Coordinate ranges and distributions
- Suggested axis to flip

### Step 2: Common Issues and Solutions

**Error: "UNHANDLED EXCEPTION" or crash on load**

This usually means:
1. **Wrong axis flipped** - Try `-z` instead of `-x`, or vice versa
2. **Rotation matrices need flipping** - Add `-rotation` flag
3. **Bounding boxes are invalid** - The diagnostic tool will detect this

**Objects appear but in wrong locations**
- Your mesh flip and coordinate flip are using different axes
- Make sure both use the same axis (e.g., both X or both Z)

**World loads but looks corrupted**
- Try adding the `-rotation` flag to flip rotation matrices too

### Step 3: Systematic Testing

Test different combinations in this order:

```bash
# Test 1: X-axis only
ZenFlipper original.zen test1.zen -x

# Test 2: Z-axis only  
ZenFlipper original.zen test2.zen -z

# Test 3: X-axis with rotation
ZenFlipper original.zen test3.zen -x -rotation

# Test 4: Z-axis with rotation
ZenFlipper original.zen test4.zen -z -rotation
```

Load each test file in Gothic 2 to see which works.

### Step 4: Use Preview Mode

Before applying changes, preview what will happen:

```bash
ZenFlipper input.zen -preview -x
```

This shows the first 5 transformations without modifying the file.

## Updated Usage Examples

**Preview changes before applying:**
```bash
ZenFlipper NEWWORLD.ZEN -preview -x
```

**Apply X-axis flip:**
```bash
ZenFlipper NEWWORLD.ZEN NEWWORLD_FLIPPED.ZEN -x
```

**Apply with rotation matrices:**
```bash
ZenFlipper NEWWORLD.ZEN NEWWORLD_FLIPPED.ZEN -x -rotation
```

**Verbose output to see changes:**
```bash
ZenFlipper NEWWORLD.ZEN NEWWORLD_FLIPPED.ZEN -x -verbose
```

## Testing

Always test with a small portion of your world first:
1. Make a backup of your original .zen file
2. Run the tool with your desired axis
3. Load the world in Gothic 2 to verify correct placement
4. If objects appear in wrong positions, try a different axis combination

## Troubleshooting

**Objects appear on the wrong side:**
- Try `-x` if you used `-z`, or vice versa

**Objects are upside down:**
- Try adding or removing `-y` from your axis combination

**Objects are in completely wrong positions:**
- Verify you flipped the 3D meshes using the same axis
- Check that both the mesh flip and coordinate flip use matching axes