namespace MorseTrainer.Core;

/// <summary>Создаёт WAV-файл (16 бит, моно, 44100 Гц) из сигнала Морзе.</summary>
public class WavGenerator
{
    public const int SampleRate = 44100;
    public const int MinFrequency = 400;
    public const int MaxFrequency = 1000;

    // Время плавного нарастания и затухания тона, чтобы не было щелчков
    private const int FadeMs = 5;

    public WavGenerator(int frequencyHz = 700, double volume = 0.8)
    {
        if (frequencyHz < MinFrequency || frequencyHz > MaxFrequency)
            throw new ArgumentOutOfRangeException(nameof(frequencyHz), $"Частота должна быть от {MinFrequency} до {MaxFrequency} Гц");
        if (volume < 0 || volume > 1)
            throw new ArgumentOutOfRangeException(nameof(volume), "Громкость должна быть от 0 до 1");

        FrequencyHz = frequencyHz;
        Volume = volume;
    }

    public int FrequencyHz { get; }

    public double Volume { get; }

    /// <summary>Возвращает WAV-файл в виде массива байт.</summary>
    public byte[] Generate(MorseSignal signal)
    {
        short[] samples = CreateSamples(signal);
        int dataSize = samples.Length * 2;

        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream);

        // Заголовок WAV
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);              // размер блока fmt
        writer.Write((short)1);        // PCM
        writer.Write((short)1);        // моно
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);  // байт в секунду
        writer.Write((short)2);        // байт на один отсчёт
        writer.Write((short)16);       // бит на отсчёт
        writer.Write("data"u8);
        writer.Write(dataSize);

        foreach (short sample in samples)
            writer.Write(sample);

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>Сохраняет WAV-файл на диск.</summary>
    public void Save(MorseSignal signal, string path)
    {
        File.WriteAllBytes(path, Generate(signal));
    }

    private short[] CreateSamples(MorseSignal signal)
    {
        var samples = new short[MsToSamples(signal.TotalMs)];
        int fadeSamples = MsToSamples(FadeMs);
        int timeMs = 0;

        foreach (var element in signal.Elements)
        {
            // Считаем начало и конец от общего времени, чтобы звук не расходился со светом
            int start = MsToSamples(timeMs);
            timeMs += element.DurationMs;
            int end = MsToSamples(timeMs);

            if (!element.IsOn)
                continue;

            int length = end - start;
            int fade = Math.Min(fadeSamples, length / 2);
            for (int i = 0; i < length; i++)
            {
                double value = Math.Sin(2 * Math.PI * FrequencyHz * i / SampleRate);

                // Плавное нарастание в начале и затухание в конце
                double gain = 1.0;
                if (i < fade)
                    gain = (double)i / fade;
                else if (i >= length - fade)
                    gain = (double)(length - i) / fade;

                samples[start + i] = (short)(value * gain * Volume * short.MaxValue);
            }
        }

        return samples;
    }

    private static int MsToSamples(int ms) => (int)((long)ms * SampleRate / 1000);
}
