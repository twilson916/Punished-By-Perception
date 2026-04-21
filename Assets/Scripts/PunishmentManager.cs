using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PunishmentManager : MonoBehaviour
{
    public enum PunishmentType { Saturation, HueShift, ColorTint, Blur, LensDistortion, FilmGrain, Vignette, ChromaticAberration };

    private Dictionary<PunishmentType, int> punishmentValues = new()
    {
        { PunishmentType.Saturation, 0 },
        { PunishmentType.HueShift, 0 },
        { PunishmentType.ColorTint, 0 },
        { PunishmentType.Blur, 0 },
        { PunishmentType.LensDistortion, 0 },
        { PunishmentType.FilmGrain, 0 },
        { PunishmentType.Vignette, 0 },
        { PunishmentType.ChromaticAberration, 0 }
    };

    // Max stacks for each type before it's fully maxed out
    private static readonly Dictionary<PunishmentType, int> maxStacks = new()
    {
        { PunishmentType.Saturation, 3 },       // 3 * 33.33 = ~100
        { PunishmentType.HueShift, 12 },         // 12 * 15 = 180
        { PunishmentType.ColorTint, 4 },
        { PunishmentType.Blur, 7 },              // 7 * 40 = 280 (under 300 cap)
        { PunishmentType.LensDistortion, 4 },    // 4 * 0.25 = 1.0
        { PunishmentType.FilmGrain, 1 },
        { PunishmentType.Vignette, 1 },
        { PunishmentType.ChromaticAberration, 1 }
    };

    public Dictionary<PunishmentType, int> GetCurrentPunishValues()
    {
        return new Dictionary<PunishmentType, int>(punishmentValues);
    }

    public bool IsMaxed(PunishmentType type)
    {
        return punishmentValues[type] >= maxStacks[type];
    }

    public bool IsAllMaxed()
    {
        return punishmentValues.All(kvp => kvp.Value >= maxStacks[kvp.Key]);
    }

    /// High-level: randomly distributes severity across available punishment types.
    /// Returns true if full severity was applied, false otherwise.
    /// If forcePunish is true, applies as much as possible.
    public bool Punish(int severity, bool forcePunish = false)
    {
        int remaining = severity;

        // Get all types that aren't maxed and shuffle them
        List<PunishmentType> available = punishmentValues
            .Where(kvp => kvp.Value < maxStacks[kvp.Key])
            .Select(kvp => kvp.Key)
            .ToList();

        Shuffle(available);

        // Spread one unit across random available types
        foreach (var type in available)
        {
            if (remaining <= 0) break;

            int applied = Punish(type, 1);
            remaining -= applied;
        }

        // If force and still remaining, loop again on anything that has room
        if (forcePunish && remaining > 0)
        {
            available = punishmentValues
                .Where(kvp => kvp.Value < maxStacks[kvp.Key])
                .Select(kvp => kvp.Key)
                .ToList();

            Shuffle(available);

            foreach (var type in available)
            {
                if (remaining <= 0) break;

                int applied = Punish(type, remaining);
                remaining -= applied;
            }
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"Could not apply full punishment. Applied {severity - remaining}/{severity}");
            return false;
        }

        return true;
    }

    /// Low-level: applies punishment to a specific type.
    /// Returns the number of severity units actually applied.
    public int Punish(PunishmentType type, int severity = 1)
    {
        // Cap severity to what's actually available
        int room = maxStacks[type] - punishmentValues[type];
        int actualSeverity = Mathf.Min(severity, room);

        if (actualSeverity <= 0) return 0;

        switch (type)
        {
            case PunishmentType.Saturation:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out ColorAdjustments colorAdjustments);
                    float newVal = Mathf.Clamp(colorAdjustments.saturation.value - 33.33f * actualSeverity, -100f, 0f);
                    colorAdjustments.saturation.Override(newVal);
                    punishmentValues[type] += actualSeverity;
                }
                break;

            case PunishmentType.HueShift:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out ColorAdjustments colorAdjustments);
                    float newVal = Mathf.Clamp(colorAdjustments.hueShift.value - 15f * actualSeverity, -180f, 180f);
                    colorAdjustments.hueShift.Override(newVal);
                    punishmentValues[type] += actualSeverity;
                }
                break;

            case PunishmentType.ColorTint:
                {
                    Color startColor = Color.white;
                    Color endColor = new Color(0.1f, 0.02f, 0.02f);

                    punishmentValues[type] = Mathf.Min(punishmentValues[type] + actualSeverity, maxStacks[type]);

                    float t = (float)punishmentValues[type] / maxStacks[type];
                    Color tint = Color.Lerp(startColor, endColor, t);

                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out ColorAdjustments colorAdjustments);
                    colorAdjustments.colorFilter.Override(tint);
                }
                break;

            case PunishmentType.Blur:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out DepthOfField dof);
                    dof.mode.Override(DepthOfFieldMode.Bokeh);
                    float newVal = Mathf.Clamp(dof.focalLength.value + 40f * actualSeverity, 0f, 300f);
                    dof.focalLength.Override(newVal);
                    punishmentValues[type] += actualSeverity;
                }
                break;

            case PunishmentType.LensDistortion:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out LensDistortion lensDistortion);
                    float newVal = Mathf.Clamp(lensDistortion.intensity.value + 0.25f * actualSeverity, 0f, 1f);
                    lensDistortion.intensity.Override(newVal);
                    punishmentValues[type] += actualSeverity;
                }
                break;

            case PunishmentType.FilmGrain:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out FilmGrain filmGrain);
                    filmGrain.intensity.Override(1.0f);
                    punishmentValues[type] = 1;
                }
                break;

            case PunishmentType.Vignette:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out Vignette vignette);
                    vignette.intensity.Override(1.0f);
                    punishmentValues[type] = 1;
                }
                break;

            case PunishmentType.ChromaticAberration:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out ChromaticAberration chromaticAberration);
                    chromaticAberration.intensity.Override(1.0f);
                    punishmentValues[type] = 1;
                }
                break;
        }

        return actualSeverity;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}