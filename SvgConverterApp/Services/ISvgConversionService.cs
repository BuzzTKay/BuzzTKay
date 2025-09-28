using System.Threading;
using System.Threading.Tasks;
using SvgConverterApp.Models;

namespace SvgConverterApp.Services;

public interface ISvgConversionService
{
    Task<string> ConvertAsync(string svgContent, SvgFormat inputFormat, SvgFormat outputFormat, CancellationToken cancellationToken = default);
}
