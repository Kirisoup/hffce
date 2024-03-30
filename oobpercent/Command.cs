using System;
using BepInEx;
using Timer;

namespace oobpercent
{
    public partial class Plugin
    {
        static class Cmds
        {
            public static void RegCmds()
            {
                // Hot toggle oob% on/off
                Soup.Regcmds.ToShell("oobmode", "oob", new Action<string>(Toggler), 
                    "Setting oob% mode on/off", 
                    "<on(1), off(0), toggle(t), query(q)>");

                /*DEBUG*/if (__()) Soup.Regcmds.ToShell("_find", "", new Action(OOB.InitLevelInfo));
                /*DEBUG*/if (__()) Soup.Regcmds.ToShell("_human", "", new Action(OOB.InitPlayerInfo));
            }

            // Helper method (only used in Toggler())
            public static bool SyntaxError(string error = null, bool badSyntax = true)
            {
                if (badSyntax)
                print("Command syntax error" + (error.IsNullOrWhiteSpace() ? null : ": " + error) + newline +
                    helpClr + Soup.Regcmds.description_["oobmode"] + "</color>"
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

            bool oldToggleState = enabledOOB;

            switch (vals[0])
            {
                case "on": case "1":
                    enabledOOB = true;
                    break;

                case "off": case "0":
                    enabledOOB = false;
                    break;

                case "toggle": case "t":
                    enabledOOB = !enabledOOB;
                    break;

                case "query": case "q":
                    break;

                default:
                    Cmds.SyntaxError("Unexpected parameters");
                    return;
            }

            bool changed = enabledOOB != oldToggleState;

            // mark run as cheated if enabledOOB is changed
            if (changed && timerLoaded) FindObjectOfType<Speedrun>().SetInvalidType(InvalidType.Cheated);

            print($"{helpClr}oob% mode:</color> " + 
                (enabledOOB ? "on" : "off") + // on/off state
                (changed ? " <#ff0088>(Changed!)</color>" : null)); // If toggle state is changed, show a magenta "(Changed)" string after the on/off state
        }
    }
}