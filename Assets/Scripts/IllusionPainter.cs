using UnityEngine;

public static class IllusionPainter
{
    public static void Fill(Texture2D tex, Color c)
    {
        Color[] pixels = new Color[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        tex.SetPixels(pixels);
    }

    public static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color c)
    {
        x = Mathf.Clamp(x, 0, tex.width);
        y = Mathf.Clamp(y, 0, tex.height);
        w = Mathf.Clamp(w, 0, tex.width - x);
        h = Mathf.Clamp(h, 0, tex.height - y);

        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        tex.SetPixels(x, y, w, h, pixels);
    }

    public static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color c, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            DrawThickPoint(tex, x0, y0, c, thickness);
            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public static void DrawArc(Texture2D tex, int cx, int cy, int r, float startDeg, float endDeg, Color c, int thickness)
    {
        // Simple trigonometric arc drawing
        float step = 1f / r; // Radians per step
        float startRad = startDeg * Mathf.Deg2Rad;
        float endRad = endDeg * Mathf.Deg2Rad;

        for (float theta = startRad; theta <= endRad; theta += step)
        {
            int x = cx + Mathf.RoundToInt(r * Mathf.Cos(theta));
            int y = cy + Mathf.RoundToInt(r * Mathf.Sin(theta));
            DrawThickPoint(tex, x, y, c, thickness);
        }
    }

    private static void DrawThickPoint(Texture2D tex, int px, int py, Color c, int thickness)
    {
        int r = thickness / 2;
        int rSq = r * r;
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= rSq)
                {
                    int drawX = px + x;
                    int drawY = py + y;
                    if (drawX >= 0 && drawX < tex.width && drawY >= 0 && drawY < tex.height)
                    {
                        tex.SetPixel(drawX, drawY, c);
                    }
                }
            }
        }
    }

    public static Sprite MakeSprite(Texture2D tex)
    {
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    public static Color ColorFromHSV(float h, float s, float v)
    {
        // Unity actually has this built-in in modern versions, but fulfilling the spec:
        return Color.HSVToRGB(h, s, v);
    }
}