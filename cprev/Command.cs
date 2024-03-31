using System;
using BepInEx;
using Timer;
using System.Reflection;

namespace cprev
{
    public partial class Plugin
    {
        static class Cmds
        {
            public static void RegCmds()
            {
                // Hot toggle oob% on/off
                Soup.Regcmds.ToShell("cprevmode", "cpr", new Action<string>(Toggler), 
                    "Setting cp% reversed mode on/off", 
                    "<on(1), off(0), toggle(t), query(q)>");
            }

            // Helper method (only used in Toggler())
            public static bool SyntaxError(string error = null, bool badSyntax = true)
            {
                if (badSyntax)
                print("Command syntax error" + (error.IsNullOrWhiteSpace() ? null : ": " + error) + newline +
                    helpClr + Soup.Regcmds.description_["cprevmode"] + "</color>"
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

            bool oldToggleState = enabledCPR;

            switch (vals[0])
            {
                case "on": case "1":
                    enabledCPR = true;
                    break;

                case "off": case "0":
                    enabledCPR = false;
                    break;

                case "toggle": case "t":
                    enabledCPR = !enabledCPR;
                    break;

                case "query": case "q":
                    break;

                default:
                    Cmds.SyntaxError("Unexpected parameters");
                    return;
            }

            bool changed = enabledCPR != oldToggleState;
            
            print($"{helpClr}cp% reversed mode:</color> " + 
                (enabledCPR ? "on" : "off") + // on/off state
                (changed ? " <#ff0088>(Changed!)</color>" : null)); // If toggle state is changed, show a magenta "(Changed)" string after the on/off state
        }
    }
}