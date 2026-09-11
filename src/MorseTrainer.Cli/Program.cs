using System.CommandLine;
using MorseTrainer.Cli;
using MorseTrainer.Core;

// Коды возврата: 0 – успех, 1 – ошибка аргументов, 2 – ошибка ввода или файла
const int Ok = 0;
const int InputError = 2;

// Чтобы русский текст нормально выводился в консоли
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

var settings = AppSettings.Load();

// Общие параметры
var alphabetOption = new Option<string>("--alphabet", "-a")
{
    Description = "Алфавит: lat (латиница) или cyr (кириллица)",
    DefaultValueFactory = _ => settings.Alphabet == AlphabetKind.Latin ? "lat" : "cyr"
};
alphabetOption.AcceptOnlyFromAmong("lat", "cyr");

var wpmOption = new Option<int>("--wpm")
{
    Description = $"Скорость в словах в минуту ({MorseTiming.MinWpm}–{MorseTiming.MaxWpm})",
    DefaultValueFactory = _ => settings.Wpm
};
wpmOption.Validators.Add(result =>
{
    int value = result.GetValueOrDefault<int>();
    if (value < MorseTiming.MinWpm || value > MorseTiming.MaxWpm)
        result.AddError($"Скорость должна быть от {MorseTiming.MinWpm} до {MorseTiming.MaxWpm} WPM");
});

var freqOption = new Option<int>("--freq")
{
    Description = $"Частота тона в герцах ({WavGenerator.MinFrequency}–{WavGenerator.MaxFrequency})",
    DefaultValueFactory = _ => settings.FrequencyHz
};
freqOption.Validators.Add(result =>
{
    int value = result.GetValueOrDefault<int>();
    if (value < WavGenerator.MinFrequency || value > WavGenerator.MaxFrequency)
        result.AddError($"Частота должна быть от {WavGenerator.MinFrequency} до {WavGenerator.MaxFrequency} Гц");
});

var strictOption = new Option<bool>("--strict")
{
    Description = "Завершиться с кодом 2, если есть неизвестные символы"
};

// morse encode
var textArgument = new Argument<string>("text") { Description = "Текст для перевода" };
var wavOption = new Option<string?>("--wav") { Description = "Сохранить звук в WAV-файл" };
var svgOption = new Option<string?>("--svg") { Description = "Сохранить картинку в SVG-файл" };

var encodeCommand = new Command("encode", "Перевести текст в код Морзе")
{
    textArgument, alphabetOption, wavOption, svgOption, wpmOption, freqOption, strictOption
};
encodeCommand.SetAction(parse =>
{
    var result = new MorseTranslator().Encode(parse.GetValue(textArgument)!);
    Console.WriteLine(result.Result);
    PrintWarnings(result.Warnings);

    try
    {
        string? wavPath = parse.GetValue(wavOption);
        if (wavPath != null)
        {
            var signal = MorseSignal.FromCode(result.Result, new MorseTiming(parse.GetValue(wpmOption)));
            new WavGenerator(parse.GetValue(freqOption), settings.Volume).Save(signal, wavPath);
            Console.WriteLine($"Звук сохранён: {wavPath}");
        }

        string? svgPath = parse.GetValue(svgOption);
        if (svgPath != null)
        {
            string svg = new MorseImageRenderer().Render(result.Result, ParseAlphabet(parse.GetValue(alphabetOption)));
            File.WriteAllText(svgPath, svg);
            Console.WriteLine($"Картинка сохранена: {svgPath}");
        }
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
    {
        Console.Error.WriteLine($"Ошибка записи файла: {ex.Message}");
        return InputError;
    }

    return parse.GetValue(strictOption) && result.Warnings.Count > 0 ? InputError : Ok;
});

// morse decode
var codeArgument = new Argument<string>("code") { Description = "Код Морзе, например \"... --- ...\"" };
var decodeCommand = new Command("decode", "Перевести код Морзе в текст")
{
    codeArgument, alphabetOption, strictOption
};
decodeCommand.SetAction(parse =>
{
    var translator = new MorseTranslator(ParseAlphabet(parse.GetValue(alphabetOption)));
    var result = translator.Decode(parse.GetValue(codeArgument)!);
    Console.WriteLine(result.Result);
    PrintWarnings(result.Warnings);
    return parse.GetValue(strictOption) && result.Warnings.Count > 0 ? InputError : Ok;
});

// morse play
var playArgument = new Argument<string>("text") { Description = "Текст или код Морзе" };
var playCommand = new Command("play", "Проиграть звук в консоли")
{
    playArgument, wpmOption, freqOption
};
playCommand.SetAction(parse =>
{
    string input = parse.GetValue(playArgument)!;
    var timing = new MorseTiming(parse.GetValue(wpmOption));

    // Если ввели код – играем его как есть
    string code = MorseTranslator.LooksLikeCode(input) ? input : new MorseTranslator().Encode(input).Result;
    Console.WriteLine(code);

    try
    {
        ConsolePlayer.Play(MorseSignal.FromCode(code, timing), parse.GetValue(freqOption), settings.Volume);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Не удалось воспроизвести звук: {ex.Message}");
        return InputError;
    }
    return Ok;
});

// morse train
var modeOption = new Option<string>("--mode", "-m")
{
    Description = "Режим: receive (приём) или send (передача)",
    Required = true
};
modeOption.AcceptOnlyFromAmong("receive", "send");

var levelOption = new Option<int?>("--level", "-l") { Description = "Уровень (по умолчанию – текущий из статистики)" };
var countOption = new Option<int>("--count", "-c")
{
    Description = "Количество заданий",
    DefaultValueFactory = _ => settings.TaskCount
};
countOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() < 1)
        result.AddError("Количество заданий должно быть больше нуля");
});

var trainCommand = new Command("train", "Тренировка в консоли")
{
    modeOption, levelOption, countOption, alphabetOption, wpmOption
};
trainCommand.SetAction(parse =>
{
    var options = new TrainingOptions
    {
        Mode = parse.GetValue(modeOption) == "send" ? TrainingMode.Send : TrainingMode.Receive,
        Alphabet = ParseAlphabet(parse.GetValue(alphabetOption)),
        Count = parse.GetValue(countOption),
        Wpm = parse.GetValue(wpmOption),
        TaskKind = TaskKind.Groups
    };
    var trainer = new ConsoleTrainer(settings);
    return trainer.Run(options, parse.GetValue(levelOption));
});

// morse stats
var statsCommand = new Command("stats", "Краткая статистика");
statsCommand.SetAction(_ =>
{
    ConsoleTrainer.PrintStatistics(new StatisticsStore());
    return Ok;
});

var root = new RootCommand("Азбука Морзе – тренажёр: перевод, звук, картинка и тренировка в консоли")
{
    encodeCommand, decodeCommand, playCommand, trainCommand, statsCommand
};

return root.Parse(args).Invoke();

static AlphabetKind ParseAlphabet(string? value) =>
    value == "cyr" ? AlphabetKind.Cyrillic : AlphabetKind.Latin;

static void PrintWarnings(IReadOnlyList<string> warnings)
{
    foreach (string warning in warnings)
        Console.Error.WriteLine($"Внимание: {warning}");
}
