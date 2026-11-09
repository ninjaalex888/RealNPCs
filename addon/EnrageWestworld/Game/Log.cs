// RealNPCs.log next to the ini. Thread-safe (the voice thread can write too). Lines from the game thread are gathered and written a couple of times a second, so a busy moment can't stall a frame on the disk once
// per line (TENHOUR item 1: the ring can let twelve people go in one frame). No thread of our own: one left running can hold up a script reload. An error, and anything from another thread (the hang watch), goes out at once, with whatever was waiting: it may be the last thing the game does.
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace EnrageWestworld.InGame
{
    internal static class Log
    {
        private static readonly object Gate = new object();
        private static string _path;
        private static readonly StringBuilder Waiting = new StringBuilder();
        private static Thread _game;     // the game's thread, from its first Tick; until then every line goes out at once
        private static int _flushedAt;
        private const int FlushMs = 500, MostWaiting = 16384;

        public static bool Debug;

        public static void Open(string path)
        {
            _path = path;
            try
            {
                if (File.Exists(path) && new FileInfo(path).Length > 2 * 1024 * 1024)
                    File.Delete(path);
                File.AppendAllText(path, "\r\n==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ====\r\n");
            }
            catch
            {
            }
        }

        public static void Write(string line)
        {
            if (_path == null)
                return;
            string text = DateTime.Now.ToString("HH:mm:ss") + " " + line + "\r\n";
            lock (Gate)
            {
                Waiting.Append(text);
                if (Thread.CurrentThread != _game || Waiting.Length > MostWaiting || (line != null && line.StartsWith("ERROR", StringComparison.Ordinal)))
                    FlushLocked();
            }
        }

        /// Once a frame from the game's thread: what's waiting goes out, a couple of times a second.
        public static void Tick(int now)
        {
            _game = Thread.CurrentThread;
            if (now - _flushedAt < FlushMs && now >= _flushedAt)
                return;
            _flushedAt = now;
            Flush();
        }

        /// Everything waiting, now (the script stopping).
        public static void Flush()
        {
            lock (Gate)
                FlushLocked();
        }

        private static void FlushLocked()
        {
            if (Waiting.Length == 0 || _path == null)
                return;
            try
            {
                File.AppendAllText(_path, Waiting.ToString());
            }
            catch
            {
            }
            Waiting.Clear();
        }

        public static void Detail(string line)
        {
            if (Debug)
                Write(line);
        }
    }
}
