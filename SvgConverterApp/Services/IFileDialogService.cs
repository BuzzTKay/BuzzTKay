namespace SvgConverterApp.Services;

public interface IFileDialogService
{
    IReadOnlyList<string>? OpenFiles(string filter, bool allowMultiple);
    string? SaveFile(string defaultFileName, string filter);
    string? SelectFolder();
}
