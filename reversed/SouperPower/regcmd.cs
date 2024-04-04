using System;
using System.Reflection;
using System.Collections.Generic;
using Multiplayer;

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

        public static void ToChat(string cmd, string abbr, Action action, string help = null, string val = null)
        {
            help = HelpUtil_(cmd, abbr, help);

            NetChat.RegisterCommand(true, false, cmd, action, help);

            if (!string.IsNullOrEmpty(abbr)) NetChat.RegisterCommand(true, false, abbr, action, null);
        }
        public static void ToChat(string cmd, string abbr, Action<string> action, string help = null, string val = null)
        {
            help = HelpUtil_(cmd, abbr, help, val);

            NetChat.RegisterCommand(true, false, cmd, action, help);

            if (!string.IsNullOrEmpty(abbr)) NetChat.RegisterCommand(true, false, abbr, action, null);
        }

        public static void ModifyHelp(string cmd, string abbr, string help, string val = null)
        {
            try { description_[cmd] = HelpUtil(cmd, abbr, help, val); } catch {}
        }

        private static string HelpUtil(string cmd, string abbr, string help, string val = null)
        {
            if (string.IsNullOrEmpty(help))
            return null;

            return $"{cmd}{( string.IsNullOrEmpty(abbr) ? null : $"({abbr})" )}{( string.IsNullOrEmpty(val) ? null : " " + val )} - {help}";
        }

        private static string HelpUtil_(string cmd, string abbr, string help, string val = null)
        {
            return "/" + HelpUtil(cmd, abbr, help, val);
        }


        public static readonly FieldInfo commandsField = typeof(Shell).GetField("commands", BindingFlags.NonPublic | BindingFlags.Static);
        public static readonly CommandRegistry CommandRegistry_ = (CommandRegistry)commandsField.GetValue(null);

        public static readonly FieldInfo descriptionfield = typeof(CommandRegistry).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly Dictionary<string, string> description_ = (Dictionary<string, string>)descriptionfield.GetValue(CommandRegistry_);

    }
}

