using MorseTrainer.Core;
using NAudio.Wave;

namespace MorseTrainer.Cli;

/// <summary>Воспроизведение сигнала в консоли через NAudio.</summary>
public static class ConsolePlayer
{
    public static void Play(MorseSignal signal, int frequencyHz, double volume)
    {
        byte[] wav = new WavGenerator(frequencyHz, volume).Generate(signal);

        using var reader = new WaveFileReader(new MemoryStream(wav));
        using var output = new WaveOut();
        output.Init(reader);
        output.Play();

        // Ждём, пока звук доиграет
        while (output.PlaybackState == PlaybackState.Playing)
            Thread.Sleep(50);
    }
}
