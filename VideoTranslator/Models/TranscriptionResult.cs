namespace VideoTranslator.Models
{
    public class TranscriptionResult
    {
        public string? FileName { get; set; }
        public string? TranscriptText { get; set; }
        public string? Language { get; set; } = "en";
        public string? DetectedLanguage { get; set; }
        public string? FileSize { get; set; }

        public DateTime TranscribedAt { get; set; } = DateTime.UtcNow;
    }
}