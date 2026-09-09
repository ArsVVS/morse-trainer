using System.Text;

namespace MorseTrainer.Core;

/// <summary>Переводит нажатия ключа (пробела) в точки и тире.</summary>
public class KeyingDecoder
{
    private readonly StringBuilder _code = new();
    private bool _pressed;
    private TimeSpan _pressTime;
    private TimeSpan? _releaseTime;
    private bool _letterEnded;
    private bool _wordEnded;

    public KeyingDecoder(int wpm)
    {
        Wpm = wpm;
    }

    /// <summary>Скорость, от неё зависит длина единицы.</summary>
    public int Wpm { get; set; }

    /// <summary>Введённый код, например ".- / -...".</summary>
    public string Code => _code.ToString().Trim();

    public bool IsPressed => _pressed;

    /// <summary>Срабатывает при каждом новом кусочке кода: ".", "-", " " (конец буквы) или "/ " (конец слова).</summary>
    public event EventHandler<string>? SymbolDecoded;

    private double UnitMs => 1200.0 / Wpm;

    /// <summary>Ключ нажат.</summary>
    public void KeyDown(TimeSpan time)
    {
        if (_pressed)
            return;

        CheckPause(time);
        _pressed = true;
        _pressTime = time;
    }

    /// <summary>Ключ отпущен.</summary>
    public void KeyUp(TimeSpan time)
    {
        if (!_pressed)
            return;

        _pressed = false;
        double duration = (time - _pressTime).TotalMilliseconds;

        // Короче двух единиц – точка, иначе тире
        Append(duration < UnitMs * 2 ? "." : "-");

        _releaseTime = time;
        _letterEnded = false;
        _wordEnded = false;
    }

    /// <summary>Вызывается по таймеру, чтобы заметить конец буквы или слова во время паузы.</summary>
    public void Tick(TimeSpan time)
    {
        CheckPause(time);
    }

    public void Reset()
    {
        _code.Clear();
        _pressed = false;
        _releaseTime = null;
        _letterEnded = false;
        _wordEnded = false;
    }

    private void CheckPause(TimeSpan time)
    {
        if (_pressed || _releaseTime == null)
            return;

        double pause = (time - _releaseTime.Value).TotalMilliseconds;

        // Пауза от 3 единиц – конец буквы
        if (pause >= UnitMs * 3 && !_letterEnded)
        {
            Append(" ");
            _letterEnded = true;
        }

        // Пауза от 7 единиц – конец слова
        if (pause >= UnitMs * 7 && !_wordEnded)
        {
            Append("/ ");
            _wordEnded = true;
        }
    }

    private void Append(string part)
    {
        _code.Append(part);
        SymbolDecoded?.Invoke(this, part);
    }
}
