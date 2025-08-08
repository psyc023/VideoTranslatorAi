using System.Diagnostics;
using System.Text.RegularExpressions;
using VideoTranslator.Services.Interfaces;

namespace VideoTranslator.Services
{
    public class SpeechToTextService : ISpeechToTextService
    {
        private readonly ILogger<SpeechToTextService> _logger;

        public SpeechToTextService(ILogger<SpeechToTextService> logger)
        {
            _logger = logger;
        }

        public async Task<(string transcript, string? detectedLang)> TranscribeAsync(
            string audioPath, string whisperExePath, string modelPath)
        {
            _logger.LogInformation("Iniciando transcripción automática (idioma detectado automáticamente)...");
            _logger.LogInformation("Ruta de audio: {AudioPath}", audioPath);

            string outputPath = Path.ChangeExtension(audioPath, ".txt");
            string outputPrefix = Path.Combine(Path.GetDirectoryName(audioPath)!, Path.GetFileNameWithoutExtension(audioPath));

            var startInfo = new ProcessStartInfo
            {
                FileName = whisperExePath,
                Arguments = $"-m \"{modelPath}\" -f \"{audioPath}\" -otxt -of \"{outputPrefix}\" --language auto",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("Ejecutando Whisper con detección automática de idioma...");
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Whisper falló. Error: {Error}", stderr);
                throw new Exception($"Whisper failed: {stderr}");
            }

            if (!File.Exists(outputPath))
                throw new Exception("No se generó el archivo de transcripción");

            string rawText = await File.ReadAllTextAsync(outputPath);
            _logger.LogInformation("Archivo de transcripción leído correctamente.");

            string? detectedLang = null;
            var match = Regex.Match(stdout, @"detected language:\s+([a-z]{2})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                detectedLang = match.Groups[1].Value;
                _logger.LogInformation("Idioma detectado: {Lang}", detectedLang);
            }

            var cleaned = CleanText(rawText);
            try { File.Delete(outputPath); } catch { /* no-op */ }

            _logger.LogInformation("Transcripción completada con éxito.");
            return (cleaned, detectedLang);
        }

        public async Task<(string transcript, string langUsed)> TranscribeWithLanguageAsync(
            string audioPath, string whisperExePath, string modelPath, string language)
        {
            language = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim().ToLowerInvariant();
            _logger.LogInformation("Iniciando transcripción forzada en idioma: {Language}", language);

            string outputPath = Path.ChangeExtension(audioPath, ".txt");
            string outputPrefix = Path.Combine(Path.GetDirectoryName(audioPath)!, Path.GetFileNameWithoutExtension(audioPath));
            var translateFlag = string.Empty;

            var args = $"-m \"{modelPath}\" -f \"{audioPath}\" -otxt -of \"{outputPrefix}\" --language {language} {translateFlag}";

            var startInfo = new ProcessStartInfo
            {
                FileName = whisperExePath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("Ejecutando Whisper con idioma forzado...");
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string _ = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Whisper falló. Error: {Error}", stderr);
                throw new Exception($"Whisper failed: {stderr}");
            }

            if (!File.Exists(outputPath))
                throw new Exception("No se generó el archivo de transcripción");

            string rawText = await File.ReadAllTextAsync(outputPath);
            var cleaned = CleanText(rawText);
            _logger.LogInformation("Archivo de transcripción leído correctamente. Tamaño del texto: {Length} caracteres", cleaned.Length);

            try { File.Delete(outputPath); } catch { /* no-op */ }

            _logger.LogInformation("Transcripción completada con éxito.");
            return (cleaned, language);
        }

        private static string CleanText(string raw)
        {
            var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l));

            return Regex.Replace(string.Join(" ", lines), @"\s+", " ");
        }
    }
}
