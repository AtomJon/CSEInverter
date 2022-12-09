using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;

namespace CSEInverter
{
    [Serializable]
    public class UserDeniedException : Exception
    {
        public UserDeniedException() { }
        public UserDeniedException(string message) : base(message) { }
    }

    internal class Dialog
    {
        public static Stream AskForFile(string title, string filter)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = filter;
            fileDialog.Title = title;
            fileDialog.Multiselect = false;

            bool? success = fileDialog.ShowDialog();

            if (success.HasValue && success.Value)
            {
                return fileDialog.OpenFile();
            }
            else
            {
                throw new UserDeniedException("No file was selected");
            }
        }

        public static IEnumerable<string> AskForDirectories(string title)
        {
            IEnumerable<string> folders = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                using CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                dialog.Multiselect = true;
                dialog.EnsurePathExists = false;
                dialog.EnsureFileExists = false;
                dialog.Title = title;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    folders = dialog.FileNames;
                }
                else
                {
                    throw new UserDeniedException("No folder was selected");
                }
            });

            if (folders == null)
            {
                throw new InvalidOperationException("Dispatcher did not invoke callback, can't select folders");
            }
            else
            {
                return folders;
            }
        }

        public static bool YesOrNo(string title, string body)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show(body, title, MessageBoxButton.YesNo);

            return messageBoxResult == MessageBoxResult.Yes;
        }
    }
}