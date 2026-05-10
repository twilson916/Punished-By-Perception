# Challenge Generator — Visual Artwork Handoff

## What Exists

`ChallengeGenerator.cs` — static class, `Generate(difficulty 1-5)` returns a `QuizQuestion`.  
`QuizQuestion.artwork` — a `UnityEngine.Sprite` field. If non-null, `QuizUI` shows it. If null, image hidden.  
`QuizQuestion.correctAnswerIndex` — 0=A 1=B 2=C 3=D. Always four answers.

Current pool has 12 text-only templates (some text-only questions are fine, keep them). The visual illusion questions need `artwork` populated.

---

## What Needs to Be Written

Two new files:

### 1. `IllusionPainter.cs` — static helper class
Low-level `Texture2D` drawing primitives:
- `Fill(Texture2D tex, Color c)`
- `DrawRect(Texture2D tex, int x, int y, int w, int h, Color c)`
- `DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color c, int thickness)`
- `DrawArc(Texture2D tex, int cx, int cy, int r, float startDeg, float endDeg, Color c, int thickness)` — full circle when 0–360
- `MakeSprite(Texture2D tex)` — calls `tex.Apply()`, wraps in `Sprite.Create()`
- `ColorFromHSV(float h, float s, float v)` — Unity doesn't have this built-in

### 2. `IllusionGenerator.cs` — static class
One public method per illusion. Each takes `(int difficulty, int answer)` where `answer` is 0–3 matching A–D, or -1 for "none" (base state — all options equivalent). Returns a `Sprite`.

The `ChallengeGenerator` pool entries for visual illusions call these and assign `.artwork`.

---

## The Seven Illusions

All canvases 400×400 unless noted. Background white unless noted.

### Müller-Lyer — 400×350
Three horizontal lines (A top, B mid, C bottom), each with arrowheads at both ends.  
Line A: inward arrows `>—<`. Line B: outward `<—>`. Line C: outward `<—>`.  
Base length all three: 220px. Center x=200, y = 90 / 175 / 260.  
Arrowhead: arm 22px, 35° from line.

**Answer param:** winning line gets +`delta` px on each side.  
**Difficulty param:** `delta` — **fill empirically, suggested range [1, 12]**.  
Question: "Which line is actually the longest?" A/B/C/D="None — all equal"

---

### Poggendorff — 400×420
Vertical grey occluding rectangle x=[155,245], full height.  
Three diagonal left segments (slope dy/dx=0.22), labeled A/B/C, from x=0 to x=155:
- A: starts y=80, B: y=185, C: y=290

Three unlabeled right segments from x=245 to x=400, same slope.  
Collinear continuations for A/B/C exit rectangle at y=134/239/344.

**Answer param:** one right segment snapped to collinear with the answer left line. Others offset by `offset` px.  
**Difficulty param:** `offset` for the wrong lines — **fill empirically, suggested range [6, 28]**.  
Question: "Which left line (A, B, C) connects through the rectangle to a right line?" D="None — none connect"

---

### Hering — 400×400
20 radiating dark-blue lines from center (200,200) to canvas edges, 9° apart.  
Three red vertical test lines at x=120, x=200, x=280, y=[10,390].

**Answer param:** two lines are arcs (curved), one is straight. Answer = which is straight (A=left, B=center, C=right, D=none — all curved).  
Arc formula: circle centered at `(x_line + R, 200)` with radius R. Small R = obvious curve.  
**Difficulty param:** `R` — **fill empirically, suggested range [130, 1800]**.

---

### Ebbinghaus — 500×200
Three side-by-side instances at x-centers 85, 250, 415. y-center 100.  
Each has a center circle (base r=22) + 6 surrounding circles evenly spaced on a ring.  
Left: surround r=10, ring dist=38. Mid: surround r=30, ring dist=65. Right: surround r=10, ring dist=38.

**Answer param:** winning instance gets center r bumped by `delta`.  
**Difficulty param:** `delta` — **fill empirically, suggested range [1, 8]**.  
Question: "Which center circle is actually the largest?" A=left B=mid C=right D="All equal"

---

### Simultaneous Brightness Contrast — 400×300
Two large outer squares side by side: left dark (#1A1A1A), right light (#E5E5E5), thin neutral divider.  
Inside each: a smaller inner square, both same grey base (#888888).

**Answer param:** winning inner square's grey value shifted by `delta` (0–255 scale).  
**Difficulty param:** `delta` — **fill empirically, suggested range [4, 20]**.  
Question: "Which inner square is lighter?" A=left B=right C="Neither — identical"  
(Only 3 answers; put a dummy D or reuse C text.)

---

### Ponzo — 400×420
Two converging lines from vanishing point (200,30) → (40,420) and (360,420).  
Three horizontal bars, all base width 110px centered at x=200:
- A: y=110 (near top / "far"), B: y=230, C: y=350 (near bottom / "close")

**Answer param:** winning bar width += `delta` px each side.  
**Difficulty param:** `delta` — **fill empirically, suggested range [2, 12]**.  
Question: "Which bar is actually the longest?" A/B/C, D="None — all equal"

---

### Hermann Grid — 400×400
5×5 black squares (56px each, 14px gap). Background white.  
Square grid offset: 32px from edges.  
Intersection centers at `x = 32+56+i*70`, `y = 32+56+j*70` for i,j ∈ {0..3} → 16 intersections.

N actual colored dots (r=6 filled circle) placed at N random intersections (N ∈ {0,1,2,3}).

**Answer param:** N (the true dot count, 0–3). Answer index = N (A=0, B=1, C=2, D=3).  
**Difficulty param:** dot color lightness — bright/colored at diff 1, near-white at diff 5. **Fill empirically, suggested Color range [#FF4444 → #EBEBEB]**.  
Question: "How many dots are actually present in this grid?" A=0 B=1 C=2 D=3  
Use a shorter time limit for this one (suggest 10–15s).

---

## How ChallengeGenerator Uses These

For each visual illusion template, pick a random `answer` (0–3 or -1 for none), call the corresponding `IllusionGenerator` method, assign result to `q.artwork`. Example:

```csharp
int answer = Random.Range(0, 4); // or -1 for none
q.artwork = IllusionGenerator.MullerLyer(difficulty, answer);
q.correctAnswerIndex = answer == -1 ? 3 : answer; // D = "none" when -1
```

The `Generate(difficulty)` method stays static — no MonoBehaviour needed since everything is procedural.

---

## Notes
- Each illusion's `answer` param must drive both the geometry AND `correctAnswerIndex` consistently.
- Color difficulty layer (optional, add after geometry works): lerp line colors from distinct hues toward equal-lightness greys as difficulty increases. Use `ColorFromHSV`.
- Animated illusions (Phi phenomenon, Flash-lag) in the existing pool are text-only — leave them, no artwork needed.
