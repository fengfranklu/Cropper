#region Using Directives

using System;
using System.Text;
using System.Windows.Forms;

#endregion

namespace Fusion8.Cropper.Core
{
    ///<summary>
    ///</summary>
    public class ShortcutTextBox : Control
    {
        private const string Seperator = " + ";

        private ShortcutMode mode;

        #region .ctor

        public ShortcutTextBox()
        {
            SetStyle(ControlStyles.UserPaint, false);
            SetInvalidKeys(~Keys.Control, Keys.None, Keys.Shift);
        }

        #endregion

        public void Clear()
        {
            NativeMethods.SendMessage(Handle, NativeMethods.HKM_SETHOTKEY, IntPtr.Zero, IntPtr.Zero);
        }

        public static string GetShorcutText(Keys keyData)
        {
            Keys keyCode = StripModifiers(keyData);
            Keys modifiers = keyData & ~keyCode;
            StringBuilder builder = new StringBuilder();

            builder.Append(keyCode == Keys.None ? Keys.None.ToString() : RetrieveKeyName((uint) keyCode));

            if ((modifiers & Keys.Alt) == Keys.Alt)
                builder.Insert(0, RetrieveKeyName((uint) Keys.Menu) + Seperator);
            if ((modifiers & Keys.Shift) == Keys.Shift)
                builder.Insert(0, RetrieveKeyName((uint) Keys.ShiftKey) + Seperator);
            if ((modifiers & Keys.Control) == Keys.Control)
                builder.Insert(0, RetrieveKeyName((uint) Keys.ControlKey) + Seperator);

            return builder.ToString();
        }

        #region Property Accessors

