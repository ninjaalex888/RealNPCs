// The AI model found by itself (Lex, 2026-10-31: "auto-detect"; README "RealNPCs finds Ollama and the model by itself").
// Once at a start, off the game thread with a short timeout, Ollama is asked at LanguageUrl /api/tags whether it runs and
// holds LanguageModel. Found, with [Mind] Language off and the player never having chosen (LanguageChosen false), and not
// turned on this way before (the save's "told:ai model"), Language is turned on in the ini and a one-time notice says so.
// Whenever the player changes Language (the panel, or the ini by hand), LanguageChosen is set, so a model turned off stays
// off. The settings panel's AI MODEL (OLLAMA) row reads AiStatusText. Log lines `LANGUAGE detect:`.
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int AiDetectTimeoutMs = 2500;
        private volatile int _aiFound;           // 0 not asked yet / under way, 1 found with the model, 2 answers without the model, 3 not reachable
        private volatile string _aiFoundWhy;
        private bool _aiDetectStarted, _aiDetectDone, _aiAutoWriting;
        private string _aiNotAnswering, _aiNotAnsweringModel;

        /// Once a second (from LanguageStep): the start's question to Ollama, and what follows from its answer.
        private void AiDetectStep()
        {
            if (_aiDetectDone || _cfg == null)
                return;
            if (!_aiDetectStarted)
            {
                _aiDetectStarted = true;
                if (_cfg.Language)
                {
                    _aiDetectDone = true;   // on already: the worker's own check of the server says whether it answers
                    Log.Write("LANGUAGE detect: [Mind] Language is on already" + (_cfg.LanguageChosen ? " (your choice)" : "") + "; the worker checks the server");
                    return;
                }
                if (_cfg.LanguageChosen)
                {
                    _aiDetectDone = true;
                    Log.Write("LANGUAGE detect: not asked ([Mind] Language is off by your choice, LanguageChosen)");
                    return;
                }
                string url = _cfg.LanguageUrl, model = _cfg.LanguageModel;
                var t = new Thread(() => AiDetectWork(url, model)) { IsBackground = true, Name = "RealNPCs AI detect", Priority = ThreadPriority.BelowNormal };
                t.Start();
                return;
            }
            if (_aiFound == 0)
                return;   // still asking
            if (_aiFound != 1)
            {
                _aiDetectDone = true;
                Log.Write("LANGUAGE detect: " + _aiFoundWhy + "; [Mind] Language stays off (asked again at the next start)");
                return;
            }
            if (_store == null)
                return;   // found: wait for the saved data, which knows whether it was turned on this way before
            _aiDetectDone = true;
            if (_cfg.Language || _cfg.LanguageChosen)
            {
                Log.Write("LANGUAGE detect: " + _aiFoundWhy + "; [Mind] Language was set meanwhile, left as it is");
                return;
            }
            if (_store.Count("told:ai model") > 0)
            {
                Log.Write("LANGUAGE detect: " + _aiFoundWhy + "; turned on once before and off since: left off");
                return;
            }
            string err = AiWriteIni("Language", "true");
            if (err != null)
            {
                Log.Write("LANGUAGE detect: " + _aiFoundWhy + ", but RealNPCs.ini couldn't be changed (" + err + "); [Mind] Language stays off");
                return;
            }
            _store.Bump("told:ai model");
            _aiAutoWriting = true;
            try
            {
                ApplyOurSettings("Language");
            }
            finally
            {
                _aiAutoWriting = false;
            }
            KeyNoticeOut("AI model found: people near you now think in their own words. Turn it off on the Essentials page (" + _settingsKey + ").");
            Log.Write("LANGUAGE detect: " + _aiFoundWhy + "; [Mind] Language turned on (you hadn't chosen), and you were told");
        }

        /// The worker thread's one question: no game natives, the answer left in two volatile fields.
        private void AiDetectWork(string url, string model)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url + "/api/tags");
                req.Method = "GET";
                req.Timeout = AiDetectTimeoutMs;
                req.ReadWriteTimeout = AiDetectTimeoutMs;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string body = reader.ReadToEnd();
                    bool has = body.IndexOf("\"name\":\"" + model + "\"", StringComparison.Ordinal) >= 0 ||
                               (model.IndexOf(':') < 0 && body.IndexOf("\"name\":\"" + model + ":", StringComparison.Ordinal) >= 0);
                    _aiFoundWhy = has ? "Ollama at " + url + " holds " + model : "Ollama answers at " + url + " but doesn't hold " + model + " (ollama pull " + model + ")";
                    _aiFound = has ? 1 : 2;
                }
            }
            catch (Exception ex)
            {
                _aiFoundWhy = "no Ollama answering at " + url + " (" + ex.Message + ")";
                _aiFound = 3;
            }
        }

        /// One value of RealNPCs.ini changed in place (the rest of the file as it was). Null when it worked.
        private string AiWriteIni(string key, string value)
        {
            IniDoc doc = IniDoc.Read(_iniPath, true);
            if (doc.Error != null)
                return doc.Error;
            IniEntry e = doc.FindKey(key);
            if (e == null)
                return key + " isn't in the file";
            if (!doc.Set(e, value))
                return "couldn't set " + key;
            return doc.Save();
        }

        /// The player changed Language (the panel, or the ini by hand): kept as their choice from now on.
        private void AiChosen(EnrageWestworld.Core.Settings fresh, string how)
        {
            if (_aiAutoWriting || fresh == null || fresh.LanguageChosen)
                return;
            string err = AiWriteIni("LanguageChosen", "true");
            if (err != null)
            {
                Log.Write("LANGUAGE chosen: couldn't note it in RealNPCs.ini (" + err + ")");
                return;
            }
            fresh.LanguageChosen = true;
            _iniStamp = EnrageWestworld.Core.Settings.Stamp(_iniPath);
            Log.Write("LANGUAGE chosen: you set [Mind] Language " + (fresh.Language ? "on" : "off") + " (" + how + "); RealNPCs leaves it as you set it (LanguageChosen = true)");
        }

        /// The settings panel's AI MODEL (OLLAMA) row: off, on and answering, on and still checking, or on and not answering.
        private string AiStatusText()
        {
            if (_cfg == null || !_cfg.Language)
                return "off";
            if (_langReady)
                return "on, answering";
            if (_langThread == null || _langServer == "not checked")
                return "on, checking...";
            if (_aiNotAnswering == null || _aiNotAnsweringModel != _cfg.LanguageModel)
            {
                _aiNotAnsweringModel = _cfg.LanguageModel;
                _aiNotAnswering = "on, NOT ANSWERING (is Ollama running? is " + _cfg.LanguageModel + " pulled?)";
            }
            return _aiNotAnswering;
        }

        /// F7: what the start's question found.
        private string AiDetectStatus()
        {
            return !_aiDetectStarted ? "not asked yet" : _aiFound == 0 && !_aiDetectDone ? "asking" : _aiFoundWhy ?? (_cfg.Language ? "Language on already" : "not asked (your choice)");
        }
    }
}
