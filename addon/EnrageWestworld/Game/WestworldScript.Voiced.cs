// Voiced thoughts and dialogue (2026-11-03; Lex: "voiced thoughts and dialogue"; the plan agreed 2026-10-31). A local voice
// server (voice\voice_server.py, Chatterbox on this PC) says RealNPCs' own words in the person's own voice, cloned from one
// of the game's recordings for their model, out of the player's own export of the voice banks; nothing of Rockstar's
// ships. Two parts, each behind its switch, both off as they come:
//   [Mind] VoicedThoughts  a thought over someone within VoiceMetres is murmured in their voice a moment after it shows (one
//                          at a time, one person once in 90 s, 12 s between); that thought isn't also said aloud by rule.
//   [Mind] VoicedDialogue  what people say near you in our words (the street's scenes, Talk's answers, the encounters' lines)
//                          is said in their voice, a second or two behind the subtitle. The game's own recorded remark, where
//                          one plays, still plays.
// The server plays the sound itself, one voice at a time in the order asked; this side keeps telling it how loud and from
// which side, from the speaker's distance and the camera, while it speaks. The game thread never waits on it: a worker
// thread does the asking, as the language layer's does (an Insert reload aborts the request under way).
// The recording row (Testing, "voicerec"): the nearest person says the game's own line their voice is cloned from while
// the server records it off this PC's output, so a player's own game can give the clips with nothing of Rockstar's shipped.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class VoiceJob
        {
            public string Id, Kind, Name, Text, Model, Person;
            public bool Male;
            public int Handle, AskedAt, WaitSeconds, NextPoll;
            public volatile bool Sent, Over, StopAsked, Played;
            public volatile string State = "waiting", Why, Bank;
            public volatile float Vol = 1f, Pan;
            public volatile int MakeMs;
            // a recording (Kind "capture"): the line the server wants said, in which bank (null: the person's own voice)
            public volatile string Line, Voice, Snr, Path;
            public int Take, PlayedAt;
            public volatile bool Accepted, Spoke, Checked;
        }

        private const int VoiceThoughtGapMs = 12000, VoiceThoughtEachMs = 90000, VoiceMostJobs = 4;
        private const float VoiceRecordMetres = 8f;
        private int _voiceRecStyle, _voiceRecorded, _voiceRecordPoor;
        private readonly object _voiceLock = new object();
        private readonly List<VoiceJob> _voiceJobs = new List<VoiceJob>();
        private Thread _voiceThread;
        private volatile bool _voiceStop, _voiceReady;
        private volatile string _voiceServer = "not checked";
        private static volatile HttpWebRequest _voiceReq;
        private int _voiceNextStep, _voiceThoughtNextAt, _voiceSeq, _voiceLogged;
        private int _voiceThoughtsAsked, _voiceSaidAsked, _voiceSpoken, _voiceHeld, _voiceDropped, _voiceFailed, _voiceSkipped;
        private readonly List<int> _voiceMakeMs = new List<int>();
        private readonly Dictionary<string, int> _voiceThoughtAt = new Dictionary<string, int>();
        private string _voiceLast, _voiceRecentKey, _voiceHeldWhy;
        private int _voiceRecentAt;
        private string _voiceLineKey, _voiceLineText;   // the thought voiced just now: ThoughtsAloud stays quiet for it

        private bool VoiceWanted { get { return _cfg != null && (_cfg.VoicedThoughts || _cfg.VoicedDialogue); } }

        /// Each tick: the worker started or stopped with the switches, and each voice under way told where its speaker is.
        private void VoiceStep(int now)
        {
            if (now < _voiceNextStep && now >= _voiceNextStep - 1000)
                return;
            _voiceNextStep = now + 150;
            if (!VoiceWanted)
            {
                if (_voiceThread != null)
                    VoiceStop();
                return;
            }
            if (_voiceThread == null)
                VoiceStart();
            VoiceJob[] jobs;
            lock (_voiceLock)
                jobs = _voiceJobs.ToArray();
            if (jobs.Length == 0)
                return;
            Vector3 me = Game.Player.Character.Position;
            Vector3 rot = CAM.GET_GAMEPLAY_CAM_ROT(2);
            double yaw = rot.Z * Math.PI / 180.0;
            float rx = (float)Math.Cos(yaw), ry = (float)Math.Sin(yaw);   // the camera's right (its forward is -sin, cos)
            foreach (VoiceJob j in jobs)
            {
                if (j.Over)
                {
                    VoiceSettle(j);
                    continue;
                }
                if (j.Handle == 0 || !ENTITY.DOES_ENTITY_EXIST(j.Handle) || ENTITY.IS_ENTITY_DEAD(j.Handle))
                {
                    j.StopAsked = true;   // gone or dead: the voice stops with them
                    continue;
                }
                Vector3 at = ENTITY.GET_ENTITY_COORDS(j.Handle, true, true);
                float d = at.DistanceTo(me);
                if (d > _cfg.VoiceMetres + 10f)
                {
                    j.StopAsked = true;   // out of earshot
                    continue;
                }
                if (j.Kind == "capture")
                {
                    VoiceRecordStep(j);
                    continue;
                }
                j.Vol = VoiceVolume(d, j.Kind == "thought");
                float dx = at.X - me.X, dy = at.Y - me.Y, len = (float)Math.Sqrt(dx * dx + dy * dy);
                j.Pan = len < 0.8f ? 0f : Math.Max(-0.85f, Math.Min(0.85f, (dx * rx + dy * ry) / len));
            }
        }

        private float VoiceVolume(float metres, bool thought)
        {
            float v = metres <= 3f ? 1f : 1f - (metres - 3f) / Math.Max(4f, _cfg.VoiceMetres * 1.5f);
            return Math.Max(0.12f, Math.Min(1f, v)) * (_cfg.VoiceVolume / 100f);
        }

        /// A finished voice taken off the list and counted.
        private void VoiceSettle(VoiceJob j)
        {
            lock (_voiceLock)
                if (!_voiceJobs.Remove(j))
                    return;
            if (j.Kind == "capture")
            {
                if (j.State == "captured")
                    _voiceRecorded++;
                else
                    _voiceRecordPoor++;
                string how = j.Voice == null ? "their own voice" : "the bank " + j.Voice + " by name";
                Log.Write("VOICE recorded: " + j.Name + " (" + j.Bank + ", " + how + "): " + (j.Line ?? "no line") + " take " + j.Take + ", " +
                    (j.PlayedAt == 0 ? "never played" : (j.Accepted ? "accepted" : "refused") + ", " + (j.Spoke ? "the game spoke" : "no speech heard")) + "; " +
                    j.State + (j.Why != null ? " (" + j.Why + ")" : "") + (j.Snr != null ? ", " + j.Snr + " dB over the sound around it" : "") + (j.Path != null ? " -> " + j.Path : ""));
                _voiceLast = j.Name + " (recorded): " + j.State;
                return;
            }
            if (j.Played)
            {
                _voiceSpoken++;
                if (j.MakeMs > 0)
                {
                    _voiceMakeMs.Add(j.MakeMs);
                    if (_voiceMakeMs.Count > 200)
                        _voiceMakeMs.RemoveAt(0);
                }
                _voiceLast = j.Name + " (" + j.Kind + ", " + j.MakeMs + " ms): " + j.Text;
            }
            else if (j.State == "held" || j.State == "no server")
            {
                _voiceHeld++;
                _voiceHeldWhy = j.Why ?? j.State;
            }
            else if (j.State == "failed")
                _voiceFailed++;
            else
                _voiceDropped++;
            (_voiceLogged++ < 60 ? (Action<string>)Log.Write : Log.Detail)("VOICE: " + j.Name + " (" + j.Kind + ", " + (j.Bank ?? "no voice") + "): " +
                (j.Played ? "said in " + j.MakeMs + " ms" : j.State + (j.Why != null ? " (" + j.Why + ")" : "")) + ": " + j.Text);
        }

        /// A thought just came up over someone near: murmured in their voice, if it's their turn.
        private void VoiceThought(TickerLine line, int now)
        {
            if (_cfg == null || !_cfg.VoicedThoughts || !_voiceReady || line == null || line.Kind != "near" || line.Key == null || string.IsNullOrEmpty(line.Text))
                return;
            if (now < _voiceThoughtNextAt && now >= _voiceThoughtNextAt - VoiceThoughtGapMs)
                return;
            int at;
            if (_voiceThoughtAt.TryGetValue(line.Key, out at) && now - at < VoiceThoughtEachMs && now >= at)
                return;
            lock (_voiceLock)
                if (_voiceJobs.Any(j => j.Kind == "thought" && !j.Over))
                    return;
            NearThought n = _near.FirstOrDefault(x => x.Key == line.Key);
            int h = n != null ? n.Handle : line.Handle;
            string text = VoiceText(line.Text);
            if (text == null || !VoiceMayAsk(h, out _))
                return;
            if (VoiceAsk(h, line.Label, text, "thought", 8))
            {
                _voiceThoughtNextAt = now + VoiceThoughtGapMs;
                _voiceThoughtAt[line.Key] = now;
                if (_voiceThoughtAt.Count > 200)
                    _voiceThoughtAt.Clear();
                _voiceLineKey = line.Key;
                _voiceLineText = line.Text;
                _voiceThoughtsAsked++;
            }
        }

        /// ThoughtsAloud asks this first: the thought being murmured in their voice isn't also said aloud by rule.
        private bool VoicedThisThought(TickerLine line)
        {
            return line != null && _voiceLineKey != null && line.Key == _voiceLineKey && line.Text == _voiceLineText;
        }

        /// Our words said by someone near (a scene, Talk, an encounter): in their own voice, when VoicedDialogue is on.
        private void VoiceSaid(int handle, string label, string text, bool face)
        {
            if (_cfg == null || !_cfg.VoicedDialogue || !_voiceReady || handle == 0)
                return;
            text = VoiceText(text);
            if (text == null)
                return;
            int now = Environment.TickCount;
            string key = handle + "|" + text;
            if (key == _voiceRecentKey && now - _voiceRecentAt < 8000)
                return;   // the same line passed on twice (a scene line both captioned and heard)
            string why;
            if (!VoiceMayAsk(handle, out why))
                return;
            if (VoiceAsk(handle, label, text, "said", face ? 15 : 9))
            {
                _voiceRecentKey = key;
                _voiceRecentAt = now;
                _voiceSaidAsked++;
            }
        }

        private bool VoiceMayAsk(int h, out string why)
        {
            why = null;
            if (h == 0 || h == Game.Player.Character.Handle || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
            {
                why = "nobody to say it";
                return false;
            }
            if (ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(Game.Player.Character.Position) > _cfg.VoiceMetres)
            {
                why = "farther than " + _cfg.VoiceMetres + " m";
                return false;
            }
            lock (_voiceLock)
                if (_voiceJobs.Count(j => !j.Over) >= VoiceMostJobs)
                {
                    _voiceSkipped++;
                    why = "the voice busy";
                    return false;
                }
            return true;
        }

        /// Our lines as they are meant to sound: the quotes and stage marks taken off; null for nothing sayable.
        private static string VoiceText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            string t = text.Trim().Trim('"', '“', '”').Replace("…", "...").Replace("*", "");
            if (t.Length < 2 || !t.Any(char.IsLetter))
                return null;
            return t.Length > 300 ? t.Substring(0, 300) : t;
        }

        private bool VoiceAsk(int h, string label, string text, string kind, int waitSeconds)
        {
            MindWho w;
            string person = _mindWho.TryGetValue(h, out w) && w.Id != null ? w.Id : null;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            string modelName = (Models.Name(model) ?? string.Empty).ToUpperInvariant();
            var j = new VoiceJob
            {
                Id = "v" + (++_voiceSeq) + "-" + (Environment.TickCount & 0xFFFFFF).ToString("x"),
                Kind = kind,
                Name = w != null && w.Name != null ? w.Name : label ?? "someone",
                Text = text,
                Model = modelName,
                Person = person ?? modelName + "#" + h,
                Male = PED.IS_PED_MALE(h),
                Handle = h,
                AskedAt = Environment.TickCount,
                WaitSeconds = waitSeconds,
            };
            float d = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(Game.Player.Character.Position);
            j.Vol = VoiceVolume(d, kind == "thought");
            lock (_voiceLock)
                _voiceJobs.Add(j);
            return true;
        }

        // ---- the worker ----

        private void VoiceStart()
        {
            if (_voiceThread != null)
                return;
            _voiceStop = false;
            _voiceThread = new Thread(VoiceWork) { IsBackground = true, Name = "RealNPCs voice", Priority = ThreadPriority.BelowNormal };
            _voiceThread.Start();
            Log.Write("VOICE worker started: " + _cfg.VoiceUrl + " (thoughts " + (_cfg.VoicedThoughts ? "on" : "off") + ", dialogue " + (_cfg.VoicedDialogue ? "on" : "off") + ", within " + _cfg.VoiceMetres + " m)");
        }

        private void VoiceStop()
        {
            Thread t = _voiceThread;
            if (t == null)
                return;
            _voiceStop = true;
            HttpWebRequest req = _voiceReq;
            if (req != null)
                try { req.Abort(); } catch { }
            try { t.Join(1500); } catch { }
            _voiceThread = null;
            _voiceReady = false;
            lock (_voiceLock)
                _voiceJobs.Clear();
        }

        private void VoiceWork()
        {
            int nextHealth = 0;
            string url = _cfg.VoiceUrl;
            while (!_voiceStop)
            {
                try
                {
                    int now = Environment.TickCount;
                    if (now - nextHealth >= 0)
                    {
                        VoiceHealth(url);
                        nextHealth = now + (_voiceReady ? 15000 : 8000);
                    }
                    VoiceJob[] jobs;
                    lock (_voiceLock)
                        jobs = _voiceJobs.Where(x => !x.Over).ToArray();
                    foreach (VoiceJob j in jobs)
                    {
                        if (_voiceStop)
                            break;
                        if (!_voiceReady)
                        {
                            j.State = "no server";
                            j.Over = true;
                            continue;
                        }
                        if (j.Kind == "capture")
                        {
                            VoiceRecordWork(url, j);
                            continue;
                        }
                        if (!j.Sent)
                        {
                            if (j.StopAsked || Environment.TickCount - j.AskedAt > j.WaitSeconds * 1000)
                            {
                                j.State = "dropped";
                                j.Over = true;
                                continue;
                            }
                            string body = "{\"id\":" + Language.JsonString(j.Id) + ",\"model\":" + Language.JsonString(j.Model) + ",\"sex\":" + Language.JsonString(j.Male ? "male" : "female") +
                                          ",\"person\":" + Language.JsonString(j.Person) + ",\"text\":" + Language.JsonString(j.Text) + ",\"kind\":" + Language.JsonString(j.Kind) +
                                          ",\"volume\":" + F(j.Vol) + ",\"pan\":" + F(j.Pan) + ",\"wait\":" + j.WaitSeconds + "}";
                            string r = VoiceHttp(url + "/say", body, 3000);
                            j.Sent = true;
                            j.Bank = Language.JsonField(r, "bank");
                            string held = Language.JsonField(r, "held");
                            if (held != null)
                            {
                                j.State = "held";
                                j.Why = held;
                                j.Over = true;
                            }
                            j.NextPoll = Environment.TickCount + 250;
                            continue;
                        }
                        if (j.StopAsked)
                        {
                            VoiceHttp(url + "/stop", "{\"id\":" + Language.JsonString(j.Id) + "}", 1500);
                            if (j.State != "playing")
                                j.State = "stopped";
                            j.Over = true;
                            continue;
                        }
                        if (Environment.TickCount - j.NextPoll < 0)
                            continue;
                        j.NextPoll = Environment.TickCount + 250;
                        if (j.State == "playing")
                            VoiceHttp(url + "/place", "{\"id\":" + Language.JsonString(j.Id) + ",\"volume\":" + F(j.Vol) + ",\"pan\":" + F(j.Pan) + "}", 1500);
                        string s = VoiceHttp(url + "/status?id=" + Uri.EscapeDataString(j.Id), null, 1500);
                        string state = Language.JsonField(s, "state") ?? "unknown";
                        int ms = VoiceNum(s, "make_ms");
                        if (ms > 0)
                            j.MakeMs = ms;
                        if (state == "playing" || state == "done")
                            j.Played = true;
                        if (state != "playing" || j.State != "playing")
                            j.State = state;
                        if (state == "done" || state == "dropped" || state == "failed" || state == "held" || state == "unknown")
                        {
                            j.Why = Language.JsonField(s, "why");
                            j.Over = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_voiceStop)
                        break;
                    bool was = _voiceReady;
                    _voiceReady = false;
                    _voiceServer = "not reachable: " + ex.Message;
                    if (was)
                        Log.Write("VOICE server stopped answering at " + url + " (" + ex.Message + "); RealNPCs' own voices until it does");
                    lock (_voiceLock)
                        foreach (VoiceJob j in _voiceJobs)
                            if (!j.Over)
                            {
                                j.State = "no server";
                                j.Over = true;
                            }
                }
                Thread.Sleep(100);
            }
        }

        private int _voiceChecks;

        private void VoiceHealth(string url)
        {
            _voiceChecks++;
            bool was = _voiceReady;
            string r;
            try
            {
                r = VoiceHttp(url + "/health", null, 1500);
            }
            catch (Exception ex)
            {
                _voiceReady = false;
                _voiceServer = "not reachable: " + ex.Message;
                if (_voiceChecks <= 1 || was)
                    Log.Write("VOICE server not reachable at " + url + " (" + ex.Message + "): start it with voice\\start-voice-server.cmd; nothing is voiced until it answers");
                return;
            }
            bool ready = r != null && r.IndexOf("\"ready\":true", StringComparison.Ordinal) >= 0;
            string model = Language.JsonField(r, "model") ?? "?";
            string free = VoiceRaw(r, "free_gb") ?? "?";
            _voiceReady = ready;
            _voiceServer = ready ? "ready (" + model + ", " + free + " GB free on the card)" : "loading its model (" + (Language.JsonField(r, "error") ?? "a few seconds") + ")";
            if (ready != was)
                Log.Write(ready ? "VOICE server ready at " + url + ": " + model + ", " + free + " GB free on the card" : "VOICE server at " + url + " isn't ready: " + _voiceServer);
        }

        private static string VoiceHttp(string url, string body, int timeoutMs)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            _voiceReq = req;
            req.Method = body == null ? "GET" : "POST";
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.Proxy = null;
            req.KeepAlive = true;
            if (body != null)
            {
                req.ContentType = "application/json";
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                req.ContentLength = bytes.Length;
                using (Stream s = req.GetRequestStream())
                    s.Write(bytes, 0, bytes.Length);
            }
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return reader.ReadToEnd();
        }

        private static string F(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// A bare number or word after "name": in the server's compact JSON, as text (null when absent).
        private static string VoiceRaw(string json, string name)
        {
            if (json == null)
                return null;
            string tag = "\"" + name + "\":";
            int i = json.IndexOf(tag, StringComparison.Ordinal);
            if (i < 0)
                return null;
            i += tag.Length;
            int e = i;
            while (e < json.Length && json[e] != ',' && json[e] != '}')
                e++;
            return json.Substring(i, e - i).Trim();
        }

        private static int VoiceNum(string json, string name)
        {
            double d;
            string raw = VoiceRaw(json, name);
            return raw != null && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? (int)d : 0;
        }

        // ---- the Testing row and F7 ----

        private static readonly string[] VoiceTestLines =
        {
            "Can't say I know you, mister, but you look like a man with somewhere to be.",
            "Fine morning for it. Wish I had the time to enjoy it.",
            "Don't stand there gawking. Say what you came to say.",
            "Heard there's work out past the river, if a man doesn't mind the mud.",
        };

        /// The Testing row: the nearest person within VoiceMetres says a line in their own voice now (proves the pipeline).
        private string VoiceTest(int now, out string said)
        {
            said = null;
            if (!VoiceWanted)
                return "[Mind] VoicedThoughts and VoicedDialogue are both off";
            if (!_voiceReady)
                return "the voice server isn't ready (" + _voiceServer + "): start voice\\start-voice-server.cmd";
            Vector3 me = Game.Player.Character.Position;
            int best = 0;
            float bestD = _cfg.VoiceMetres;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.Handle == Game.Player.Character.Handle || p.IsDead || !PED.IS_PED_HUMAN(p.Handle))
                    continue;
                float d = p.Position.DistanceTo(me);
                if (d < bestD)
                {
                    bestD = d;
                    best = p.Handle;
                }
            }
            if (best == 0)
                return "nobody within " + _cfg.VoiceMetres + " m";
            string line = VoiceTestLines[(_voiceSeq + now / 1000) % VoiceTestLines.Length];
            string why;
            if (!VoiceMayAsk(best, out why))
                return why;
            MindWho w;
            string name = _mindWho.TryGetValue(best, out w) && w.Name != null ? w.Name : "Someone";
            VoiceAsk(best, name, line, "said", 15);
            SpeechCaption(Person.FirstOf(name), line, best, Math.Min(7000, 1600 + line.Length * 55), false, false);
            said = name + " (" + (Models.Name(ENTITY.GET_ENTITY_MODEL(best)) ?? "?") + ", " + bestD.ToString("0") + " m) is saying it in their own voice";
            return null;
        }

        // ---- recording the game's own clips (the Testing row "voicerec") ----

        /// The Testing row: the nearest person within 8 m whose voice bank is certain says the game's own line their voice
        /// is cloned from, and the server records it off this PC's output (the clip the player's own game gives). Press by
        /// press: their own voice, then the bank by its full name, then by its short name (which name the game takes is
        /// what recording every bank on one body will need to know).
        private string VoiceRecord(int now, out string said)
        {
            said = null;
            if (!VoiceWanted)
                return "[Mind] VoicedThoughts and VoicedDialogue are both off";
            if (!_voiceReady)
                return "the voice server isn't ready (" + _voiceServer + "): start voice\\start-voice-server.cmd";
            lock (_voiceLock)
                if (_voiceJobs.Any(j => j.Kind == "capture" && !j.Over))
                    return "a recording is under way";
            Vector3 me = Game.Player.Character.Position;
            int best = 0;
            float bestD = VoiceRecordMetres;
            string bank = null;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.Handle == Game.Player.Character.Handle || p.IsDead || !PED.IS_PED_HUMAN(p.Handle))
                    continue;
                float d = p.Position.DistanceTo(me);
                if (d >= bestD)
                    continue;
                string b = Speech.BankOf(p.Handle);
                if (b == null || Speech.Busy(p.Handle))
                    continue;
                bestD = d;
                best = p.Handle;
                bank = b;
            }
            if (best == 0)
                return "nobody within " + VoiceRecordMetres + " m whose voice bank is certain and who isn't speaking";
            int style = _voiceRecStyle++ % 3;
            string voice = style == 0 ? null : style == 1 ? bank : VoiceShortBank(bank);
            MindWho w;
            string name = _mindWho.TryGetValue(best, out w) && w.Name != null ? w.Name : "Someone";
            var job = new VoiceJob
            {
                Id = "rec" + (++_voiceSeq) + "-" + (Environment.TickCount & 0xFFFFFF).ToString("x"),
                Kind = "capture",
                Name = name,
                Text = "",
                Bank = bank,
                Voice = voice,
                Handle = best,
                AskedAt = Environment.TickCount,
                WaitSeconds = 25,
            };
            lock (_voiceLock)
                _voiceJobs.Add(job);
            said = name + " (" + bank + ", " + bestD.ToString("0") + " m) says the clip's line in " + (voice == null ? "their own voice" : "the bank by name, " + voice) + "; the server records it";
            return null;
        }

        /// The bank's name without the number in front (0013_A_F_M_ASBTOWNFOLK_01_WHITE_01 gives A_F_M_ASBTOWNFOLK_01_WHITE_01).
        private static string VoiceShortBank(string bank)
        {
            int i = bank.IndexOf('_');
            return i > 0 && i <= 5 && bank.Substring(0, i).All(char.IsDigit) ? bank.Substring(i + 1) : bank;
        }

        /// The game thread's side of a recording: the moment the server records, the person says the line (their own voice,
        /// or a bank by name), and a second later the game is asked whether speech really started.
        private void VoiceRecordStep(VoiceJob j)
        {
            if (j.Line == null)
                return;   // the server hasn't answered yet
            int now = Environment.TickCount;
            if (j.PlayedAt == 0)
            {
                j.Accepted = Speech.PlayAs(j.Handle, j.Voice, j.Line, j.Take, 0);
                j.PlayedAt = now;
                if (!j.Accepted)
                    j.StopAsked = true;   // the game won't say it: the server mustn't keep whatever else it hears
                return;
            }
            if (!j.Checked && now - j.PlayedAt >= 1200)
            {
                j.Spoke = Speech.Busy(j.Handle);
                j.Checked = true;
                if (!j.Spoke)
                    j.StopAsked = true;   // accepted but silent (seen in game 2026-11-03: the tap then kept a passer-by's line)
            }
        }

        /// The worker's side of a recording: the server asked to record, then watched until it has judged the clip.
        private void VoiceRecordWork(string url, VoiceJob j)
        {
            if (!j.Sent)
            {
                string r = VoiceHttp(url + "/capture", "{\"id\":" + Language.JsonString(j.Id) + ",\"bank\":" + Language.JsonString(j.Bank) + "}", 3000);
                j.Sent = true;
                string held = Language.JsonField(r, "held");
                if (held != null)
                {
                    j.State = "held";
                    j.Why = held;
                    j.Over = true;
                    return;
                }
                j.Take = VoiceNum(r, "take");
                j.Line = Language.JsonField(r, "line");   // the game thread plays it the moment it sees this
                j.State = "recording";
                j.NextPoll = Environment.TickCount + 500;
                return;
            }
            if (Environment.TickCount - j.NextPoll < 0)
                return;
            j.NextPoll = Environment.TickCount + 500;
            if (j.StopAsked || Environment.TickCount - j.AskedAt > j.WaitSeconds * 1000)
            {
                VoiceHttp(url + "/stop", "{\"id\":" + Language.JsonString(j.Id) + "}", 1500);
                j.State = j.StopAsked ? "stopped" : "timed out";
                j.Over = true;
                return;
            }
            string s = VoiceHttp(url + "/status?id=" + Uri.EscapeDataString(j.Id), null, 1500);
            string state = Language.JsonField(s, "state") ?? "unknown";
            if (state == "captured" || state == "poor" || state == "failed" || state == "unknown" || state == "dropped")
            {
                j.State = state;
                j.Why = Language.JsonField(s, "why");
                string snr = VoiceRaw(s, "snr");
                j.Snr = snr == null || snr == "null" ? null : snr;
                j.Path = Language.JsonField(s, "path");
                j.Over = true;
            }
        }

        private string VoiceStatus()
        {
            if (!VoiceWanted)
                return "off ([Mind] VoicedThoughts and VoicedDialogue)";
            int med = 0;
            if (_voiceMakeMs.Count > 0)
            {
                var s = _voiceMakeMs.OrderBy(x => x).ToList();
                med = s[s.Count / 2];
            }
            return "thoughts " + (_cfg.VoicedThoughts ? "on" : "off") + ", dialogue " + (_cfg.VoicedDialogue ? "on" : "off") + " within " + _cfg.VoiceMetres + " m; server " + _voiceServer + " (" + _cfg.VoiceUrl + ")" +
                   "; asked " + _voiceThoughtsAsked + " thoughts and " + _voiceSaidAsked + " lines, " + _voiceSpoken + " spoken (median " + med + " ms to make), " + _voiceHeld + " held" +
                   (_voiceHeldWhy != null ? " (last: " + _voiceHeldWhy + ")" : "") + ", " + _voiceDropped + " dropped, " + _voiceFailed + " failed, " + _voiceSkipped + " passed over while busy" +
                   (_voiceRecorded + _voiceRecordPoor > 0 ? "; " + _voiceRecorded + " clips recorded in game (" + _voiceRecordPoor + " not kept)" : "") +
                   (_voiceLast != null ? "; last: " + _voiceLast : "");
        }
    }
}
