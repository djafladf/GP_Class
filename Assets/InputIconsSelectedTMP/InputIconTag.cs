using System;

/// <summary>
/// Unity Input System control path -> TMP sprite tag.
/// Uses the SAME sprite names for White and Black TMP Sprite Assets.
/// Only controls included in this filtered package are emitted.
/// </summary>
public static class InputIconTag
{
    public static bool TryGetTag(string path, out string tag)
    {
        tag = null;
        if (!TryGetName(path, out string name)) return false;
        tag = "<sprite name=\"" + name + "\">";
        return true;
    }

    public static bool TryGetName(string path, out string name)
    {
        name = null;
        if (string.IsNullOrEmpty(path)) return false;
        path = path.ToLowerInvariant();
        int slash = path.LastIndexOf('/');
        if (slash < 0 || slash + 1 >= path.Length) return false;
        string key = path.Substring(slash + 1);
        if (path.Contains("<mouse>"))
        {
            switch (key)
            {
                case "leftbutton": key = "lb"; break;
                case "rightbutton": key = "rb"; break;
                case "middlebutton": key = "mb"; break;
                case "scroll": key = "scroll"; break;
                default: return false;
            }
            name = "mouse_transparent_" + key;
            return true;
        }
        if (!path.Contains("<keyboard>")) return false;
        string type;
        if (key.Length == 1 && key[0] >= 'a' && key[0] <= 'z')
            type = "letter";
        else if (key.StartsWith("digit") && key.Length == 6 && key[5] >= '0' && key[5] <= '9')
        { type = "number"; key = key.Substring(5); }
        else if (key.StartsWith("numpad"))
        {
            type = "numpad";
            key = key.Substring(6);
            if (key == "enter") { type = "special"; key = "numpadenter"; }
            else if (key == "divide") key = "slash";
            else if (key == "multiply") key = "asterisk";
            else if (key == "decimal") key = "period";
            else if (key == "add") key = "plus";
            else if (key == "subtract") key = "minus";
            else if (key != "plus" && key != "minus" && key != "period" &&
                     (key.Length != 1 || key[0] < '0' || key[0] > '9')) return false;
        }
        else if (key.Length > 1 && key[0] == 'f' && int.TryParse(key.Substring(1),out int fn) && fn >= 1 && fn <= 12)
            type = "function";
        else
        {
            switch (key)
            {
                case "leftctrl": key = "lctrl"; type = "modifier"; break;
                case "rightctrl": key = "rctrl"; type = "modifier"; break;
                case "leftalt": key = "lalt"; type = "modifier"; break;
                case "rightalt": key = "ralt"; type = "modifier"; break;
                case "leftshift": key = "lshift"; type = "modifier"; break;
                case "rightshift": key = "rshift"; type = "modifier"; break;
                case "leftmeta": key = "lwin"; type = "modifier"; break;
                case "rightmeta": key = "rwin"; type = "modifier"; break;
                case "ctrl": case "alt": case "shift": type = "modifier"; break;
                case "space": case "tab": case "enter": case "escape": case "backspace":
                case "delete": case "home": case "insert": case "pageup": case "pagedown":
                case "capslock": case "printscreen": case "scrolllock": case "pausebreak":
                case "numlock": key = key == "numlock" ? "numberlock" : key; type = "special"; break;
                case "leftbracket": case "rightbracket": case "equals": case "semicolon":
                case "backslash": case "comma": case "period": case "slash": type = "symbol"; break;
                case "backquote": key = "backtick"; type = "symbol"; break;
                case "minus": key = "dash"; type = "symbol"; break;
                case "quote": key = "singlequote"; type = "symbol"; break;
                default: return false;
            }
        }
        name = "key_" + type + "_border_" + key;
        return true;
    }

    /// <summary>Single pressed arrow cluster. direction: "up", "left", "down", "right".</summary>
    public static bool TryGetArrowLayoutTag(string direction, out string tag)
    {
        tag = null;
        if (string.IsNullOrEmpty(direction)) return false;
        string k;
        switch(direction.ToLowerInvariant())
        {
            case "up":    k = "w"; break;
            case "left":  k = "a"; break;
            case "down":  k = "s"; break;
            case "right": k = "d"; break;
            default: return false;
        }
        tag = "<sprite name=\"layout_arrow_transparent_" + k + "\">";
        return true;
    }
}
