// The player's own translation, the game side (Core/Translations.cs has the file's rules and the lookup; Run 3, item 8). With
// [Output] Translate on, translation.txt in the RealNPCs folder (beside the saves) is read at the start and again whenever it
// changes (looked at every ten seconds), so a translator can save the file and see the next line in their language. Gone,
// emptied or switched off, everything shows in English again. The Testing row "Write the translation template" writes
// translation-template.txt there: every line RealNPCs has, each followed by a TAB, ready to fill in.
using System;
using System.IO;
using System.Linq;
using System.Text;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _nextTranslationLook;
        private DateTime _translationStamp = DateTime.MinValue;
        private string _translationLast = "no translation.txt read";

        private string TranslationPath { get { return Path.Combine(_dir, "translation.txt"); } }

        /// Every ten seconds: the file read again if it changed, or let go if it's gone or the switch is off.
        private void TranslationStep(int now)
        {
            if (now - _nextTranslationLook < 0)
                return;
            _nextTranslationLook = now + 10000;
            try
            {
                string path = TranslationPath;
                bool there = _cfg != null && _cfg.Translate && File.Exists(path);
                if (!there)
                {
                    if (Translations.On || _translationStamp != DateTime.MinValue)
                    {
                        Translations.Clear();
                        _translationStamp = DateTime.MinValue;
                        _translationLast = _cfg != null && _cfg.Translate ? "translation.txt is gone: English again" : "off ([Output] Translate)";
                        Log.Write("TRANSLATION " + _translationLast);
                    }
                    return;
                }
                DateTime stamp = File.GetLastWriteTimeUtc(path);
                if (stamp == _translationStamp)
                    return;
                _translationStamp = stamp;
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                int skipped;
                int n = Translations.Load(lines, out skipped);
                Translations.On = n > 0;
                _translationLast = n + " entries (" + Translations.Templates + " with {slots}) from translation.txt" + (skipped > 0 ? ", " + skipped + " lines not entries (no TAB, or nothing before it)" : "");
                Log.Write("TRANSLATION read " + _translationLast);
            }
            catch (Exception ex)
            {
                _translationLast = "translation.txt couldn't be read: " + ex.Message;
                Log.Write("TRANSLATION " + _translationLast);
            }
        }

        /// The Testing row: every line RealNPCs has, written to translation-template.txt for a translator to fill in.
        private string TranslationTemplate(out string said)
        {
            said = null;
            try
            {
                var lines = Translations.AllLines();
                string path = Path.Combine(_dir, "translation-template.txt");
                File.WriteAllText(path, Translations.TemplateText(lines), new UTF8Encoding(false));
                said = lines.Count + " lines written to translation-template.txt in the RealNPCs folder";
                Log.Write("TRANSLATION template: " + lines.Count + " lines written to " + path);
                return null;
            }
            catch (Exception ex)
            {
                return "the template couldn't be written: " + ex.Message;
            }
        }

        /// F7: what's read, and how many lines found their translation since.
        private string TranslationStatus()
        {
            return (_cfg != null && !_cfg.Translate ? "off ([Output] Translate); " : "") + _translationLast + (Translations.On ? "; " + Translations.Hits + " lines shown translated, " + Translations.Misses + " with no entry" : "");
        }
    }
}
