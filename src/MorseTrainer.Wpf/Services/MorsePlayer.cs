using System.Diagnostics;
using System.IO;
using MorseTrainer.Core;
using NAudio.Wave;

namespace MorseTrainer.Wpf.Services;

/// <summary>Проигрывает сигнал звуком и/или светом. Звук и свет идут по одним и тем же часам.</summary>
public class MorsePlayer
{
    private CancellationTokenSource? _cancel;

    public bool IsPlaying => _cancel != null;

    /// <summary>
    /// Проигрывает сигнал. onChange вызывается с состоянием лампы и индексом текущего символа
    /// (в конце – false и -1).
    /// </summary>
    public async Task PlayAsync(MorseSignal signal, bool sound, bool light, int frequencyHz, double volume,
        Action<bool, int>? onChange = null)
    {
        Stop();
        var cancel = new CancellationTokenSource();
        _cancel = cancel;

        WaveFileReader? reader = null;
        WaveOut? output = null;
        try
        {
            if (sound)
            {
                byte[] wav = new WavGenerator(frequencyHz, volume).Generate(signal);
                reader = new WaveFileReader(new MemoryStream(wav));
                output = new WaveOut { BufferMilliseconds = 50 };
                output.Init(reader);
                output.Play();
            }

            var clock = Stopwatch.StartNew();
            while (!cancel.IsCancellationRequested)
            {
                // Если играет звук, время берём по тому, сколько звука уже проиграно, –
                // так лампа не убегает вперёд звука
                int timeMs = output != null && reader != null
                    ? (int)(output.GetPosition() * 1000 / reader.WaveFormat.AverageBytesPerSecond)
                    : (int)clock.ElapsedMilliseconds;

                int index = signal.FindElement(timeMs);
                if (index < 0 || (output != null && output.PlaybackState == PlaybackState.Stopped))
                    break;

                var element = signal.Elements[index];
                onChange?.Invoke(light && element.IsOn, element.CharIndex);
                await Task.Delay(10);
            }
        }
        finally
        {
            output?.Stop();
            output?.Dispose();
            reader?.Dispose();
            if (_cancel == cancel)
                _cancel = null;
            onChange?.Invoke(false, -1);
        }
    }

    public void Stop()
    {
        _cancel?.Cancel();
        _cancel = null;
    }
}
