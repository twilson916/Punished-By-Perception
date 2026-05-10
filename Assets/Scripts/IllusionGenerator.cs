using UnityEngine;

public static class IllusionGenerator
{
    private static Color black = Color.black;
    private static Color white = Color.white;
    private static Color grey = Color.gray;

    // --- THE MASTER RESOLUTION DIAL ---
    // 1 = 400px, 2 = 800px, 3 = 1200px, etc.
    public static int RESOLUTION_SCALE = 3;

    // Helper to get difficulty percentage 0.0 (diff 1) to 1.0 (diff 5)
    private static float DiffT(int difficulty) => Mathf.Clamp01((difficulty - 1) / 4f);

    public static Sprite MullerLyer(int difficulty, int answer)
    {
        Texture2D tex = new Texture2D(400 * RESOLUTION_SCALE, 350 * RESOLUTION_SCALE, TextureFormat.RGBA32, false);
        IllusionPainter.Fill(tex, white);

        // Diff 1 = 15px difference (obvious). Diff 5 = 2px difference (subtle).
        int delta = Mathf.RoundToInt(Mathf.Lerp(12f, 2f, DiffT(difficulty)));
        int[] yPos = { 260, 175, 90 }; // A (top), B (mid), C (bottom)
        int baseLen = 220;
        int halfBase = baseLen / 2;

        for (int i = 0; i < 3; i++)
        {
            int lineLen = halfBase;
            if (i == answer) lineLen += delta;

            int xStart = 200 - lineLen;
            int xEnd = 200 + lineLen;
            IllusionPainter.DrawLine(tex, xStart, yPos[i], xEnd, yPos[i], black, 3);

            // Arrows: i=0 is inward, i>0 is outward
            int arrowDir = (i == 0) ? 1 : -1;
            int armX = 18 * arrowDir; // Approx 22px length at ~35 degrees
            int armY = 13;

            // Left arrow
            IllusionPainter.DrawLine(tex, xStart, yPos[i], xStart + armX, yPos[i] + armY, black, 3);
            IllusionPainter.DrawLine(tex, xStart, yPos[i], xStart + armX, yPos[i] - armY, black, 3);

            // Right arrow
            IllusionPainter.DrawLine(tex, xEnd, yPos[i], xEnd - armX, yPos[i] + armY, black, 3);
            IllusionPainter.DrawLine(tex, xEnd, yPos[i], xEnd - armX, yPos[i] - armY, black, 3);
        }

        return IllusionPainter.MakeSprite(tex);
    }

    public static Sprite Poggendorff(int difficulty, int answer)
    {
        Texture2D tex = new Texture2D(400 * RESOLUTION_SCALE, 420 * RESOLUTION_SCALE, TextureFormat.RGBA32, false);
        IllusionPainter.Fill(tex, white);

        IllusionPainter.DrawRect(tex, 155, 0, 90, 420, grey);

        // Diff 1 = 28px offset. Diff 5 = 6px offset.
        int offsetAmt = Mathf.RoundToInt(Mathf.Lerp(28f, 6f, DiffT(difficulty)));
        int[] yStarts = { 290, 185, 80 }; // A(top), B(mid), C(bottom)
        float slope = 0.22f;

        for (int i = 0; i < 3; i++)
        {
            // Left segments
            int yLeftEnd = yStarts[i] + Mathf.RoundToInt(155 * slope);
            IllusionPainter.DrawLine(tex, 0, yStarts[i], 155, yLeftEnd, black, 3);

            // Right segments
            int yCollinearStart = yStarts[i] + Mathf.RoundToInt(245 * slope);
            int yRightEnd = yStarts[i] + Mathf.RoundToInt(400 * slope);

            int actualYStart = yCollinearStart;
            int actualYEnd = yRightEnd;

            // If not the correct answer (and we are picking an answer), apply offset
            if (answer != i && answer != -1)
            {
                actualYStart += offsetAmt;
                actualYEnd += offsetAmt;
            }
            else if (answer == -1) // All none-connecting
            {
                actualYStart += offsetAmt;
                actualYEnd += offsetAmt;
            }

            IllusionPainter.DrawLine(tex, 245, actualYStart, 400, actualYEnd, black, 3);
        }

        return IllusionPainter.MakeSprite(tex);
    }

