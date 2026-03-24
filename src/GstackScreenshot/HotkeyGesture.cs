using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GstackScreenshot
{
    [Flags]
    internal enum HotkeyModifiers : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
        NoRepeat = 0x4000
    }

    internal sealed class HotkeyGesture : IEquatable<HotkeyGesture>
    {
        public HotkeyGesture(Keys key, HotkeyModifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public Keys Key { get; private set; }

        public HotkeyModifiers Modifiers { get; private set; }

        public static HotkeyGesture Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Shortcut cannot be empty.");
            }

            var parts = value.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .ToArray();

            if (parts.Length < 2)
            {
                throw new ArgumentException("Shortcut must include at least one modifier and one key.");
            }

            HotkeyModifiers modifiers = HotkeyModifiers.None;
            Keys? key = null;

            foreach (var part in parts)
            {
                switch (part.ToUpperInvariant())
                {
                    case "CTRL":
                    case "CONTROL":
                        modifiers |= HotkeyModifiers.Control;
                        break;
                    case "ALT":
                        modifiers |= HotkeyModifiers.Alt;
                        break;
                    case "SHIFT":
                        modifiers |= HotkeyModifiers.Shift;
                        break;
                    case "WIN":
                    case "WINDOWS":
                        modifiers |= HotkeyModifiers.Win;
                        break;
                    default:
                        if (key.HasValue)
                        {
                            throw new ArgumentException("Shortcut can only include one non-modifier key.");
                        }

                        Keys parsedKey;
                        if (!Enum.TryParse(part, true, out parsedKey))
                        {
                            throw new ArgumentException("Unsupported key '" + part + "'.");
                        }

                        key = parsedKey;
                        break;
                }
            }

            if (modifiers == HotkeyModifiers.None)
            {
                throw new ArgumentException("Shortcut must include Ctrl, Alt, Shift, or Win.");
            }

            if (!key.HasValue)
            {
                throw new ArgumentException("Shortcut must end with a key, for example Ctrl+Alt+S.");
            }

            return new HotkeyGesture(key.Value, modifiers | HotkeyModifiers.NoRepeat);
        }

        public override string ToString()
        {
            var parts = new List<string>();

            if ((Modifiers & HotkeyModifiers.Control) == HotkeyModifiers.Control)
            {
                parts.Add("Ctrl");
            }

            if ((Modifiers & HotkeyModifiers.Alt) == HotkeyModifiers.Alt)
            {
                parts.Add("Alt");
            }

            if ((Modifiers & HotkeyModifiers.Shift) == HotkeyModifiers.Shift)
            {
                parts.Add("Shift");
            }

            if ((Modifiers & HotkeyModifiers.Win) == HotkeyModifiers.Win)
            {
                parts.Add("Win");
            }

            parts.Add(Key.ToString().ToUpperInvariant());
            return string.Join("+", parts);
        }

        public bool Equals(HotkeyGesture other)
        {
            return other != null && Key == other.Key && Modifiers == other.Modifiers;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as HotkeyGesture);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Key * 397) ^ (int)Modifiers;
            }
        }
    }
}

