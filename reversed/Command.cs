using System;
using BepInEx;
using Timer;
using System.Reflection;
using HumanAPI;
using UnityEngine;
using Human_Mod;

namespace reversed
{
    public partial class Plugin
    {
        static class Cmds
        {
            public static void RegCmds()
            {
                // Hot toggle oob% on/off
                Soup.Regcmds.ToShell("reversedmode", "rev", new Action<string>(Toggler), 
                    "Setting reversed% mode on/off", 
                    "<on(1), off(0), toggle(t), query(q)>");

                Soup.Regcmds.ToShell("throw", "", new Action<string>(ThrowPlayer));


            }

            static void objdb()
            {
                print(Instantiate<GameObject>(Human_Mod.Main.selected));
            }

            // Helper method (only used in Toggler())
            public static bool SyntaxError(string error = null, bool badSyntax = true)
            {
                if (badSyntax)
                print("Command syntax error" + (error.IsNullOrWhiteSpace() ? null : ": " + error) + newline +
                    helpClr + Soup.Regcmds.description_["reversedmode"] + "</color>"
                );

                return badSyntax;
            }
        }

        // string stuff (used in command output)
        static readonly string newline = Environment.NewLine;
        static readonly string helpClr = new CommandRegistry(null, false).helpColor;


        // Universal toggler
        // if toggles off, everything (beside the toggler itself) is expected to get killed
        // the run becomes invalid if the toggle state is changed
        //
        // Accepted arguments:
        //   `on` and `1`
        //   `off` and `0`
        //   `toggle` and `t`
        //   `query` and `q`
        public static void Toggler(String param)
        {
            if (Cmds.SyntaxError("Parameter cannot be empty", param.IsNullOrWhiteSpace())) return;

            string[] vals = param.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (Cmds.SyntaxError("Too many parameters", vals.Length != 1)) return;

            bool oldToggleState = enabledRev;

            switch (vals[0])
            {
                case "on": case "1":
                    enabledRev = true;
                    break;

                case "off": case "0":
                    enabledRev = false;
                    break;

                case "toggle": case "t":
                    enabledRev = !enabledRev;
                    break;

                case "query": case "q":
                    break;

                case "alwaysSpawnInside": case "asi":
                    alwaysSpawnInside = !alwaysSpawnInside;
                    print($"{helpClr}reversed% alwaysSpawnInside:</color> " + alwaysSpawnInside + " <#ff0088>(Changed!)</color>");
                    break;

                default:
                    Cmds.SyntaxError("Unexpected parameters");
                    return;
            }

            bool changed = enabledRev != oldToggleState;
            
            print($"{helpClr}reversed% mode:</color> " + 
                (enabledRev ? "on" : "off") + // on/off state
                (changed ? " <#ff0088>(Changed!)</color>" : null)); // If toggle state is changed, show a magenta "(Changed)" string after the on/off state
        }
    }
}