    public static Sprite Hering(int difficulty, int answer)
    {
        Texture2D tex = new Texture2D(400 * RESOLUTION_SCALE, 400 * RESOLUTION_SCALE, TextureFormat.RGBA32, false);
        IllusionPainter.Fill(tex, white);

        Color darkBlue = new Color(0, 0, 0.5f);
        for (int i = 0; i < 40; i++)
        {
            float angle = i * 9f;
            int dx = Mathf.RoundToInt(300 * Mathf.Cos(angle * Mathf.Deg2Rad));
            int dy = Mathf.RoundToInt(300 * Mathf.Sin(angle * Mathf.Deg2Rad));
            IllusionPainter.DrawLine(tex, 200, 200, 200 + dx, 200 + dy, darkBlue, 2);
        }

        // Diff 1 = R:130 (very curved), Diff 5 = R:1800 (barely curved)
        int R = Mathf.RoundToInt(Mathf.Lerp(130f, 1800f, DiffT(difficulty)));
        int[] xPos = { 120, 200, 280 }; // A, B, C

        for (int i = 0; i < 3; i++)
        {
            if (i == answer)
            {
                IllusionPainter.DrawLine(tex, xPos[i], 10, xPos[i], 390, Color.red, 3); // Straight
            }
            else
            {
                // Curve them outward relative to center
                int cx = (xPos[i] < 200) ? xPos[i] + R : (xPos[i] > 200 ? xPos[i] - R : xPos[i] + R);
                float maxAngle = Mathf.Asin(190f / R) * Mathf.Rad2Deg; // 190 is half height of line (390-10)/2
                float startDeg = 180 - maxAngle;
                float endDeg = 180 + maxAngle;

                if (xPos[i] >= 200)
                {
                    startDeg = -maxAngle;
                    endDeg = maxAngle;
                }

                IllusionPainter.DrawArc(tex, cx, 200, R, startDeg, endDeg, Color.red, 3);
            }
        }

        return IllusionPainter.MakeSprite(tex);
    }

    public static Sprite Ebbinghaus(int difficulty, int answer)
    {
        Texture2D tex = new Texture2D(500 * RESOLUTION_SCALE, 200 * RESOLUTION_SCALE, TextureFormat.RGBA32, false);
        IllusionPainter.Fill(tex, white);

        // Diff 1 = 8px bump. Diff 5 = 2px bump.
        int delta = Mathf.RoundToInt(Mathf.Lerp(8f, 2f, DiffT(difficulty)));
        int[] xCenters = { 85, 250, 415 };
        int[] sRadius = { 10, 30, 10 };
        int[] rDist = { 38, 65, 38 };
        int baseR = 22;

        for (int i = 0; i < 3; i++)
        {
            int cx = xCenters[i];
            int currentR = baseR + ((i == answer) ? delta : 0);

            // Draw center circle
            IllusionPainter.DrawFilledCircle(tex, cx, 100, currentR, new Color(1f, 0.5f, 0f));

            // Draw surrounding circles
            for (int j = 0; j < 6; j++)
            {
                float angle = j * 60f * Mathf.Deg2Rad;
                int sx = cx + Mathf.RoundToInt(rDist[i] * Mathf.Cos(angle));
                int sy = 100 + Mathf.RoundToInt(rDist[i] * Mathf.Sin(angle));
                IllusionPainter.DrawFilledCircle(tex, sx, sy, sRadius[i], Color.cyan);
            }
        }

        return IllusionPainter.MakeSprite(tex);
    }

    public static Sprite BrightnessContrast(int difficulty, int answer)
    {
        Texture2D tex = new Texture2D(400 * RESOLUTION_SCALE, 300 * RESOLUTION_SCALE, TextureFormat.RGBA32, false);
        Color leftOuter = new Color(0.1f, 0.1f, 0.1f); // #1A1A1A
        Color rightOuter = new Color(0.9f, 0.9f, 0.9f); // #E5E5E5

        IllusionPainter.DrawRect(tex, 0, 0, 200, 300, leftOuter);
        IllusionPainter.DrawRect(tex, 200, 0, 200, 300, rightOuter);
        IllusionPainter.DrawLine(tex, 200, 0, 200, 300, grey, 2); // divider

        // Diff 1 = 30/255. Diff 5 = 5/255.
        float delta = Mathf.Lerp(20f, 4f, DiffT(difficulty)) / 255f;
        float baseGrey = 136f / 255f; // #888888

        float leftGrey = baseGrey + ((answer == 0) ? delta : 0);
        float rightGrey = baseGrey + ((answer == 1) ? delta : 0);

        IllusionPainter.DrawRect(tex, 60, 110, 80, 80, new Color(leftGrey, leftGrey, leftGrey));
        IllusionPainter.DrawRect(tex, 260, 110, 80, 80, new Color(rightGrey, rightGrey, rightGrey));

        return IllusionPainter.MakeSprite(tex);
    }

