using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VideoTranslator.Models;
using VideoTranslator.Services.Interfaces;

namespace VideoTranslator.Controllers
{
    public class VideoTranslatorController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly IAudioConversionService _audioConversionService;
        private readonly ISpeechToTextService _speechToTextService;

        public VideoTranslatorController(
            IWebHostEnvironment env,
            IAudioConversionService audioConversionService,
            ISpeechToTextService speechToTextService)
        {
            _env = env;
            _audioConversionService = audioConversionService;
            _speechToTextService = speechToTextService;
        }

        public IActionResult Index()
        {
            var baseTemp = Path.Combine(Path.GetTempPath(), "Uploads");
            CleanOldTempFiles(baseTemp, 30); // Borra solo los que tengan más de 30 minutos
            return View();
        }
        private static void CleanOldTempFiles(string path, int minutes)
        {
            try
            {
                if (!System.IO.Directory.Exists(path))
                    return;

                var cutoff = DateTime.UtcNow.AddMinutes(-minutes);

                foreach (var file in System.IO.Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var lastWrite = System.IO.File.GetLastWriteTimeUtc(file);
                        if (lastWrite < cutoff)
                            System.IO.File.Delete(file);
                    }
                    catch
                    {
                        // ignorar archivos en uso
                    }
                }

                foreach (var dir in System.IO.Directory.GetDirectories(path))
                {
                    try
                    {
                        if (!System.IO.Directory.EnumerateFileSystemEntries(dir).Any())
                            System.IO.Directory.Delete(dir, true);
                    }
                    catch
                    {
                        // ignorar carpetas en uso
                    }
                }
            }
            catch
            {
                // ignorar errores globales
            }
        }



        [HttpPost]
        public async Task<IActionResult> Index(IFormFile videoFile, string language)
        {
            if (videoFile == null || videoFile.Length == 0)
            {
                ModelState.AddModelError("", "Por favor selecciona un archivo de video.");
                return View();
            }

            // 1) Guardar temporal
            var tempPath = Path.Combine(Path.GetTempPath(), "Uploads");
            Directory.CreateDirectory(tempPath);
            var videoPath = Path.Combine(tempPath, videoFile.FileName);
            using (var fs = new FileStream(videoPath, FileMode.Create))
                await videoFile.CopyToAsync(fs);

            // Calcular tamaño
            var fileInfo = new FileInfo(videoPath);
            string fileSize = fileInfo.Length switch
            {
                < 1024 => $"{fileInfo.Length} B",
                < 1024 * 1024 => $"{fileInfo.Length / 1024.0:F2} KB",
                _ => $"{fileInfo.Length / (1024.0 * 1024.0):F2} MB"
            };

            // 2) Convertir a WAV
            var ffmpegPath = Path.Combine(_env.ContentRootPath, "Utils", "Tools", "ffmpeg", "ffmpeg.exe");
            var audioOutputPath = Path.Combine(tempPath, "Audio");
            var wavPath = await _audioConversionService.ConvertVideoToWavAsync(videoPath, audioOutputPath, ffmpegPath);

            // 3) Transcribir (y traducir si language == "es")
            var whisperPath = Path.Combine(_env.ContentRootPath, "Utils", "Tools", "whisper", "whisper-cli.exe");
            var modelPath = Path.Combine(_env.ContentRootPath, "Utils", "Tools", "whisper", "ggml-base.bin");

            var (transcript, langUsed) = await _speechToTextService
                .TranscribeWithLanguageAsync(wavPath, whisperPath, modelPath, language);

            var result = new TranscriptionResult
            {
                FileName = videoFile.FileName,
                FileSize = fileSize,
                TranscriptText = transcript, // si era ES, viene traducido a EN
                DetectedLanguage = langUsed, // lo que seleccionó el usuario (en/es)
                Language = langUsed,
                TranscribedAt = DateTime.UtcNow
            };

            return View("Result", result);
        }

        [HttpPost]
        public IActionResult ExportTxt(string fileName, string transcript, string language)
        {
            // Nombre de archivo "limpio"
            var baseName = Path.GetFileNameWithoutExtension(fileName) ?? "video";
            foreach (var c in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(c, '_');

            var finalName = $"{baseName}_transcript_{language}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

            // UTF-8 con BOM (para que Notepad muestre bien tildes/ñ)
            var encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = encoding.GetBytes(transcript ?? string.Empty);

            return File(bytes, "text/plain", finalName);
        }

    }
}
