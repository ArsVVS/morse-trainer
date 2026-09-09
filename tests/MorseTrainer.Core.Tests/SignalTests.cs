using System.Text;
using MorseTrainer.Core;

namespace MorseTrainer.Core.Tests;

public class SignalTests
{
    [Theory]
    [InlineData(20, 60)]
    [InlineData(12, 100)]
    public void Timing_DotLengthDependsOnWpm(int wpm, int dotMs)
    {
        var timing = new MorseTiming(wpm);

        Assert.Equal(dotMs, timing.DotMs);
        Assert.Equal(dotMs * 3, timing.DashMs);
        Assert.Equal(dotMs * 7, timing.WordGapMs);
    }

    [Fact]
    public void Signal_Paris_Is50Units()
    {
        var timing = new MorseTiming(20);
        var signal = MorseSignal.FromText("PARIS", timing);

        // Слово PARIS вместе с паузой после него – ровно 50 единиц
        Assert.Equal(50 * timing.DotMs, signal.TotalMs + timing.WordGapMs);
    }

    [Fact]
    public void Signal_CharIndex_PointsToSourceText()
    {
        var signal = MorseSignal.FromText("A  B", new MorseTiming(20));

        Assert.Equal(0, signal.Elements[0].CharIndex);
        Assert.Equal(3, signal.Elements[^1].CharIndex);
    }

    [Fact]
    public void Wav_HeaderAndDataLength()
    {
        // Одна точка на 20 WPM – 60 мс, это 2646 отсчётов по 2 байта
        var signal = MorseSignal.FromText("E", new MorseTiming(20));
        byte[] wav = new WavGenerator(700, 0.5).Generate(signal);

        Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
        Assert.Equal(44100, BitConverter.ToInt32(wav, 24));
        Assert.Equal(16, BitConverter.ToInt16(wav, 34));
        Assert.Equal(2646 * 2, BitConverter.ToInt32(wav, 40));
        Assert.Equal(44 + 2646 * 2, wav.Length);
    }
}
