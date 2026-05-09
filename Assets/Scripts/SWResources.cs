using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Definitions/Resources (mainly just enums)
namespace MyGame.Resources
{
    public enum GameState { Exploring, FakeEnding, Ending }; //placeholders for now

    public enum RoomNumber { MinusOne = 0, One = 1, Two = 2, Three = 3 }; // Minus one rooms to handle door changes behind room 1

    public enum DoorPos { Left = 0, Middle = 1, Right = 2 };

    /* For colors use
    Color.red
    Color.green
    Color.blue
    Color.white
    Color.black
    Color.yellow
    Color.cyan
    Color.magenta
    Color.gray
    */

    public static class ColorUtils
    {
        private static readonly Color[] predefinedColors = new Color[]
        {
        Color.red,
        Color.green,
        Color.blue,
        Color.white,
        Color.black,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        Color.gray
        };

        public static Color ParseColor(string name)
        {
            switch (name.ToLower())
            {
                case "red": return Color.red;
                case "green": return Color.green;
                case "blue": return Color.blue;
                case "white": return Color.white;
                case "black": return Color.black;
                case "yellow": return Color.yellow;
                case "cyan": return Color.cyan;
                case "magenta": return Color.magenta;
                case "gray": return Color.gray;
                default:
                    Debug.LogWarning($"Unknown color: {name}, defaulting to white");
                    return Color.white;
            }
        }

        public static Color GetRandColor()
        {
            int index = Random.Range(0, predefinedColors.Length);
            return predefinedColors[index];
        }
    }

    public struct RoomColors
    {
        // Array for multiple doors
        public Color[] doors;

        // Array for door handles, must match doors length
        public Color[] doorHandles;

        public Color? wall; // ? -> nullable
        public Color? floor;
        public Color? ceiling;
        public Color? trim;

        // Optional constructor for easy initialization
        public RoomColors(Color[] doors, Color[] doorHandles, Color? wall = null, Color? floor = null, Color? ceiling = null, Color? trim = null)
        {
            this.doors = doors;
            this.doorHandles = doorHandles;
            this.wall = wall;
            this.floor = floor;
            this.ceiling = ceiling;
            this.trim = trim;
        }

        //USAGE: 
        /*
         RoomColors room = new RoomColors
         (
            wall: Color.gray,
            floor: Color.white,
            ceiling: Color.gray,
            doors: new Color[] { Color.red, Color.green, Color.blue },
            doorHandles: new Color[] { Color.yellow, Color.black, color.white },
            trim: Color.black
        );
        */
    }

}