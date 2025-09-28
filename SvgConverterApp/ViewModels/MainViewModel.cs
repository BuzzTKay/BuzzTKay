using SvgConverterApp.Commands;
using SvgConverterApp.Models;
using SvgConverterApp.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;

namespace SvgConverterApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly ISvgConversionService _conversionService;

    private SvgFormat _selectedInputFormat;
    private SvgFormat _selectedOutputFormat;
    private string _statusMessage = "Bereit";

    public MainViewModel(IFileDialogService fileDialogService, ISvgConversionService conversionService)
    {
        _fileDialogService = fileDialogService;
        _conversionService = conversionService;

        Formats = new ObservableCollection<SvgFormat>(Enum.GetValues<SvgFormat>());
        SelectedInputFormat = SvgFormat.Standard;
        SelectedOutputFormat = SvgFormat.Inkscape;

        SelectedFiles = new ObservableCollection<string>();
        ActivityLog = new ObservableCollection<string>();

        BrowseFilesCommand = new RelayCommand(BrowseFiles);
        ConvertCommand = new AsyncRelayCommand(ConvertAsync, CanConvert);
    }

    public ObservableCollection<SvgFormat> Formats { get; }

    public ObservableCollection<string> SelectedFiles { get; }

    public ObservableCollection<string> ActivityLog { get; }

    public RelayCommand BrowseFilesCommand { get; }

    public AsyncRelayCommand ConvertCommand { get; }

    public SvgFormat SelectedInputFormat
    {
        get => _selectedInputFormat;
        set
        {
            if (SetProperty(ref _selectedInputFormat, value))
            {
                ConvertCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public SvgFormat SelectedOutputFormat
    {
        get => _selectedOutputFormat;
        set
        {
            if (SetProperty(ref _selectedOutputFormat, value))
            {
                ConvertCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private void BrowseFiles()
    {
        var files = _fileDialogService.OpenFiles("SVG Dateien (*.svg)|*.svg", allowMultiple: true);
        if (files is null || files.Count == 0)
        {
            return;
        }

        SelectedFiles.Clear();
        foreach (var file in files)
        {
            SelectedFiles.Add(file);
        }

        ActivityLog.Clear();
        ActivityLog.Add(files.Count == 1
            ? "1 Datei bereit zum Konvertieren."
            : string.Format(CultureInfo.CurrentCulture, "{0} Dateien bereit zum Konvertieren.", files.Count));
    }

    private bool CanConvert()
    {
        return SelectedFiles.Count > 0 && SelectedInputFormat != SelectedOutputFormat;
    }

    private async Task ConvertAsync()
    {
        if (SelectedFiles.Count == 0)
        {
            StatusMessage = "Bitte wähle zuerst Dateien aus.";
            return;
        }

        if (SelectedInputFormat == SelectedOutputFormat)
        {
            StatusMessage = "Eingabe- und Zielformat sind identisch.";
            return;
        }

        StatusMessage = "Konvertierung läuft...";
        ActivityLog.Add("Konvertierung gestartet.");

        if (SelectedFiles.Count == 1)
        {
            await ConvertSingleAsync(SelectedFiles[0]);
        }
        else
        {
            await ConvertBatchAsync();
        }
    }

    private async Task ConvertSingleAsync(string filePath)
    {
        var defaultName = Path.GetFileNameWithoutExtension(filePath) + GetFormatSuffix(SelectedOutputFormat) + ".svg";
        var savePath = _fileDialogService.SaveFile(defaultName, "SVG Dateien (*.svg)|*.svg");

        if (string.IsNullOrWhiteSpace(savePath))
        {
            ActivityLog.Add("Konvertierung abgebrochen - kein Speicherort gewählt.");
            StatusMessage = "Bereit";
            return;
        }

        await ConvertAndSaveAsync(filePath, savePath);
    }

    private async Task ConvertBatchAsync()
    {
        var folder = _fileDialogService.SelectFolder();
        if (string.IsNullOrWhiteSpace(folder))
        {
            ActivityLog.Add("Konvertierung abgebrochen - kein Zielordner gewählt.");
            StatusMessage = "Bereit";
            return;
        }

        var total = SelectedFiles.Count;
        var index = 0;
        foreach (var file in SelectedFiles)
        {
            index++;
            var savePath = Path.Combine(folder, Path.GetFileNameWithoutExtension(file) + GetFormatSuffix(SelectedOutputFormat) + ".svg");
            ActivityLog.Add(string.Format(CultureInfo.CurrentCulture, "[{0}/{1}] {2} wird konvertiert...", index, total, Path.GetFileName(file)));
            await ConvertAndSaveAsync(file, savePath);
        }

        ActivityLog.Add("Batch-Konvertierung abgeschlossen.");
    }

    private async Task ConvertAndSaveAsync(string inputPath, string outputPath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(inputPath);
            var converted = await _conversionService.ConvertAsync(content, SelectedInputFormat, SelectedOutputFormat);
            await File.WriteAllTextAsync(outputPath, converted);
            ActivityLog.Add(string.Format(CultureInfo.CurrentCulture, "Gespeichert unter {0}.", outputPath));
            StatusMessage = "Konvertierung abgeschlossen.";
        }
        catch (Exception ex)
        {
            ActivityLog.Add(string.Format(CultureInfo.CurrentCulture, "Fehler: {0}", ex.Message));
            StatusMessage = "Konvertierung fehlgeschlagen.";
        }
    }

    private static string GetFormatSuffix(SvgFormat format) => format switch
    {
        SvgFormat.Standard => "_standard",
        SvgFormat.Inkscape => "_inkscape",
        SvgFormat.SiemensSvghmi => "_svghmi",
        _ => string.Empty
    };
}
