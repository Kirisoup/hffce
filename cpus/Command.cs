// using System;
// using BepInEx;

// namespace cpus
// {
//         public class Commands
//         {
//                 public static void RegCmds()
//                 {
//                         // Hot toggle oob% on/off
//                         Soup.Regcmds.ToShell("cprevmode", "cpr", new Action<string>(Toggler), 
//                                 "Setting cp% reversed mode on/off", 
//                                 "<on(1), off(0), toggle(t), query(q)>");
//                 }

//                 // Helper method (only used in Toggler())
//                 public static bool SyntaxError(string error = null, bool badSyntax = true)
//                 {
//                         if (badSyntax)
//                         Shell.Print("Command syntax error" + (error.IsNullOrWhiteSpace() ? null : ": " + error) + newline + Help());

//                         return badSyntax;
//                 }

//                 public static string Help() => helpClr + Soup.Regcmds.description_["cprevmode"] + "</color>";

//                 static readonly string newline = Environment.NewLine;
//                 static readonly string helpClr = new CommandRegistry(null, false).helpColor;


//                 // Universal toggler
//                 // if toggles off, everything (beside the toggler itself) is expected to get killed
//                 // the run becomes invalid if the toggle state is changed
//                 //
//                 // Accepted arguments:
//                 //   `on` and `1`
//                 //   `off` and `0`
//                 //   `toggle` and `t`
//                 //   `query` and `q`
//                 public static void Toggler(String param)
//                 {
//                         // if (SyntaxError("Parameter cannot be empty", param.IsNullOrWhiteSpace())) return;

//                         string[] vals;

//                         string val;

//                         if (!param.IsNullOrWhiteSpace()) 
//                         {
//                                 vals = param.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
//                                 val = vals[0];

//                                 if (SyntaxError("Too many parameters", vals.Length != 1)) return;
//                         }

//                         else val = "";

//                         var enabled = Plugin.enabledCPR;

//                         bool prev = enabled;
//                         bool changed;

//                         switch (val)
//                         {
//                                 case "on": case "1": enabled = true;
//                                 break;

//                                 case "off": case "0": enabled = false;
//                                 break;

//                                 case "toggle": case "t": enabled = !enabled;
//                                 break;

//                                 case "query": case "q":
//                                 break;

//                                 case "": Shell.Print(Help());
//                                 break;

//                                 default: SyntaxError("Unexpected parameters");
//                                 return;
//                         }

//                         if (changed = enabled != prev) Plugin.enabledCPR = enabled;
                        
//                         Shell.Print($"{helpClr}cp% reversed mode:</color> " + 
//                                 (enabled ? "on" : "off") + // on/off state
//                                 (changed ? " <#ff0088>(Changed!)</color>" : null)); // If toggle state is changed, show a magenta "(Changed)" string after the on/off state
//                 }
//         }
// }