namespace VideoTranslator.Services.Interfaces
{
    public interface ISpeechToTextService
    {
        // Modo auto-detec (si lo sigues usando en otro lado)
        Task<(string transcript, string? detectedLang)> TranscribeAsync(
            string audioPath, string whisperExePath, string modelPath);

        // Nuevo: fuerza idioma (en/es) y traduce a EN si es ES
        Task<(string transcript, string langUsed)> TranscribeWithLanguageAsync(
            string audioPath, string whisperExePath, string modelPath, string language);
    }
}
