using System;
using System.Reflection;
using System.Collections.Generic;

public partial class Soup
{
    public class Regcmds
    {
        public static void ToShell(string cmd, string abbr, Action action, string help = null)
        {
            help = HelpUtil(cmd, abbr, help);

            Shell.RegisterCommand(cmd, action, help);

            if (!string.IsNullOrEmpty(abbr)) Shell.RegisterCommand(abbr, action, null);
        }
        public static void ToShell(string cmd, string abbr, Action<string> action, string help = null, string val = null)
        {
            help = HelpUtil(cmd, abbr, help, val);

            Shell.RegisterCommand(cmd, action, help);

            if (!string.IsNullOrEmpty(abbr)) Shell.RegisterCommand(abbr, action, null);
        }

        private static string HelpUtil(string cmd, string abbr, string help, string val = null)
        {
            if (string.IsNullOrEmpty(help))
            return null;

            return $"{cmd}{( string.IsNullOrEmpty(abbr) ? null : $"({abbr})" )}{( string.IsNullOrEmpty(val) ? null : " " + val )} - {help}";
        }

        public static readonly FieldInfo commandsField = typeof(Shell).GetField("commands", BindingFlags.NonPublic | BindingFlags.Static);
        public static readonly CommandRegistry CommandRegistry_ = (CommandRegistry)commandsField.GetValue(null);

        public static readonly FieldInfo descriptionfield = typeof(CommandRegistry).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly Dictionary<string, string> description_ = (Dictionary<string, string>)descriptionfield.GetValue(CommandRegistry_);

    }
}