        /// <summary>
        ///     Encapsulates the information needed when creating a control.
        /// </summary>
        /// <value></value>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ClassName = NativeMethods.HOTKEY_CLASS;
                return createParams;
            }
        }

        public ShortcutMode Mode
        {
            get => mode;
            set
            {
                mode = value;

                switch (mode)
                {
                    case ShortcutMode.Global:
                        SetInvalidKeys(~Keys.Control, Keys.None);
                        break;
                    case ShortcutMode.Local:
                        NativeMethods.SendMessage(Handle, NativeMethods.HKM_SETRULES, IntPtr.Zero, IntPtr.Zero);
                        break;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the
        /// </summary>
        public Keys KeyData
        {
            get => KeyCode | Modifiers;
            set => SetKeys(value);
        }

        /// <summary>
        ///     Gets the key code.
        /// </summary>
        /// <value>The key code.</value>
        public Keys KeyCode
        {
            get
            {
                IntPtr message = NativeMethods.SendMessage(Handle, NativeMethods.HKM_GETHOTKEY, IntPtr.Zero, IntPtr.Zero);
                int keyCode = message.ToInt32() & 0xff;

                return (Keys) keyCode;
            }
            set => SetKeys(value, Modifiers);
        }

        /// <summary>
        ///     Gets the modifiers.
        /// </summary>
        /// <value>The modifiers.</value>
        public Keys Modifiers
        {
            get
            {
                IntPtr message = NativeMethods.SendMessage(Handle, NativeMethods.HKM_GETHOTKEY, IntPtr.Zero, IntPtr.Zero);
                int flags = message.ToInt32() >> 8;

                Keys modifiers = Keys.None;
                if ((flags & NativeMethods.HOTKEYF_ALT) == NativeMethods.HOTKEYF_ALT)
                    modifiers |= Keys.Alt;
                if ((flags & NativeMethods.HOTKEYF_CONTROL) == NativeMethods.HOTKEYF_CONTROL)
                    modifiers |= Keys.Control;
                if ((flags & NativeMethods.HOTKEYF_SHIFT) == NativeMethods.HOTKEYF_SHIFT)
                    modifiers |= Keys.Shift;
                return modifiers;
            }
            set => SetKeys(KeyCode, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating if the Alt key is displayed in
        ///     the <see cref="ShortcutTextBox" /> control.
        /// </summary>
        public bool Alt
        {
            get => (Modifiers & Keys.Alt) == Keys.Alt;
            set
            {
                if (value)
                    Modifiers |= Keys.Alt;
                else
                    Modifiers &= ~Keys.Alt;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating if the Shift key is displayed in
        ///     the <see cref="ShortcutTextBox" /> control.
        /// </summary>
        public bool Shift
        {
            get => (Modifiers & Keys.Shift) == Keys.Shift;
            set
            {
                if (value)
                    Modifiers |= Keys.Shift;
                else
                    Modifiers &= ~Keys.Shift;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating if the Control key is displayed in
        ///     the <see cref="ShortcutTextBox" /> control.
        /// </summary>
        public bool Control
        {
            get => (Modifiers & Keys.Control) == Keys.Control;
            set
            {
                if (value)
                    Modifiers |= Keys.Control;
                else
                    Modifiers &= ~Keys.Control;
            }
        }

        /// <summary>
        ///     Gets the current text in the <see cref="ShortcutTextBox" />.
        /// </summary>
        /// <remarks>This property will throw an exception if the setter is called.</remarks>
        /// <value></value>
        /// <filterPriority>1</filterPriority>
        public override string Text
        {
            get => GetShorcutText(KeyData);
            set
            {
                try
                {
                    Keys keys = (Keys) Enum.Parse(typeof(Keys), value.Replace('+', ',').Replace("Ctrl", "Control"));
                    SetKeys(keys);
                }
                catch (ArgumentException)
                {
                    SetKeys(Keys.None);
                }
            }
        }

        #endregion

        #region Private Helpers

        private void SetInvalidKeys(Keys replacementModifiers, params Keys[] invalidModifiers)
        {
            int hotKeyF = 0;

            foreach (Keys keys in invalidModifiers)
                switch (keys)
                {
                    case Keys.Alt:
                        hotKeyF |= (int) NativeMethods.InvalidHotKeyModifiers.HKCOMB_A;
                        break;
                    case Keys.Control:
                        hotKeyF |= (int) NativeMethods.InvalidHotKeyModifiers.HKCOMB_C;
                        break;
                    case Keys.Control | Keys.Alt:
                        hotKeyF |= (int) NativeMethods.InvalidHotKeyModifiers.HKCOMB_CA;
                        break;
                    case Keys.None:
                        hotKeyF |= (int) NativeMethods.InvalidHotKeyModifiers.HKCOMB_NONE;
                        break;
                    case Keys.Shift:
                        hotKeyF |= (int) NativeMethods.InvalidHotKeyModifiers.HKCOMB_S;
                        break;
                    case Keys.Shift | Keys.Alt:
                        hotKeyF |= (int) NativeMethods.InvalidHotKeyModifiers.HKCOMB_SA;
                        break;
                    case Keys.Shift | Keys.Control:
                        hotKeyF = (int) NativeMethods.InvalidHotKeyModifiers.HKCOMB_SC;
                        break;
                    case Keys.Shift | Keys.Control | Keys.Alt:
                        hotKeyF |= (int) NativeMethods.InvalidHotKeyModifiers.HKCOMB_SCA;
                        break;
                }

            int replacement = 0;
            if ((replacementModifiers & Keys.Alt) == Keys.Alt)
                replacement |= NativeMethods.HOTKEYF_ALT;
            if ((replacementModifiers & Keys.Control) == Keys.Control)
                replacement |= NativeMethods.HOTKEYF_CONTROL;
            if ((replacementModifiers & Keys.Shift) == Keys.Shift)
                replacement |= NativeMethods.HOTKEYF_SHIFT;

            NativeMethods.SendMessage(Handle, NativeMethods.HKM_SETRULES, (IntPtr) hotKeyF, (IntPtr) replacement);
        }

        private void SetKeys(Keys keyData)
        {
            Keys keyCode = StripModifiers(keyData);
            Keys modifiers = keyData & ~keyCode;

            SetKeys(keyCode, modifiers);
        }

        private void SetKeys(Keys keyCode, Keys modifiers)
        {
            int modifiers2 = 0;
            if ((modifiers & Keys.Alt) == Keys.Alt)
                modifiers2 |= NativeMethods.HOTKEYF_ALT;
            if ((modifiers & Keys.Control) == Keys.Control)
                modifiers2 |= NativeMethods.HOTKEYF_CONTROL;
            if ((modifiers & Keys.Shift) == Keys.Shift)
                modifiers2 |= NativeMethods.HOTKEYF_SHIFT;

            NativeMethods.SendMessage(Handle, NativeMethods.HKM_SETHOTKEY, MakeWParam((int) keyCode, modifiers2), IntPtr.Zero);
        }

        private static Keys StripModifiers(Keys keyData)
        {
            return keyData & ~(Keys.Alt | Keys.Control | Keys.Shift);
        }

        private static IntPtr MakeWParam(int l, int h)
        {
            return (IntPtr) ((l & 0xFFFF) | (h << 8));
        }

        private static string RetrieveKeyName(uint keyCode)
        {
            uint scanCode = GetScanCode(keyCode);

            StringBuilder buffer = new StringBuilder(260);
            NativeMethods.GetKeyNameText(scanCode << 16, buffer, buffer.Capacity);
            return buffer.ToString();
        }

        private static uint GetScanCode(uint keyCode)
        {
            IntPtr hkl = NativeMethods.GetKeyboardLayout(0);
            uint scanCode = NativeMethods.MapVirtualKeyEx(keyCode, NativeMethods.MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC, hkl);

            switch ((Keys) keyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.Insert:
                case Keys.Delete:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                case Keys.Oem1:
                case Keys.Oem102:
                case Keys.Oem2:
                case Keys.Oem3:
                case Keys.Oem4:
                case Keys.Oem5:
                case Keys.Oem6:
                case Keys.Oem7:
                case Keys.Oem8:
                case Keys.OemClear:
                case Keys.Oemcomma:
                case Keys.OemMinus:
                case Keys.OemPeriod:
                case Keys.Oemplus:
                    scanCode += 0x100;
                    break;
            }
            return scanCode;
        }

        #endregion
    }

    public enum ShortcutMode
    {
        Global,
        Local
    }
}