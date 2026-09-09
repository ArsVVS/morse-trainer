using MorseTrainer.Core;

namespace MorseTrainer.Core.Tests;

public class TranslatorTests
{
    [Fact]
    public void Encode_Latin_LettersAndWords()
    {
        var result = new MorseTranslator().Encode("SOS help");

        Assert.Equal("... --- ... / .... . .-.. .--.", result.Result);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Encode_CyrillicAndDigits()
    {
        var translator = new MorseTranslator();

        Assert.Equal(".--. .-. .. .-- . - / ....- ..---", translator.Encode("Привет 42").Result);
        // Ё кодируется как Е
        Assert.Equal(". ...-", translator.Encode("ёж").Result);
    }

    [Fact]
    public void Decode_Latin_WordSeparators()
    {
        var translator = new MorseTranslator();

        Assert.Equal("SOS HE", translator.Decode("... --- ... / .... .").Result);
        Assert.Equal("S O", translator.Decode("...   ---").Result);
    }

    [Fact]
    public void Decode_Cyrillic_UsesSelectedAlphabet()
    {
        var translator = new MorseTranslator(AlphabetKind.Cyrillic);

        Assert.Equal("ВОЛНА 1", translator.Decode(".-- --- .-.. -. .- / .----").Result);
    }

    [Fact]
    public void Decode_NormalizesSimilarSymbols()
    {
        var result = new MorseTranslator().Decode("··· ___ ••• / —·—");

        Assert.Equal("SOS K", result.Result);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Encode_UnknownSymbols_AreSkippedWithWarning()
    {
        var result = new MorseTranslator().Encode("A#B");

        Assert.Equal(".- -...", result.Result);
        Assert.Single(result.Warnings);
        Assert.Contains("#", result.Warnings[0]);
    }

    [Fact]
    public void Decode_UnknownCombination_GivesQuestionMark()
    {
        var result = new MorseTranslator().Decode("...... .-");

        Assert.Equal("?A", result.Result);
        Assert.Single(result.Warnings);
        Assert.Contains("позиция 1", result.Warnings[0]);
    }
}
