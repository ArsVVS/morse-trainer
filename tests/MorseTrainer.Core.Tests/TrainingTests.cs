using MorseTrainer.Core;

namespace MorseTrainer.Core.Tests;

public class TrainingTests
{
    private static TimeSpan Ms(int ms) => TimeSpan.FromMilliseconds(ms);

    [Fact]
    public void Keying_ShortPressIsDot_LongPressIsDash()
    {
        // 20 WPM – единица 60 мс
        var decoder = new KeyingDecoder(20);

        decoder.KeyDown(Ms(0));
        decoder.KeyUp(Ms(50));
        decoder.KeyDown(Ms(150));
        decoder.KeyUp(Ms(350));

        Assert.Equal(".-", decoder.Code);
    }

    [Fact]
    public void Keying_PausesSplitLettersAndWords()
    {
        var decoder = new KeyingDecoder(20);
        var parts = new List<string>();
        decoder.SymbolDecoded += (_, part) => parts.Add(part);

        decoder.KeyDown(Ms(0));
        decoder.KeyUp(Ms(60));
        decoder.KeyDown(Ms(360));   // пауза 300 мс – конец буквы
        decoder.KeyUp(Ms(420));
        decoder.Tick(Ms(920));      // пауза 500 мс – конец слова
        decoder.KeyDown(Ms(1000));
        decoder.KeyUp(Ms(1200));

        Assert.Equal(". . / -", decoder.Code);
        Assert.Equal([".", " ", ".", " ", "/ ", "-"], parts);
        Assert.Equal("EE T", new MorseTranslator().Decode(decoder.Code).Result);
    }

    [Fact]
    public void Levels_SymbolsForLevel()
    {
        Assert.Equal("KM", KochLevels.GetSymbols(AlphabetKind.Latin, 1));
        Assert.Equal("KMRS", KochLevels.GetSymbols(AlphabetKind.Latin, 3));
        Assert.Equal("ЕТ", KochLevels.GetSymbols(AlphabetKind.Cyrillic, 1));
        Assert.Equal(KochLevels.LatinOrder, KochLevels.GetSymbols(AlphabetKind.Latin, 100));
    }

    [Fact]
    public void Session_AnswerIsComparedBySymbols()
    {
        var options = new TrainingOptions { Level = 1, TaskKind = TaskKind.Groups, Count = 2 };
        var session = new TrainingSession(options, new Random(1));

        string expected = session.CurrentText;
        var result = session.SubmitAnswer(expected[..4].ToLower() + "X");

        Assert.False(result.IsCorrect);
        Assert.Equal([new CharError(expected[4].ToString(), "X")], result.Errors);

        // В режиме передачи ответ вводится кодом
        var send = new TrainingSession(new TrainingOptions { Mode = TrainingMode.Send, Count = 1 }, new Random(2));
        Assert.True(send.SubmitAnswer(send.CurrentCode).IsCorrect);
    }

    [Fact]
    public void Session_AccuracyAndLevelUp()
    {
        var session = new TrainingSession(new TrainingOptions { Count = 4 }, new Random(3));
        for (int i = 0; i < 3; i++)
            session.SubmitAnswer(session.CurrentText);
        session.SubmitAnswer("");

        var summary = session.GetSummary();
        Assert.Equal(0.75, summary.Accuracy);
        Assert.Equal(3, summary.CorrectTasks);
        Assert.False(summary.CanLevelUp);

        var good = new TrainingSession(new TrainingOptions { Count = 2 }, new Random(4));
        good.SubmitAnswer(good.CurrentText);
        good.SubmitAnswer(good.CurrentText);
        Assert.True(good.GetSummary().CanLevelUp);
    }

    [Fact]
    public void Statistics_SavesSessionAndFindsProblemSymbols()
    {
        string path = Path.Combine(Path.GetTempPath(), $"morse-stats-{Guid.NewGuid()}.json");
        try
        {
            var store = new StatisticsStore(path);
            var session = new TrainingSession(new TrainingOptions { TaskKind = TaskKind.Symbols, Count = 3 }, new Random(5));
            session.SubmitAnswer("X");
            session.SubmitAnswer(session.CurrentText);
            session.SubmitAnswer(session.CurrentText);
            store.AddSession(session);

            // Загружаем заново из файла
            var loaded = new StatisticsStore(path);
            var problem = Assert.Single(loaded.GetProblemSymbols());

            Assert.Single(loaded.Data.Sessions);
            Assert.Equal(1, problem.Errors);
            Assert.Equal("X", problem.ConfusedWith);
            Assert.Equal(1, loaded.GetLevel(AlphabetKind.Latin, TrainingMode.Receive));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
