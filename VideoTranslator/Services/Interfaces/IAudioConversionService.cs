namespace VideoTranslator.Services.Interfaces
{
    public interface IAudioConversionService
    {
        Task<string> ConvertVideoToWavAsync(string videoPath, string outputDirectory, string ffmpegPath);
    }
}
