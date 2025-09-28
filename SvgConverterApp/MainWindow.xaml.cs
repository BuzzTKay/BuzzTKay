using SvgConverterApp.Services;
using SvgConverterApp.ViewModels;
using System.Windows;

namespace SvgConverterApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var dialogService = new FileDialogService(this);
        var conversionService = new SvgConversionService();
        DataContext = new MainViewModel(dialogService, conversionService);
    }
}
