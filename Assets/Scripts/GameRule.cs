using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RuleManager;

//enum will be used for the dictionary/map in rule manager

public enum RuleResultType
{
    Safe,
    Punishment,
    Challenge,
    Random
}

public class GameRule : MonoBehaviour
{
    public RuleName ruleName;
    public string description; //shown to user in rulebook ui
    public bool isDiscovered = false;
    public RuleName parent = RuleName.None; //parent of rule (need to have unlocked parent to unlock child rule)

    public Color doorColor;
    public Color handleColor;

    public RuleResultType result;

    public void resetRule()
    {
        isDiscovered = false;
    }

    public enum RuleName
    {
        None,

        GeGa,
        GeGe,
        GeYw,
        GeRd,
        GeBu,
        GeCy,
        GeMg,
        GeBk,
        GeWh,

        YwGa,
        YwGe,
        YwYw,
        YwRd,
        YwBu,
        YwCy,
        YwMg,
        YwBk,
        YwWh,

        RdGa,
        RdGe,
        RdYw,
        RdRd,
        RdBu,
        RdCy,
        RdMg,
        RdBk,
        RdWh,

        CyGa,
        CyGe,
        CyYw,
        CyRd,
        CyBu,
        CyCy,
        CyMg,
        CyBk,
        CyWh,

        BkGa,
        BkGe,
        BkYw,
        BkRd,
        BkBu,
        BkCy,
        BkMg,
        BkBk,
        BkWh,

        MgGa,
        MgGe,
        MgYw,
        MgRd,
        MgBu,
        MgCy,
        MgMg,
        MgBk,
        MgWh,

        GaGa,
        GaGe,
        GaYw,
        GaRd,
        GaBu,
        GaCy,
        GaMg,
        GaBk,
        GaWh,
    }
}
