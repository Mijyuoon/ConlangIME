using System;

namespace ConlangIME.Core;

public class StringReaderEx(string buffer, int length)
{
    const char EOF_CHAR = '\0';

    private int _position = 0;

    public StringReaderEx(string str) : this(str, str.Length) { }

    public char this[int i] => (i < 0 || i >= length) ? EOF_CHAR : buffer[i];

    public int Position {
        get => _position;
        set => _position = Math.Min(Math.Max(value, 0), length);
    }

    public bool IsEOF => (Position >= length);

    public char Peek(int n) => this[Position + n - 1];

    public bool Advance(int n) {
        var old = Position;

        Position += n;
        return old != Position;
    }

    public char Read() {
        var ch = Peek(1);

        Position += 1;
        return ch;
    }

    public string Read(int n) {
        var len = Math.Min(n, length - Position);
        var str = buffer.Substring(Position, len);

        Position += len;
        return str;
    }

    public string ReadWhile(Func<char, bool> func) {
        var pos = Position;

        while(pos < length) {
            if(!func(buffer[pos])) break;
            pos++;
        }

        var len = Math.Min(pos, length) - Position;
        var str = buffer.Substring(Position, len);

        Position += len;
        return str;
    }

    public bool Backtrack(Func<bool> func) {
        var old = Position;
        var keep = false;

        try {
            keep = func();
            return keep;
        } finally {
            if(!keep) {
                Position = old;
            }
        }
    }
}