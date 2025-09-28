using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;
using Forms = System.Windows.Forms;

namespace SvgConverterApp.Services;

public class FileDialogService : IFileDialogService
{
    private readonly Window _owner;

    public FileDialogService(Window owner)
    {
        _owner = owner;
    }

    public IReadOnlyList<string>? OpenFiles(string filter, bool allowMultiple)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Multiselect = allowMultiple,
            DefaultExt = ".svg"
        };

        return dialog.ShowDialog(_owner) == true ? dialog.FileNames : null;
    }

    public string? SaveFile(string defaultFileName, string filter)
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter = filter,
            DefaultExt = ".svg"
        };

        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    public string? SelectFolder()
    {
        using var dialog = new Forms.FolderBrowserDialog();
        return dialog.ShowDialog(new WindowWrapper(new System.Windows.Interop.WindowInteropHelper(_owner).Handle)) == Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }

    private sealed class WindowWrapper : Forms.IWin32Window
    {
        public WindowWrapper(nint handle)
        {
            Handle = handle;
        }

        public nint Handle { get; }
    }
}
