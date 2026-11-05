// The last body read, kept on disk while a read runs (Run 3, item 5: the recognition plan's Stage 0d, in this add-on's own
// words and file). A small fixed file, lastread.txt in the RealNPCs folder (beside the saves), mapped into memory: before each native call of a
// looks read it says which body, which part and which call, so if the game hangs or dies inside one the file still names it,
// where a log line per call would be too slow and a hard crash leaves the log short. Only the probe row reads looks, so only
// it writes here; between reads the file says nothing is being read.
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace EnrageWestworld.InGame
{
    internal static class ReadMarks
    {
        private const int Size = 192;
        private static string _path;
        private static MemoryMappedFile _lastReadFile;
        private static MemoryMappedViewAccessor _window;
        private static bool _failed;

        /// Where the file goes (the RealNPCs folder); nothing is opened until the first mark.
        public static void Open(string folder)
        {
            if (!string.IsNullOrEmpty(folder))
                _path = Path.Combine(folder, "lastread.txt");
        }

        /// What is being read now: the body, the piece and the call.
        public static void Set(string reading)
        {
            if (_failed || _path == null)
                return;
            try
            {
                if (_window == null)
                {
                    _lastReadFile = MemoryMappedFile.CreateFromFile(_path, FileMode.OpenOrCreate, null, Size);
                    _window = _lastReadFile.CreateViewAccessor(0, Size);
                }
                string line = DateTime.Now.ToString("HH:mm:ss") + " reading " + (reading ?? "");
                if (line.Length > Size - 2)
                    line = line.Substring(0, Size - 2);
                byte[] bytes = Encoding.ASCII.GetBytes(line.PadRight(Size - 2) + "\r\n");
                _window.WriteArray(0, bytes, 0, bytes.Length);
            }
            catch
            {
                _failed = true;   // a file we can't make: the reads go on without their marks
                Close();
            }
        }

        /// The read is over.
        public static void Done()
        {
            Set("nothing: the last read finished");
        }

        public static void Close()
        {
            try
            {
                if (_window != null)
                    _window.Dispose();
                if (_lastReadFile != null)
                    _lastReadFile.Dispose();
            }
            catch
            {
            }
            _window = null;
            _lastReadFile = null;
        }
    }
}
