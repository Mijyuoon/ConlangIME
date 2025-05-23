﻿using System;

namespace ConlangIME {
    public class StringReaderEx {
        const char EOF_CHAR = '\0';

        private string buffer;
        private int length;
        private int offset;

        public StringReaderEx(string str, int len) {
            this.buffer = str;
            this.length = len;
            this.offset = 0;
        }

        public StringReaderEx(string str) : this(str, str.Length) { }

        public char this[int i] => (i < 0 || i >= length) ? EOF_CHAR : buffer[i];

        public int Position {
            get => offset;
            set => offset = Math.Min(Math.Max(value, 0), length);
        }

        public bool IsEOF => (offset >= length);

        public char Peek(int n) => this[offset + n - 1];

        public bool Advance(int n) {
            int old = offset;
            Position = offset + n;
            return old != offset;
        }

        public char Read() {
            char ch = Peek(1);
            Position += 1;
            return ch;
        }

        public string Read(int n) {
            int len = Math.Min(n, length - offset);
            string s = buffer.Substring(offset, len);
            Position += len;
            return s;
        }

        public string ReadWhile(Func<char, bool> func) {
            int npos = offset;

            while(npos < length) {
                char ch = buffer[npos];
                if(!func(ch)) break;
                npos++;
            }

            int len = Math.Min(npos, length) - offset;
            string s = buffer.Substring(offset, len);
            Position += len;
            return s;
        }

        public bool Backtrack(Func<bool> func) {
            int old = offset;
            bool keep = false;
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
}
