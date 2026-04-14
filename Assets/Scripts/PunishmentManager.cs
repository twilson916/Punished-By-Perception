using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PunishmentManager : MonoBehaviour
{
    public enum PunishmentType { Saturation, HueShift, ColorTint, Blur, LensDistortion, FilmGrain, Vignette, ChromaticAberration };

    // Keep track of how many punishments of each type the user currently has
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

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public Dictionary<PunishmentType, int> GetCurrentPunishValues() {
        return punishmentValues;
    }

    public bool Punish(PunishmentType type, int severity = 1) // higher severity is basically like double or triple punishments
    {
        switch(type)
        {
            case PunishmentType.Saturation:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out ColorAdjustments colorAdjustments);
                    float current = colorAdjustments.saturation.value;
                    float newVal = Mathf.Clamp(current - 33.33f * severity, -100f, 0f);

                    if (Mathf.Abs(current - newVal) < 1e-6) return false;

                    colorAdjustments.saturation.Override(newVal); // -100 = full grayscale
                    punishmentValues[PunishmentType.Saturation] += severity;
                }
                break;

            case PunishmentType.HueShift:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out ColorAdjustments colorAdjustments);
                    float current = colorAdjustments.hueShift.value;
                    float newVal = Mathf.Clamp(current - 15f * severity, -180f, 180f);

                    if (Mathf.Abs(current - newVal) < 1e-6) return false;

                    colorAdjustments.hueShift.Override(newVal); // -180 = opposite side of color wheel
                    punishmentValues[PunishmentType.HueShift] += severity;
                }
                break;
            case PunishmentType.ColorTint:
                {
                    Color startColor = Color.white; // no tint
                    Color endColor = new Color(0.1f, 0.02f, 0.02f); // dark red tint
                    int numSteps = 4;

                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out ColorAdjustments colorAdjustments);

                    punishmentValues[PunishmentType.ColorTint] = Mathf.Clamp(punishmentValues[PunishmentType.ColorTint] + severity, 0, numSteps);

                    int steps = punishmentValues[PunishmentType.ColorTint];
                    float t = (float)steps / numSteps;
                    Color tint = Color.Lerp(startColor, endColor, t);

                    float diff = Vector4.Distance(tint, endColor);
                    if (diff < 1e-6f) return false;

                    colorAdjustments.colorFilter.Override(tint);

                }
                break;
            case PunishmentType.Blur:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out DepthOfField dof);
                    dof.mode.Override(DepthOfFieldMode.Bokeh);
                    float current = dof.focalLength.value;
                    float newVal = Mathf.Clamp(current + 40f * severity, 0f, 300f);

                    if (Mathf.Abs(current - newVal) < 1e-6) return false;

                    dof.focalLength.Override(newVal);
                    punishmentValues[PunishmentType.Blur] += severity;
                }
                break;
            case PunishmentType.LensDistortion:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out LensDistortion lensDistortion);
                    float current = lensDistortion.intensity.value;
                    float newVal = Mathf.Clamp(current + 0.25f * severity, 0f, 1f);

                    if (Mathf.Abs(current - newVal) < 1e-6) return false;

                    lensDistortion.intensity.Override(newVal);
                    punishmentValues[PunishmentType.LensDistortion] += severity;
                }
                break;
            case PunishmentType.FilmGrain:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out FilmGrain filmGrain);
                    float current = filmGrain.intensity.value;
                    float newVal = 1.0f;

                    if (Mathf.Abs(current - newVal) < 1e-6f) return false;

                    filmGrain.intensity.Override(newVal);
                    punishmentValues[PunishmentType.FilmGrain] = 1; //binary punishment, no severity
                }
                break;
            case PunishmentType.Vignette:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out Vignette vignette);
                    float current = vignette.intensity.value;
                    float newVal = 1.0f
                        ;
                    if (Mathf.Abs(current - newVal) < 1e-6f) return false;

                    vignette.intensity.Override(newVal);
                    punishmentValues[PunishmentType.Vignette] = 1; //binary punishment, no severity
                }
                break;
            case PunishmentType.ChromaticAberration:
                {
                    Volume volume = GetComponent<Volume>();
                    volume.profile.TryGet(out ChromaticAberration chromaticAberration);
                    float current = chromaticAberration.intensity.value;
                    float newVal = 1.0f;

                    if (Mathf.Abs(current - newVal) < 1e-6f) return false;

                    chromaticAberration.intensity.Override(newVal);
                    punishmentValues[PunishmentType.ChromaticAberration] = 1; //binary punishment, no severity
                }
                break;
        }
        return true;
    }
}
