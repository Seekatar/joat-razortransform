// Copyright (c) 2012 JOAT Services, Jim Wallace
// See the file license.txt for copying permission.
using System.Windows;
using System.Runtime.InteropServices;
using System;
using System.Windows.Interop;
using RazorTransform.Properties;

namespace Joat
{
    public class TaskDialog
    {
        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, EntryPoint="TaskDialog")]
        static extern int _TaskDialog(IntPtr hWndParent, IntPtr hInstance, String pszWindowTitle, String pszMainInstruction, String pszContent, int dwCommonButtons, IntPtr pszIcon, out int pnButton);

        #region Modal

        public static TaskDialogResult Show(IWin32Window owner, string text)
        {
            return Show(owner, text, null, null, TaskDialogButtons.OK);
        }

        public static TaskDialogResult Show(IWin32Window owner, string text, string instruction)
        {
            return Show(owner, text, instruction, null, TaskDialogButtons.OK, 0);
        }

        public static TaskDialogResult Show(IWin32Window owner, string text, string instruction, string caption)
        {
            return Show(owner, text, instruction, caption, TaskDialogButtons.OK, 0);
        }

        public static TaskDialogResult Show(IWin32Window owner, string text, string instruction, string caption, TaskDialogButtons buttons)
        {
            return Show(owner, text, instruction, caption, buttons, 0);
        }

        public static TaskDialogResult Show(IWin32Window owner, string text, string instruction, string caption, TaskDialogButtons buttons, TaskDialogIcon icon)
        {
            return ShowInternal(owner.Handle, text, instruction, caption, buttons, icon);
        }

        #endregion

        #region Non-modal

        public static TaskDialogResult Show(string text)
        {
            return Show(text, null, null, TaskDialogButtons.OK);
        }

        public static TaskDialogResult Show(string text, string instruction)
        {
            return Show(text, instruction, null, TaskDialogButtons.OK, 0);
        }

        public static TaskDialogResult Show(string text, string instruction, string caption)
        {
            return Show(text, instruction, caption, TaskDialogButtons.OK, 0);
        }

        public static TaskDialogResult Show(string text, string instruction, string caption, TaskDialogButtons buttons)
        {
            return Show(text, instruction, caption, buttons, 0);
        }

        public static TaskDialogResult Show(string text, string instruction, string caption, TaskDialogButtons buttons, TaskDialogIcon icon)
        {
            return ShowInternal(IntPtr.Zero, text, instruction, caption, buttons, icon);
        }

        public static MessageBoxResult ShowMsg(Window parent, string text, string instruction = "", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Warning)
        {
            switch ( ShowMsg( parent, text, instruction, mapMsgBoxButton(buttons), mapMsgBoxImage(icon)))
            {
                case TaskDialogResult.Cancel:
                case TaskDialogResult.Close:
                    return MessageBoxResult.Cancel;
                case TaskDialogResult.No:
                    return  MessageBoxResult.No;
                case TaskDialogResult.OK:
                    return MessageBoxResult.OK;
                case TaskDialogResult.None:
                    return MessageBoxResult.None;
                case TaskDialogResult.Yes:
                    return MessageBoxResult.Yes;
                default:
                    return MessageBoxResult.None;
            }
        }

        private static TaskDialogIcon mapMsgBoxImage(MessageBoxImage icon)
        {
            switch ( icon )
            {
                case MessageBoxImage.Exclamation:
                    return TaskDialogIcon.Warning;
                case MessageBoxImage.Error:
                    return TaskDialogIcon.Stop;
                case MessageBoxImage.Information:
                    return TaskDialogIcon.Information;
                default:
                    return 0;
            }
        }

        private static TaskDialogButtons mapMsgBoxButton(MessageBoxButton buttons)
        {
            switch( buttons )
            {
                case MessageBoxButton.OKCancel:
                    return TaskDialogButtons.OK | TaskDialogButtons.Cancel;
                case MessageBoxButton.YesNoCancel:
                    return TaskDialogButtons.YesNoCancel;
                case MessageBoxButton.YesNo:
                    return TaskDialogButtons.Yes | TaskDialogButtons.No;
                default:
                    return TaskDialogButtons.OK;
            }
        }

        public static TaskDialogResult ShowMsg(Window parent, string text, string instruction = "", TaskDialogButtons buttons = TaskDialogButtons.OK, TaskDialogIcon icon = TaskDialogIcon.Warning)
        {
            return TaskDialog.ShowInternal(parent != null ? new System.Windows.Interop.WindowInteropHelper(parent).Handle : IntPtr.Zero,
                                            instruction,
                                            text,
                                            RazorTransform.Resource.Title,
                                            buttons, 
                                            icon);
        }

        public static TaskDialogResult ShowMsg(string text, string instruction = "", TaskDialogButtons buttons = TaskDialogButtons.OK, TaskDialogIcon icon = TaskDialogIcon.Warning)
        {
            return TaskDialog.ShowInternal(IntPtr.Zero,
                                            instruction,
                                            text,
                                            RazorTransform.Resource.Title,
                                            buttons,
                                            icon);
        }

        #endregion

        #region Core implementation

        private static TaskDialogResult ShowInternal(IntPtr owner, string text, string instruction, string caption, TaskDialogButtons buttons, TaskDialogIcon icon)
        {
            int p;
            if (_TaskDialog(owner, IntPtr.Zero, caption, instruction, text, (int)buttons, new IntPtr((int)icon), out p) != 0)
                throw new InvalidOperationException("Something weird has happened.");

            switch (p)
            {
                case 1:
                    return TaskDialogResult.OK;
                case 2:
                    return TaskDialogResult.Cancel;
                case 4:
                    return TaskDialogResult.Retry;
                case 6:
                    return TaskDialogResult.Yes;
                case 7:
                    return TaskDialogResult.No;
                case 8:
                    return TaskDialogResult.Close;
                default:
                    return TaskDialogResult.None;
            }
        }

        #endregion
    }

    [Flags]
    public enum TaskDialogButtons
    {
        OK = 0x0001,
        Cancel = 0x0008,
        Yes = 0x0002,
        No = 0x0004,
        Retry = 0x0010,
        Close = 0x0020,
        YesNoCancel = Yes | No | Cancel
    }

    public enum TaskDialogIcon
    {
        Information = UInt16.MaxValue - 2,
        Warning = UInt16.MaxValue,
        Stop = UInt16.MaxValue - 1,        
        SecurityWarning = UInt16.MaxValue - 5,
        SecurityError = UInt16.MaxValue - 6,
        SecuritySuccess = UInt16.MaxValue - 7,
        SecurityShield = UInt16.MaxValue - 3,
        SecurityShieldBlue = UInt16.MaxValue - 4,
        SecurityShieldGray = UInt16.MaxValue - 8
    }

    public enum TaskDialogResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No,
        Retry,
        Close
    }

}