    public static Sprite Ponzo(int difficulty, int answer)
    {
        Texture2D tex = new Texture2D(400 * RESOLUTION_SCALE, 420 * RESOLUTION_SCALE, TextureFormat.RGBA32, false);
        IllusionPainter.Fill(tex, white);

        IllusionPainter.DrawLine(tex, 200, 30, 40, 420, black, 4);
        IllusionPainter.DrawLine(tex, 200, 30, 360, 420, black, 4);

        // Diff 1 = 12px bump. Diff 5 = 2px bump.
        int delta = Mathf.RoundToInt(Mathf.Lerp(12f, 2f, DiffT(difficulty)));
        int[] yPos = { 350, 230, 110 }; // A(top visual, lower Y), B(mid), C(bottom visual, higher Y)
        int baseW = 110;

        for (int i = 0; i < 3; i++)
        {
            int w = baseW + ((i == answer) ? delta * 2 : 0);
            int xStart = 200 - w / 2;
            IllusionPainter.DrawLine(tex, xStart, yPos[i], xStart + w, yPos[i], Color.red, 6);
        }

        return IllusionPainter.MakeSprite(tex);
    }

    public static Sprite HermannGrid(int difficulty, int answer)
    {
        Texture2D tex = new Texture2D(400 * RESOLUTION_SCALE, 400 * RESOLUTION_SCALE, TextureFormat.RGBA32, false);
        IllusionPainter.Fill(tex, white);

        // Draw 5x5 black squares
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int x = 32 + i * 70;
                int y = 32 + j * 70;
                IllusionPainter.DrawRect(tex, x, y, 56, 56, black);
            }
        }

        int dotCount = Mathf.Clamp(answer, 0, 3); // 0-3 dots

        // Diff 1 = Medium Gray (Obvious against white)
        // Diff 5 = Very Light Gray (Practically invisible, perfectly mimics the optical illusion)
        Color dotColor = Color.Lerp(new Color(0.6f, 0.6f, 0.6f), new Color(0.96f, 0.96f, 0.96f), DiffT(difficulty));

        // Place N dots randomly at intersections
        for (int n = 0; n < dotCount; n++)
        {
            int rx = Random.Range(0, 4);
            int ry = Random.Range(0, 4);
            int cx = 32 + 56 + rx * 70 + 7; // +7 to center in the 14px gap
            int cy = 32 + 56 + ry * 70 + 7;
            IllusionPainter.DrawFilledCircle(tex, cx, cy, 6, dotColor);
        }

        return IllusionPainter.MakeSprite(tex);
    }

    public static Sprite Jastrow(int difficulty, int answer)
    {
        // Create the canvas
        Texture2D tex = new Texture2D(400 * RESOLUTION_SCALE, 400 * RESOLUTION_SCALE, TextureFormat.RGBA32, false);
        IllusionPainter.Fill(tex, Color.white);

        // Difficulty Scaling: 
        // Diff 1 = 15 degrees wider (very obvious)
        // Diff 5 = 2.5 degrees wider (requires squinting to notice)
        float bonusAngle = Mathf.Lerp(15f, 2.5f, DiffT(difficulty));

        // X-centers for the circles. Because the arcs curve to the left (like a 'C'), 
        // the mathematical centers are pushed far to the right side of the canvas.
        // This staggers them Left, Middle, and Right.
        int[] cxOffsets = { 280, 360, 440 }; // A (Left), B (Middle), C (Right)

        int R = 200;        // Larger radius for the horizontal view
        int thickness = 40; // Slightly thicker

        for (int i = 0; i < 3; i++)
        {
            // 180 degrees is the exact left side of a circle.
            // Sweeping from 135 to 225 draws a perfect "C" shape.
            float startDeg = 135f;
            float endDeg = 225f;

            if (i == answer)
            {
                // Make this specific shape actually wider
                startDeg -= bonusAngle;
                endDeg += bonusAngle;
            }

            // All of them stay perfectly centered vertically (Y = 200)
            IllusionPainter.DrawArc(tex, cxOffsets[i], 200, R, startDeg, endDeg, Color.black, thickness);
        }

        return IllusionPainter.MakeSprite(tex);
    }
}