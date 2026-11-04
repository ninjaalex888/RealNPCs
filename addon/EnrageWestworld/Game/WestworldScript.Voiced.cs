// Voiced thoughts and dialogue (2026-11-03; Lex: "voiced thoughts and dialogue"; the plan agreed 2026-10-31). A local voice
// server (voice\voice_server.py, Chatterbox on this PC) says RealNPCs' own words in the person's own voice, cloned from one
// of the game's recordings for their model, out of the player's own export of the voice banks; nothing of Rockstar's
// ships. Two parts, each behind its switch, both off as they come:
//   [Mind] VoicedThoughts  a thought over someone within VoiceMetres is murmured in their voice a moment after it shows (one
//                          at a time, one person once in 90 s, 12 s between); that thought isn't also said aloud by rule.
//   [Mind] VoicedDialogue  what people say near you in our words (the street's scenes, Talk's answers, the encounters' lines)
//                          is said in their voice; the subtitle waits for the voice (a second or two to make) and shows as it
//                          starts, or after 3.5 s anyway (Lex, 2026-11-03: the voice came behind the words). The game's own
//                          recorded remark, where one plays, still plays.
// The server plays the sound itself, one voice at a time in the order asked; this side keeps telling it how loud and from
// which side, from the speaker's distance and the camera, while it speaks. The game thread never waits on it: a worker
// thread does the asking, as the language layer's does (an Insert reload aborts the request under way).
// The clips come from the player's own game ([Mind] VoiceRecord): someone whose bank has no clip yet says the game's own line
// their voice is cloned from, by the bank's name, in a quiet moment, and the server records it off this PC's output, so nothing
// of Rockstar's ships. While it records, our own game lines are held (Speech.RecordingPed) and another voice near spoils the
// clip. The Testing rows "voicerec" (the nearest person now) and "voicerec:all" (every bank still wanted, on whoever is nearest).
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
            public string Origin, How;       // their people (for the bank that fits them); how the server chose the voice (own, model, borrowed)
            public bool Auto, Batch;         // a recording the mod started by itself (not the Testing row); one of the batch
            public volatile string Tainted;  // another voice spoke near into the recording's window: the clip can't be kept
            public volatile string TaintFar; // a voice farther off spoke into it: kept as the weaker sort until a clean one comes
            public volatile string TaintSent;   // what the server was told of that (near, far)
            public volatile bool VoiceSet;
            // the subtitle that waits for this voice (SpeechCaption): shown as the voice starts, or when it isn't coming
            public string CapLabel, CapText;
            public int CapMs, CapAskedAt;
            public bool CapToYou;
        }

        private const int VoiceThoughtGapMs = 12000, VoiceThoughtEachMs = 90000, VoiceMostJobs = 4, VoiceCaptionWaitMs = 3500;
        private const float VoiceRecordMetres = 10f;
        // another voice this near the player while it records spoils the clip; farther off, out to the second, it's kept as the
        // weaker sort (a town's street always has someone talking within 20 m: seen in game 2026-11-03, 7 of 8 clips thrown out)
        private const float VoiceTaintNearMetres = 15f, VoiceTaintFarMetres = 25f;
        private const int VoiceRecordGapMs = 20000, VoiceRecordBatchGapMs = 1500, VoiceRecordRetryMs = 600000;
        private int _voiceRecorded, _voiceRecordPoor, _voiceRecNextAt, _voiceRecAuto, _voiceRecBatch;
        private readonly Dictionary<string, int> _voiceRecTriedAt = new Dictionary<string, int>();
        private readonly HashSet<int> _voiceBankSet = new HashSet<int>();
        private volatile string _voiceRecWantBank;   // the worker's note: the server wants this bank recorded, on this person
        private volatile int _voiceRecWantHandle;
        private volatile bool _voiceRecAll, _voiceRecFetch;   // the batch (Testing row "voicerec:all"): on, and its list still to fetch
        private volatile string _voiceRecQueueText;
        private readonly Queue<string> _voiceRecQueue = new Queue<string>();
        private int _voiceRecQueueNone, _voiceRecQueueWeak, _voiceRecQueueTotal;
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
            VoiceRecordStep();
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
                if (j.CapText != null && (j.Played || j.Over || Environment.TickCount - j.CapAskedAt > VoiceCaptionWaitMs))
                    VoiceCaptionShow(j);   // the voice started (or isn't coming, or took too long): its words now
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
                    VoiceCaptureStep(j);
                    continue;
                }
                if (j.How == "own" && !j.VoiceSet)
                    VoiceAlign(j);
                j.Vol = VoiceVolume(d, j.Kind == "thought");
                float dx = at.X - me.X, dy = at.Y - me.Y, len = (float)Math.Sqrt(dx * dx + dy * dy);
                j.Pan = len < 0.8f ? 0f : Math.Max(-0.85f, Math.Min(0.85f, (dx * rx + dy * ry) / len));
            }
        }

        /// The subtitle that waited for this voice, shown now; once.
        private void VoiceCaptionShow(VoiceJob j)
        {
            string text = j.CapText;
            if (text == null)
                return;
            j.CapText = null;
            CaptionNow(j.CapLabel, text, j.Handle, j.CapMs, j.CapToYou);
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
                if (j.Auto)
                {
                    if (j.Batch)
                        _voiceRecBatch++;
                    else
                        _voiceRecAuto++;
                }
                string how = (j.Voice == null ? "their own voice" : "the bank by name") + (j.Auto ? (j.Batch ? ", the batch" : ", by itself") : ", the row");
                (j.Batch && _voiceRecBatch > 20 ? (Action<string>)Log.Detail : Log.Write)("VOICE recorded: " + j.Name + " (" + j.Bank + ", " + how + "): " + (j.Line ?? "no line") + " take " + j.Take + ", " +
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
            if (VoiceAsk(h, line.Label, text, "thought", 8) != null)
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

        /// Our words said by someone near (a scene, Talk, an encounter): in their own voice, when VoicedDialogue is on. The
        /// voice asked for, or null when none is (the caller's subtitle then shows at once).
        private VoiceJob VoiceSaid(int handle, string label, string text, bool face)
        {
            if (_cfg == null || !_cfg.VoicedDialogue || !_voiceReady || handle == 0)
                return null;
            text = VoiceText(text);
            if (text == null)
                return null;
            int now = Environment.TickCount;
            string key = handle + "|" + text;
            if (key == _voiceRecentKey && now - _voiceRecentAt < 8000)
                return null;   // the same line passed on twice (a scene line both captioned and heard)
            string why;
            if (!VoiceMayAsk(handle, out why))
                return null;
            VoiceJob j = VoiceAsk(handle, label, text, "said", face ? 15 : 9);
            if (j != null)
            {
                _voiceRecentKey = key;
                _voiceRecentAt = now;
                _voiceSaidAsked++;
            }
            return j;
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

        private VoiceJob VoiceAsk(int h, string label, string text, string kind, int waitSeconds)
        {
            MindWho w;
            string person = _mindWho.TryGetValue(h, out w) && w.Id != null ? w.Id : null;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            string modelName = (Models.Name(model) ?? string.Empty).ToUpperInvariant();
            ResBind rb;
            string origin = _resBind.TryGetValue(h, out rb) && rb.R != null ? rb.R.Origin : null;   // their people, as their name was chosen
            var j = new VoiceJob
            {
                Id = "v" + (++_voiceSeq) + "-" + (Environment.TickCount & 0xFFFFFF).ToString("x"),
                Kind = kind,
                Name = w != null && w.Name != null ? w.Name : label ?? "someone",
                Text = text,
                Model = modelName,
                Person = person ?? modelName + "#" + h,
                Origin = origin,
                Male = PED.IS_PED_MALE(h),
                Handle = h,
                AskedAt = Environment.TickCount,
                WaitSeconds = waitSeconds,
            };
            float d = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(Game.Player.Character.Position);
            j.Vol = VoiceVolume(d, kind == "thought");
            lock (_voiceLock)
                _voiceJobs.Add(j);
            return j;
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
            Speech.RecordingPed = 0;
            lock (_voiceLock)
            {
                foreach (VoiceJob j in _voiceJobs)
                    VoiceCaptionShow(j);   // words waiting on a voice that won't come now
                _voiceJobs.Clear();
            }
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
                    if (_voiceRecFetch && _voiceReady)   // the batch's list: the banks the server still wants
                    {
                        _voiceRecFetch = false;
                        string n = VoiceHttp(url + "/needed?most=600", null, 3000);
                        _voiceRecQueueNone = VoiceNum(n, "none");
                        _voiceRecQueueWeak = VoiceNum(n, "weak");
                        _voiceRecQueueText = Language.JsonField(n, "banks") ?? "";
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
                                          ",\"person\":" + Language.JsonString(j.Person) + ",\"origin\":" + Language.JsonString(j.Origin ?? "") +
                                          ",\"text\":" + Language.JsonString(j.Text) + ",\"kind\":" + Language.JsonString(j.Kind) +
                                          ",\"volume\":" + F(j.Vol) + ",\"pan\":" + F(j.Pan) + ",\"wait\":" + j.WaitSeconds + "}";
                            string r = VoiceHttp(url + "/say", body, 3000);
                            j.Sent = true;
                            j.Bank = Language.JsonField(r, "bank");
                            j.How = Language.JsonField(r, "voice");
                            string need = Language.JsonField(r, "need");
                            if (need != null && _cfg.VoiceRecord)   // their own bank has no clip yet: the game thread may have them record it
                            {
                                _voiceRecWantHandle = j.Handle;
                                _voiceRecWantBank = need;
                            }
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
                        j.NextPoll = Environment.TickCount + (j.State == "making" || j.State == "ready" ? 120 : 250);   // about to start: looked at more often, so its subtitle isn't far behind
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
            string card = VoiceRaw(r, "card_gb");   // what the voices hold of the card, from the server that gives it back after a quiet spell (2026-11-03)
            bool onCard = r != null && r.IndexOf("\"on_card\":true", StringComparison.Ordinal) >= 0;
            string holds = card == null ? "" : onCard ? ", the voices hold " + card + " GB" : ", the voices off the card until the next line";
            _voiceReady = ready;
            _voiceServer = ready ? "ready (" + model + ", " + free + " GB free on the card" + holds + ")" : "loading its model (" + (Language.JsonField(r, "error") ?? "a few seconds") + ")";
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
            // the bank by its full name, as the recordings by themselves do (seen in game 2026-11-03: the full name with the number
            // in front plays, the short name is turned down, and their own voice is the same recording)
            VoiceJob job = VoiceRecordStart(best, bank, bank, false, VoiceRecordBatchGapMs);
            said = job.Name + " (" + bank + ", " + bestD.ToString("0") + " m) says the clip's line in the bank by name; the server records it";
            return null;
        }

        /// The Testing row "voicerec:all": every bank the server still wants, recorded on whoever is nearest, one after
        /// another (about ten seconds each); pressed again, it stops.
        private string VoiceRecordAll(int now, out string said)
        {
            said = null;
            if (_voiceRecAll)
            {
                _voiceRecAll = false;
                _voiceRecQueue.Clear();
                said = "the batch stopped: " + _voiceRecBatch + " recorded this session";
                return null;
            }
            if (!VoiceWanted)
                return "[Mind] VoicedThoughts and VoicedDialogue are both off";
            if (!_voiceReady)
                return "the voice server isn't ready (" + _voiceServer + "): start voice\\start-voice-server.cmd";
            _voiceRecQueueText = null;
            _voiceRecFetch = true;
            _voiceRecAll = true;
            said = "recording every voice still wanted on whoever is nearest (10 m), one after another; press again to stop";
            return null;
        }

        /// A recording started: this person says the bank's line (by name, or in their own voice when voice is null) while
        /// the server records; the next one not before the gap, and this bank not again for a while.
        private VoiceJob VoiceRecordStart(int h, string bank, string voice, bool auto, int gapMs, bool batch = false)
        {
            int now = Environment.TickCount;
            MindWho w;
            var job = new VoiceJob
            {
                Id = "rec" + (++_voiceSeq) + "-" + (now & 0xFFFFFF).ToString("x"),
                Kind = "capture",
                Name = _mindWho.TryGetValue(h, out w) && w.Name != null ? w.Name : "Someone",
                Text = "",
                Bank = bank,
                Voice = voice,
                Handle = h,
                Auto = auto,
                Batch = batch,
                AskedAt = now,
                WaitSeconds = 25,
            };
            lock (_voiceLock)
                _voiceJobs.Add(job);
            _voiceRecNextAt = now + gapMs;
            if (_voiceRecTriedAt.Count > 1000)
                _voiceRecTriedAt.Clear();
            _voiceRecTriedAt[bank] = now;
            return job;
        }

        /// Whether this person may be recorded now: near, in a quiet moment (nobody speaking, no rain, no fight, no
        /// mission), not just after another recording, and not a bank tried lately.
        private bool VoiceMayRecord(int h, string bank, out string why)
        {
            int now = Environment.TickCount;
            why = null;
            if (now < _voiceRecNextAt && now >= _voiceRecNextAt - 120000)
            {
                why = "the last recording was a moment ago";
                return false;
            }
            int tried;
            if (bank != null && _voiceRecTriedAt.TryGetValue(bank, out tried) && now - tried < VoiceRecordRetryMs && now >= tried)
            {
                why = "that bank was tried lately";
                return false;
            }
            if (_missionPaused)
            {
                why = "a mission";
                return false;
            }
            if (_engine != null && !_engine.NowIndoors && (_engine.NowWeather == "rain" || _engine.NowWeather == "storm"))
            {
                why = "the rain";
                return false;
            }
            int me = Game.Player.Character.Handle;
            if (PED.IS_PED_IN_COMBAT(me, 0))
            {
                why = "you're fighting";
                return false;
            }
            if (PED.IS_PED_ON_MOUNT(me) || PED.IS_PED_IN_ANY_VEHICLE(me, false) || ENTITY.GET_ENTITY_SPEED(me) > 2.5f)
            {
                why = "you're on the move";   // the clip is cut when they fall out of earshot (seen in game 2026-11-03: a man spoke and the recording stopped)
                return false;
            }
            if (h == 0 || h == me || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
            {
                why = "nobody to record";
                return false;
            }
            if (PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || PED.IS_PED_RAGDOLL(h))
            {
                why = "they're busy";
                return false;
            }
            if (ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(Game.Player.Character.Position) > VoiceRecordMetres)
            {
                why = "farther than " + VoiceRecordMetres + " m";
                return false;
            }
            float at;
            if (Speech.Busy(h) || Speech.Busy(me) || VoiceAnyoneSpeaking(h, VoiceTaintFarMetres, out at))
            {
                why = "someone is speaking";
                return false;
            }
            return true;
        }

        /// Anyone but this person speaking within so many metres of you (the game's own remarks would land in the recording);
        /// how far off the nearest of them is.
        private bool VoiceAnyoneSpeaking(int except, float within, out float nearest)
        {
            nearest = float.MaxValue;
            Vector3 me = Game.Player.Character.Position;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.Handle == except || p.Handle == Game.Player.Character.Handle)
                    continue;
                float d = p.Position.DistanceTo(me);
                if (d <= within && d < nearest && Speech.Busy(p.Handle))
                    nearest = d;
            }
            return nearest <= within;
        }

        /// Each step: the recording under way watched for another voice (near, the clip is spoilt; farther off, it's kept as the
        /// weaker sort) and our own game lines held meanwhile; else the recording the server asked for (someone's own bank with
        /// no clip yet), then the batch.
        private void VoiceRecordStep()
        {
            if (!_voiceReady)
                return;
            VoiceJob under;
            lock (_voiceLock)
                under = _voiceJobs.FirstOrDefault(j => j.Kind == "capture" && !j.Over);
            Speech.RecordingPed = under != null ? under.Handle : 0;
            if (under != null)
            {
                float at;
                if (under.PlayedAt != 0 && under.Tainted == null && VoiceAnyoneSpeaking(under.Handle, VoiceTaintFarMetres, out at))
                {
                    if (at <= VoiceTaintNearMetres)
                        under.Tainted = "a voice " + at.ToString("0") + " m off";
                    else if (under.TaintFar == null)
                        under.TaintFar = "a voice " + at.ToString("0") + " m off";
                }
                return;
            }
            string bank = _voiceRecWantBank;
            if (bank != null)
            {
                _voiceRecWantBank = null;
                int h = _voiceRecWantHandle;
                string why;
                if (_cfg.VoiceRecord && VoiceMayRecord(h, bank, out why))
                    VoiceRecordStart(h, bank, bank, true, VoiceRecordGapMs);
            }
            if (_voiceRecAll)
                VoiceRecordBatchStep();
        }

        /// The batch: the banks the server still wants, one after another, said by whoever is nearest and may be recorded.
        private void VoiceRecordBatchStep()
        {
            if (_voiceRecQueue.Count == 0)
            {
                string text = _voiceRecQueueText;
                if (text == null)
                    return;   // the list is still on its way
                _voiceRecQueueText = null;
                foreach (string b in text.Split(','))
                    if (b.Length > 0)
                        _voiceRecQueue.Enqueue(b);
                _voiceRecQueueTotal = _voiceRecQueue.Count;
                if (_voiceRecQueue.Count == 0)
                {
                    _voiceRecAll = false;
                    Log.Write("VOICE batch: nothing left to record");
                    return;
                }
                Log.Write("VOICE batch: " + _voiceRecQueue.Count + " banks to record (" + _voiceRecQueueNone + " with no clip, " + _voiceRecQueueWeak + " weak), about " + Math.Max(1, _voiceRecQueue.Count * 10 / 60) + " minutes");
            }
            Vector3 me = Game.Player.Character.Position;
            int best = 0;
            float bestD = VoiceRecordMetres;
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
            string why;
            if (best == 0 || !VoiceMayRecord(best, null, out why))
                return;
            string bank = _voiceRecQueue.Dequeue();
            VoiceRecordStart(best, bank, bank, true, VoiceRecordBatchGapMs, true);   // the bank by name on this body (in their own voice the clip would be theirs, saved under the bank's name)
        }

        /// The game's own remarks from this person in the bank their clone is (a model with several banks: the game picks
        /// one at random for each body, the server one by who they are, for good); told once per body.
        private void VoiceAlign(VoiceJob j)
        {
            j.VoiceSet = true;
            if (j.Bank == null || _voiceBankSet.Contains(j.Handle) || Speech.BankOf(j.Handle) != null)
                return;
            if (_voiceBankSet.Count > 400)
                _voiceBankSet.Clear();
            _voiceBankSet.Add(j.Handle);
            AUDIO.SET_AMBIENT_VOICE_NAME(j.Handle, j.Bank);
            Log.Detail("VOICE: " + j.Name + " speaks the game's lines in " + j.Bank + " from here (their clone's bank)");
        }

        /// The game thread's side of a recording: the moment the server records, the person says the line (their own voice,
        /// or a bank by name), and a second later the game is asked whether speech really started.
        private void VoiceCaptureStep(VoiceJob j)
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
            if (Environment.TickCount - j.NextPoll < 0)
                return;
            if (!j.Sent)
            {
                string r = VoiceHttp(url + "/capture", "{\"id\":" + Language.JsonString(j.Id) + ",\"bank\":" + Language.JsonString(j.Bank) + "}", 3000);
                j.Sent = true;
                string held = Language.JsonField(r, "held");
                if (held != null)
                {
                    if (held.StartsWith("a voice is playing", StringComparison.Ordinal) && Environment.TickCount - j.AskedAt < j.WaitSeconds * 1000)
                    {
                        j.Sent = false;   // asked again in a moment, while its wait lasts
                        j.NextPoll = Environment.TickCount + 2000;
                        return;
                    }
                    j.State = "held";
                    j.Why = held;
                    j.Over = true;
                    return;
                }
                j.Take = VoiceNum(r, "take");
                j.Line = Language.JsonField(r, "line");   // the game thread plays it the moment it sees this
                j.State = "recording";
                j.NextPoll = Environment.TickCount + 300;
                return;
            }
            j.NextPoll = Environment.TickCount + 300;
            string taint = j.Tainted != null ? "near" : j.TaintFar != null ? "far" : null;
            if (taint != null && taint != j.TaintSent)
            {
                j.TaintSent = taint;
                VoiceHttp(url + "/taint", "{\"id\":" + Language.JsonString(j.Id) + ",\"why\":" + Language.JsonString(j.Tainted ?? j.TaintFar) + ",\"far\":" + (taint == "far" ? "true" : "false") + "}", 1500);
            }
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

        /// The settings panel's word on the voice server, under the AI model's (Lex, 2026-11-03): "off", "on, ready (...)",
        /// "on, checking...", "on, loading its model (...)", or "on, NOT REACHABLE (...)".
        private string VoiceStatusText()
        {
            if (!VoiceWanted)
                return "off ([Mind] VoicedThoughts and VoicedDialogue, on the Mind page)";
            string s = _voiceServer ?? "not checked";
            if (_voiceReady)
                return "on, " + s;
            if (s == "not checked")
                return "on, checking...";
            if (s.StartsWith("not reachable", StringComparison.Ordinal))
                return "on, NOT REACHABLE (start voice\\start-voice-server.cmd and leave its window open)";
            return "on, " + s;
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
                   (_voiceRecorded + _voiceRecordPoor > 0 ? "; " + _voiceRecorded + " clips recorded in game (" + _voiceRecordPoor + " not kept; " + _voiceRecAuto + " by itself, " + _voiceRecBatch + " in the batch)" : "") +
                   (_voiceRecAll ? "; the batch running, " + _voiceRecQueue.Count + " of " + _voiceRecQueueTotal + " to go" : "") +
                   (_voiceLast != null ? "; last: " + _voiceLast : "");
        }
    }
}
