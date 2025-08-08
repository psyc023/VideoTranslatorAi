using System.Diagnostics;
using VideoTranslator.Services.Interfaces;

namespace VideoTranslator.Services
{
    public class AudioConversionService : IAudioConversionService
    {
        private readonly ILogger<AudioConversionService> _logger;

        public AudioConversionService(ILogger<AudioConversionService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ConvertVideoToWavAsync(string videoPath, string outputDirectory, string ffmpegPath)
        {
            _logger.LogInformation("Iniciando conversión de video a WAV...");
            _logger.LogInformation("Ruta del video: {VideoPath}", videoPath);

            if (!File.Exists(videoPath))
                throw new FileNotFoundException("El archivo de video no existe.", videoPath);

            var fileInfo = new FileInfo(videoPath);
            _logger.LogInformation("Tamaño del archivo de entrada: {FileSize} MB",
                (fileInfo.Length / (1024.0 * 1024.0)).ToString("F2"));

            Directory.CreateDirectory(outputDirectory);
            _logger.LogInformation("Directorio de salida creado/verificado: {OutputDirectory}", outputDirectory);

            string outputFileName = Path.GetFileNameWithoutExtension(videoPath) + "_full.wav";
            string outputPath = Path.Combine(outputDirectory, outputFileName);
            _logger.LogInformation("Archivo de salida WAV: {OutputPath}", outputPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-y -i \"{videoPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("Ejecutando FFmpeg...");
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg falló. Error: {Error}", stderr);
                throw new Exception($"FFmpeg failed: {stderr}");
            }

            _logger.LogInformation("Conversión a WAV completada con éxito.");
            return outputPath;
        }
    }
}
