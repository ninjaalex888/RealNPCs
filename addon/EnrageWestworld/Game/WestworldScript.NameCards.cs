// Names over heads (Lex, 2026-10-29, with enRAGE gone: "it did a really good job of placing NPC names over the NPC head
// that looked really good and our mod wasn't doing this"). Adapted from enRAGE's DrawNameCards (its author permits reuse;
// credited here): the candidates re-read every 250 ms, the head bone cached and looked up for six bodies a frame at most,
// the screen point steadied by a one-euro filter and led by its velocity, an occlusion ray for three bodies a frame every
// 150 ms, a fade by distance to the edge of the range and in and out, the name in the game's title font over a second
// line in smaller type, both centred by the font's own width table; no box. The names are ours: one of enRAGE's records
// (coloured by his hate), one of its townsfolk, or a resident bound to the body (a regular, someone who thought, spoke or
// took part in a scene); in "everyone" mode anyone else gets the kind of person they look ("A townsman") in grey, since
// nobody is given an identity just to draw a card (names over every passer-by wait for identity by look, roadmap 34).
// With enRAGE running and its own cards on, ours stay off unless [Display] NameCardsBesideEnrage says otherwise.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private sealed class NameCard
        {
            public int Handle;
            public uint Model;
            public string Name, Line2;
            public int R, G, B, R2, G2, B2;
            public bool Known;
            public bool Ready;   // the body exists and is ready to draw, asked one frame in four (NameCardPlan.BodyCheckDue)
            public int MadeAt, FirstSeen, SeenFrame;
            public float Metres, Range;
            public Vector3 HeadOff;
            public int HeadFrame = -1000;
            public float HeadZ;
            public bool HasHeadZ;
            public float PrevX, PrevY, VelX, VelY;
            public int PrevFrame = -1000;
            public bool Blocked;
            public int BlockedAt;
            public float Alpha;
            // the text as drawn and its width in the title font's units, kept until the text changes (night 2026-10-29,
            // NIGHT.md checklist 4-5: built anew for every card every frame, the costliest feature in a Saint Denis crowd)
            public string FormedOf, Formed, Formed2Of, Formed2;
            public float Units, Units2;
        }

        private readonly Dictionary<int, NameCard> _nameCards = new Dictionary<int, NameCard>();
        private readonly List<NameCard> _nameCardsNow = new List<NameCard>();
        private readonly List<NameCard> _nameFound = new List<NameCard>();   // the candidates' pass, reused
        private static readonly Comparison<NameCard> NearerFirst = (a, b) => a.Metres.CompareTo(b.Metres);
        private int _nameCardsCandAt = -100000, _nameCardsFrame, _nameCardsHoldUntil = -100000, _nameCardsLastMs, _nameCardsUp, _nameCardsMade;
        private int? _enrageCards;
        private int _enrageCardsAt = -100000;
        private Action _aNameCards;
        private int _nameCardsProbedIn = -1, _nameCardsProbeAt = -100000;
        private PerfSlot _psCardsWho, _psCardsPlace, _psCardsWalls, _psCardsDraw;   // the cards' parts on the frame meter (FOURTEENHOUR item 2)
        private long _nameCardsDormant;   // card-frames skipped behind a wall and faded out (F7: the cut's share)
        private const int NameCardsMost = 10, NameCardsMostAlways = 16, NameCardsRefreshMs = 3000;
        private const int NameCardFloor = 8;   // the nearest this many always get their name, even from the overlays' share of the frame's text (Lex, 2026-10-31)
        private int _nameHeldLogAt = -100000, _nameHeldFrames;
        private const float NameCardAbove = 0.32f, NameCardSteady = 2.5f, NameCardLead = 1f;
        private static readonly int[] ColourTitle = { 245, 244, 240 }, ColourStranger = { 170, 168, 160 }, ColourDim = { 146, 144, 138 };

        private void NameCardsStep(int now)
        {
            if (_cfg == null || _cfg.NameCards == "off")
            {
                if (_nameCards.Count > 0)
                    _nameCards.Clear();
                return;
            }
            if (_aNameCards == null)
                _aNameCards = () => NameCardsFrame(_now);
            Safe("name cards", _aNameCards);
        }

        /// enRAGE is running and drawing its own cards (its [Menu] NameCards 1 or 2), and we weren't asked to draw beside them.
        private bool NameCardsLeftToEnrage(int now)
        {
            if (_bridge == null || !_bridge.Present || _cfg.NameCardsBesideEnrage)
                return false;
            if (now - _enrageCardsAt > 5000)
            {
                _enrageCardsAt = now;
                int? was = _enrageCards;
                _enrageCards = _bridge.EnrageNameCards();
                bool on = _enrageCards.HasValue && _enrageCards.Value > 0, wasOn = was.HasValue && was.Value > 0;
                if (on != wasOn)
                    Log.Write("NAME cards: enRAGE's own cards are " + (on ? "on (its NameCards " + _enrageCards.Value + "): " + (_cfg.NameCardsAlways ? "ours card everyone it doesn't" : "ours stand aside") : "off: ours card everyone"));
            }
            _enrageCardsOn = _enrageCards.HasValue && _enrageCards.Value > 0;
            // always on, ours never stand aside: enRAGE's cards are only over its own people, and a city of strangers had
            // none at all (Lex, 2026-10-30, Saint Denis); ours leave those to it (NameCardFillInner)
            return _enrageCardsOn && !_cfg.NameCardsAlways;
        }

        private bool _enrageCardsOn;

        private void NameCardsFrame(int now)
        {
            if (_engine == null || _bridge == null || PanelOpen || _reader.Open || now < _nameCardsHoldUntil || NameCardsLeftToEnrage(now))
            {
                if (_nameCards.Count > 0)
                    _nameCards.Clear();
                _nameCardsNow.Clear();
                _nameCardsUp = 0;
                return;
            }
            Ped me = Game.Player.Character;
            Vector3 mePos = me.Position;
            if (now - _nameCardsCandAt >= 250)
            {
                _nameCardsCandAt = now;
                PerfSub cs = SubStart();
                NameCardsCandidates(me, mePos, now);
                SubEnd(ref _psCardsWho, "name cards: who's near", cs);
            }
            // the frame's parts on the meter (FOURTEENHOUR item 2): placing (the body, the head, the screen point), the walls (the
            // ray and the room) and the drawing, each gathered through the loop and added once; "name cards" is what's left
            bool timed = _cfg.PerfMeter;
            long tPlace = 0, tWalls = 0, tDraw = 0, tAt = 0;
            _nameCardsFrame++;
            float dt = Math.Max(0.004f, Math.Min(0.1f, (now - _nameCardsLastMs) / 1000f));
            _nameCardsLastMs = now;
            int bones = 0, rays = 0, up = 0;
            int myInterior = _cfg.NameCardsAlways ? 0 : INTERIOR.GET_INTERIOR_FROM_ENTITY(me.Handle);   // always on, walls alone decide and this isn't read
            // always on, the cards are drawn before the Testing overlays: they leave those their room (Lex, 2026-10-30: a wake
            // stage tag went the moment her Fondness bar came, the frame's text all spent on names)
            int keep = !_cfg.NameCardsAlways ? 0 : (_cfg.ShowWakeStages && _cfg.WestWorld ? WakeTagMax : 0) + (_cfg.ShowMinds ? 12 : 0) + 2;
            bool held = false;   // cards that wanted drawing and had no text left (logged once a minute: what spent it)
            foreach (NameCard c in _nameCardsNow)
            {
                int h = c.Handle;
                if (NameCardPlan.BodyCheckDue(_nameCardsFrame, h))
                    c.Ready = ENTITY.DOES_ENTITY_EXIST(h) && PED.IS_PED_READY_TO_RENDER(h);
                if (!c.Ready)
                    continue;
                if (now - c.FirstSeen < 1000)
                    continue;   // a body just come into range settles first
                // behind a wall and faded out, its next look at the walls not due: nothing to place or draw this frame, so none of
                // the natives below (its body, its head, its screen point, its room) are asked for; the ray, when due, decides as
                // before whether it comes back (FOURTEENHOUR item 2: in a town, many of those near are indoors)
                if (c.Blocked && c.Alpha <= 0f && c.BlockedAt != 0 && now - c.BlockedAt <= NameCardPlan.RayEveryMs(c.Metres))
                {
                    c.SeenFrame = -1000;
                    _nameCardsDormant++;
                    continue;
                }
                if (timed)
                    tAt = System.Diagnostics.Stopwatch.GetTimestamp();
                Vector3 pos = ENTITY.GET_ENTITY_COORDS(h, true, true);
                if (pos.X == 0f && pos.Y == 0f)
                {
                    c.Ready = false;   // gone since the last check
                    continue;
                }
                bool hasHead = c.HeadFrame > -1000;
                Vector3 head;
                if ((!hasHead || _nameCardsFrame - c.HeadFrame >= NameCardPlan.BoneEveryFrames(c.Metres)) && bones < 6)
                {
                    bones++;
                    head = PED.GET_PED_BONE_COORDS(h, 21030, 0f, 0f, 0f);
                    if (head.X != 0f || head.Y != 0f)
                    {
                        c.HeadOff = head - pos;
                        c.HeadFrame = _nameCardsFrame;
                    }
                }
                else
                {
                    if (!hasHead)
                        continue;
                    head = pos + c.HeadOff;
                }
                bool noHead = head.X == 0f && head.Y == 0f;
                float z = noHead ? pos.Z + 0.75f : head.Z;
                if (c.HasHeadZ && Math.Abs(c.HeadZ - z) < 1.5f)
                    z = c.HeadZ + (z - c.HeadZ) * Math.Min(1f, dt * 6f);
                c.HeadZ = z;
                c.HasHeadZ = true;
                float wx = noHead ? pos.X : pos.X + (head.X - pos.X) * 0.6f;
                float wy = noHead ? pos.Y : pos.Y + (head.Y - pos.Y) * 0.6f;
                float x, y;
                bool onScreen = ScreenAt(new Vector3(wx, wy, z + NameCardAbove), out x, out y);
                if (timed)
                    tPlace += System.Diagnostics.Stopwatch.GetTimestamp() - tAt;
                if (!onScreen)
                {
                    c.SeenFrame = -1000;
                    continue;
                }
                // the one-euro filter: still cards are smoothed hard, moving ones follow, and a little lead hides the lag
                if (_nameCardsFrame - c.PrevFrame == 1)
                {
                    float vx = (x - c.PrevX) / dt, vy = (y - c.PrevY) / dt;
                    if (Math.Abs(x - c.PrevX) > 0.08f || Math.Abs(y - c.PrevY) > 0.08f)
                    {
                        vx = vy = 0f;
                        c.VelX = c.VelY = 0f;
                        c.PrevX = x;
                        c.PrevY = y;
                    }
                    float a1 = OneEuroAlpha(1f, dt);
                    c.VelX += (vx - c.VelX) * a1;
                    c.VelY += (vy - c.VelY) * a1;
                    float speed = (float)Math.Sqrt(c.VelX * c.VelX + c.VelY * c.VelY);
                    float a2 = OneEuroAlpha(NameCardSteady + 18f * speed, dt);
                    float sx = c.PrevX + (x - c.PrevX) * a2, sy = c.PrevY + (y - c.PrevY) * a2;
                    c.PrevX = sx;
                    c.PrevY = sy;
                    x = sx + c.VelX * dt * NameCardLead;
                    y = sy + c.VelY * dt * NameCardLead;
                }
                else
                {
                    c.PrevX = x;
                    c.PrevY = y;
                    c.VelX = c.VelY = 0f;
                }
                c.PrevFrame = _nameCardsFrame;
                if (timed)
                    tAt = System.Diagnostics.Stopwatch.GetTimestamp();
                if ((c.BlockedAt == 0 || now - c.BlockedAt > NameCardPlan.RayEveryMs(c.Metres)) && rays < NameCardPlan.RaysAFrame)
                {
                    rays++;
                    // always on: only a wall hides it (the map's own shapes), not a table, a post or someone in the way (Lex, 2026-10-30)
                    c.Blocked = Natives.Blocked(CAM.GET_FINAL_RENDERED_CAM_COORD(), new Vector3(pos.X, pos.Y, z + 0.05f), me.Handle, _cfg.NameCardsAlways ? 1 : 1 | 16);
                    c.BlockedAt = now;
                }
                bool hidden = c.BlockedAt == 0 || c.Blocked;
                if (!hidden && !_cfg.NameCardsAlways)   // always on, the walls' ray alone decides: a big building is several interiors (Lex, 2026-10-30: none in the Bastille)
                {
                    int interior = INTERIOR.GET_INTERIOR_FROM_ENTITY(h);
                    if (interior != 0 && interior != myInterior)
                        hidden = true;
                }
                if (timed)
                    tWalls += System.Diagnostics.Stopwatch.GetTimestamp() - tAt;
                float edge =Math.Max(0f, Math.Min(1f, (c.Range - c.Metres) / (_cfg.NameCardsAlways ? 4f : 12f)));   // always on: full until the last few metres
                float alpha = _nameCardsFrame - c.SeenFrame <= 3 ? c.Alpha : 0f;
                alpha = hidden ? Math.Max(0f, alpha - dt * 6f) : Math.Min(edge, alpha + dt * 4f);
                c.Alpha = alpha;
                c.SeenFrame = _nameCardsFrame;
                int a = (int)(255f * alpha);
                if (a < 8)
                    continue;
                bool tight = keep > 0 && Draw.Left - 2 < keep;
                if ((tight && up >= NameCardFloor) || Draw.Left <= 0)
                {
                    held = true;   // the rest of the frame's text is the overlays' (the nearest NameCardFloor get theirs all the same: names are kept, the overlays are test aids)
                    break;
                }
                // smaller with distance: the title 0.25 down to 0.19, the line under it 0.175 down to 0.145
                float far = Math.Max(0f, Math.Min(1f, (c.Metres - 6f) / Math.Max(1f, c.Range - 6f)));
                float s1 = 0.25f - 0.06f * far, s2 = 0.175f - 0.03f * far;
                float h1 = 0.1f * s1, h2 = 0.094f * s2;
                if (timed)
                    tAt = System.Diagnostics.Stopwatch.GetTimestamp();
                if (!ReferenceEquals(c.FormedOf, c.Name))
                {
                    c.FormedOf = c.Name;
                    c.Formed = Draw.Formed(c.Name, "$title");
                    c.Units = TitleUnits(c.Name);
                }
                Draw.TextFormed(c.Formed, x - c.Units * s1 * 0.0205f / 2f, y - h1 - h2 + 0.002f, s1, c.R, c.G, c.B, a);
                if (_cfg.NameCardsSecondLine && NameCardPlan.SecondLine(up, c.Metres) && !string.IsNullOrEmpty(c.Line2) && !tight && Draw.Left > 0)   // off: the name alone (players, 2026-10-30); short of text, the name alone too; the nearest few only (FIVEDAY2 0g)
                {
                    if (!ReferenceEquals(c.Formed2Of, c.Line2))
                    {
                        c.Formed2Of = c.Line2;
                        c.Formed2 = Draw.Formed(c.Line2, "$title");
                        c.Units2 = TitleUnits(c.Line2);
                    }
                    Draw.TextFormed(c.Formed2, x - c.Units2 * s2 * 0.0205f / 2f, y - h2 - 0.0018518518f, s2, c.R2, c.G2, c.B2, (int)(a * 0.88f));
                }
                if (timed)
                    tDraw += System.Diagnostics.Stopwatch.GetTimestamp() - tAt;
                up++;
                if (_nameCardsMade++ == 0)
                    Log.Write("NAME cards: the first, over " + c.Name + (c.Line2 != null && c.Line2.Length > 0 ? " (" + c.Line2 + ")" : "") + ", " + (int)c.Metres + " m off");
            }
            if (timed)
            {
                SubTicks(ref _psCardsPlace, "name cards: placing", tPlace);
                SubTicks(ref _psCardsWalls, "name cards: walls", tWalls);
                SubTicks(ref _psCardsDraw, "name cards: drawing", tDraw);
            }
            _nameCardsUp = up;
            if (held && (now - _nameHeldLogAt > 60000 || now < _nameHeldLogAt))
            {
                _nameHeldLogAt = now;
                Log.Write("NAME cards held back: " + up + " drawn of " + _nameCardsNow.Count + " near; the frame's text: " + Draw.Spent());
            }
            if (held)
                _nameHeldFrames++;
            if (_nameCardsFrame % 120 == 0)
                foreach (int gone in _nameCards.Where(kv => _nameCardsFrame - kv.Value.SeenFrame > 60 && now - kv.Value.FirstSeen > 5000 && !_nameCardsNow.Contains(kv.Value)).Select(kv => kv.Key).ToList())
                    _nameCards.Remove(gone);
        }

        /// Whom you're locked on to or aiming at, or 0.
        private static unsafe int NameCardTarget()
        {
            int t = 0;
            PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &t, false, false);
            if (t == 0)
                PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(Game.Player.Handle, &t);
            return t;
        }

        /// The living humans in range with a name of ours to show, nearest first, at most NameCardsMost; each keeps its card
        /// (and its smoothing) between looks, its words worked out again every three seconds, or when the game reuses the handle, a
        /// few a pass (NameCardPlan.MayFill): all of a crowd's came due together, a spike every three seconds.
        private void NameCardsCandidates(Ped me, Vector3 mePos, int now)
        {
            int fills = 0;
            _nameCardsNow.Clear();
            bool targeted = _cfg.NameCards == "targeted";   // only the one you're locked on to or aiming at (a player on Nexus, 2026-11-04: names "only when targeted")
            int targetNow = targeted ? NameCardTarget() : 0;
            bool everyone = _cfg.NameCards == "everyone" || targeted;   // whoever it is: the one you asked about gets a card, known or not
            int inside = INTERIOR.GET_INTERIOR_FROM_ENTITY(me.Handle);
            bool probe = inside != _nameCardsProbedIn && now - _nameCardsProbeAt > 3000;
            int seen = 0, carded = 0;
            var why = probe ? new Dictionary<string, int>() : null;
            var models = probe ? new List<string>() : null;
            float knownRange = _cfg.NameCardsKnownMetres, strangerRange = _cfg.NameCardsMetres, most = Math.Max(knownRange, strangerRange) + 20f;
            List<NameCard> found = _nameFound;   // reused: four passes a second, no list each time (THREEHOUR item 1)
            found.Clear();
            foreach (Ped p in Peds.All())
            {
                if (p == null || p.Handle == me.Handle)
                    continue;
                if (targeted && p.Handle != targetNow)
                    continue;
                // the distance first, before any other native: in a city most of the pool is far, and a body that's gone reads
                // as 0,0,0, far too (THREEHOUR item 1: Exists, IsDead and IS_PED_HUMAN were asked of everyone, every pass)
                float d = p.Position.DistanceTo(mePos);
                if (d > most || !p.Exists() || p.IsDead || !PED.IS_PED_HUMAN(p.Handle))
                    continue;
                int h = p.Handle;
                uint model = ENTITY.GET_ENTITY_MODEL(h);
                NameCard c;
                if (!_nameCards.TryGetValue(h, out c) || c.Model != model)
                {
                    c = new NameCard { Handle = h, Model = model, FirstSeen = now, MadeAt = -100000 };
                    _nameCards[h] = c;
                }
                if (probe && d <= strangerRange)
                    seen++;
                if (probe || NameCardPlan.MayFill(now - c.MadeAt >= NameCardsRefreshMs, c.Name == null && c.MadeAt < 0, fills))
                {
                    fills++;
                    c.MadeAt = now;
                    if (!NameCardFill(p, c, everyone))
                    {
                        c.Name = null;
                        if (probe && d <= strangerRange)
                        {
                            string r = Natives.StoryModel(p) ? "the game's own" : !IsGrownUp(p) ? "not grown" : !everyone ? "not known (NameCards known)" : "nothing to say";
                            int n0;
                            why.TryGetValue(r, out n0);
                            why[r] = n0 + 1;
                            if (models.Count < 6)
                                models.Add(Natives.ModelName(p));
                        }
                        continue;
                    }
                }
                if (c.Name == null)
                    continue;
                c.Range = c.Known ? knownRange : strangerRange;
                c.Metres = d;
                if (d <= c.Range)
                    found.Add(c);
            }
            found.Sort(NearerFirst);   // in place, one comparer for good (was OrderBy and Take: an enumerator and a buffer a pass)
            int most2 = Math.Min(found.Count, _cfg.NameCardsAlways ? NameCardsMostAlways : NameCardsMost);
            for (int i = 0; i < most2; i++)
                _nameCardsNow.Add(found[i]);
            if (probe)
            {
                carded = _nameCardsNow.Count;
                _nameCardsProbedIn = inside;
                _nameCardsProbeAt = now;
                Log.Write("NAME cards " + (inside != 0 ? "indoors (" + (_placeKind ?? "a building") + (_placeTown != null ? ", " + _placeTown : "") + ")" : "out of doors") + ": " + seen + " people within " + (int)strangerRange + " m, " + carded + " carded" +
                          (why.Count > 0 ? "; none for " + string.Join(", ", why.Select(kv => kv.Value + " " + kv.Key)) + " (" + string.Join(", ", models) + ")" : ""));
            }
        }

        /// What the card says for this body: false when there's nothing of ours to say (and the mode doesn't show everyone).
        /// The card, with the gang a member belongs to before the rest of the second line ("Lemoyne Raider - Hothead"), and
        /// an unnamed gang member carded by his gang ("A Lemoyne Raider"), as enRAGE showed them (Lex, 2026-10-29).
        // who owes you, for the card's "Owes you" (DEEPEN item 7; [Display] NameCardsOwes): one pass over the log every 5 s, so
        // a card asks a set, never the log (the cards refill every 250 ms). A debt you can't see is a debt you can't play with:
        // the man you stood with, or saved, says so under his name.
        private readonly HashSet<string> _owesYou = new HashSet<string>();
        private int _owesYouAt = -100000;

        private readonly HashSet<string> _stoodByYou = new HashSet<string>();
        private readonly HashSet<string> _youOwe = new HashSet<string>();   // the men you welshed on and haven't paid (SEAMS item 25)   // who stood by you in a fight this week (SEAMS item 5)

        /// The card's word for what's between you: "Owes you" (you saved him, stood with him, paid his fine), "Stood by you"
        /// (he fought for you this week; [Display] NameCardsStood), or null. Both sets refilled every 5 s, never per card.
        private string OwesYou(string id)
        {
            if (_cfg == null || (!_cfg.NameCardsOwes && !_cfg.NameCardsStood && !_cfg.NameCardsYouOwe) || _store == null)
                return null;
            int now = Game.GameTime;
            if (now - _owesYouAt >= 5000 || now < _owesYouAt)
            {
                _owesYouAt = now;
                if (_cfg.NameCardsOwes)
                    Interact.OwedBy(_store.WorldLog, _store.LastGameMin, _owesYou);
                else
                    _owesYou.Clear();
                if (_cfg.NameCardsStood)
                    Interact.StoodByYou(_store.WorldLog, _store.LastGameMin, _stoodByYou);
                else
                    _stoodByYou.Clear();
                if (_cfg.NameCardsYouOwe)
                    BrawlVerbs.OwedByYou(_store.WorldLog, _store.LastGameMin, _youOwe);
                else
                    _youOwe.Clear();
            }
            if (_owesYou.Count == 0 && _stoodByYou.Count == 0 && _youOwe.Count == 0 || id == null)
                return null;
            if (_youOwe.Count > 0 && (_youOwe.Contains(id) || (_engine != null && _youOwe.Contains(_engine.WorryIdOf(id)))))
                return "You owe them " + BrawlVerbs.Money(BrawlVerbs.StakeCents);   // a debt of yours first: the welsh unpaid (SEAMS item 25)
            string wid = _engine != null ? _engine.WorryIdOf(id) : id;   // one enRAGE took up since: the debt is under his first id (the round's review)
            return _owesYou.Contains(id) || _owesYou.Contains(wid) ? "Owes you" : _stoodByYou.Contains(id) || _stoodByYou.Contains(wid) ? "Stood by you" : null;
        }

        // who mourns one you killed this week (SEAMS item 12; [Display] NameCardsMourns): the mind's own grief (Engine.MournsYourKill,
        // the friend's warnings' and the mourning's rule) on the card, cached per person for 5 s so a card never walks a mind each
        // refill. The grief was there, read by a friend's warning and the mourners' walk; you only saw it if someone told you.
        // A face you can read is a choice you can make: keep clear, or go and face it.
        private readonly Dictionary<string, KeyValuePair<int, bool>> _mournsAt = new Dictionary<string, KeyValuePair<int, bool>>();

        private bool MournsYou(string id)
        {
            if (_cfg == null || !_cfg.NameCardsMourns || _engine == null || id == null)
                return false;
            int now = Game.GameTime;
            KeyValuePair<int, bool> was;
            if (_mournsAt.TryGetValue(id, out was) && now - was.Key < 5000 && now >= was.Key)
                return was.Value;
            if (_mournsAt.Count > 400)
                _mournsAt.Clear();
            string wid = _engine.WorryIdOf(id);
            bool m = _engine.MournsYourKill(id, NameCardMournDays) || (wid != id && _engine.MournsYourKill(wid, NameCardMournDays));
            _mournsAt[id] = new KeyValuePair<int, bool>(now, m);
            return m;
        }

        private const int NameCardMournDays = 7;   // a week, as the friends' warning has it

        /// Whether you carry a letter for him not yet given (FindLetter's mark, on his id or his worry's: SEAMS item 21). A
        /// dictionary look or two a card: no scan.
        private bool LetterFor(string id)
        {
            if (_store == null || _engine == null || id == null)
                return false;
            PersonState st = _store.Find(id);
            if ((st == null || !st.Has(FindLetter.HeldMark)) && _engine.WorryIdOf(id) != id)
                st = _store.Find(_engine.WorryIdOf(id));
            return st != null && st.Has(FindLetter.HeldMark) && !st.Has(FindLetter.GivenMark);
        }

        private bool NameCardFill(Ped p, NameCard c, bool everyone)
        {
            string gang = Factions.MemberOf(c.Model, PED.GET_PED_RELATIONSHIP_GROUP_HASH(p.Handle));
            bool got = NameCardFillInner(p, c, everyone && gang == null);
            // the person's lasting id, looked up once a card for the three below (it was three LastingId calls a refill: review 6)
            string cid = null, cname;
            if (got && c.Name != null && !LastingId(p, out cid, out cname))
                cid = null;
            // grief first: a man you saved whose friend you killed since thinks of the killing (the round's review)
            string between = cid != null && MournsYou(cid) ? "Mourns one you killed" : null;
            if (between == null && cid != null)
                between = OwesYou(cid);
            if (between == "Owes you" && c.Line2 != null && c.Line2.StartsWith("Robbed you", StringComparison.Ordinal))
                between = null;   // the thief who robbed you: his card says that, not a debt too
            if (between == null && got && c.Name != null && _cfg.NameCardsLetter && LetterFor(cid))
                between = "You carry their letter";   // the letter you carry for him (SEAMS item 21; Find the letter)
            if (between != null && (c.Line2 == null || c.Line2.IndexOf(between, StringComparison.Ordinal) < 0))
                c.Line2 = string.IsNullOrEmpty(c.Line2) || c.Line2 == "Stranger" ? between : c.Line2 + " - " + between;
            // what they are, before the rest of the line: "Lawman - Proud", "Barkeep" (Lex, 2026-10-30: enRAGE showed a
            // lawman's and a shopkeeper's trade on the card); a stranger's "Stranger" gives way to it, the badge or the apron
            // being plain to see
            string job = got && gang == null ? JobOf(p) : null;
            if (job != null && c.Name != null)
            {
                if (string.IsNullOrEmpty(c.Line2) || c.Line2 == "Stranger")
                    c.Line2 = job;
                else if (c.Line2.IndexOf(job, StringComparison.OrdinalIgnoreCase) < 0)
                    c.Line2 = job + " - " + c.Line2;
            }
            if (gang == null)
                return got;
            if (!got)
            {
                Set(c, Factions.AOne(gang), "", ColourStranger, ColourDim, false);
                return true;
            }
            if (c.Name != null && !c.Name.EndsWith(gang))
                c.Line2 = gang + (string.IsNullOrEmpty(c.Line2) ? "" : " - " + c.Line2);
            return true;
        }

        /// The trade a card shows first: a lawman (the sheriff, a deputy), or a town's counter keeper (Posts); null else.
        private static string JobOf(Ped p)
        {
            if (Natives.IsLaw(p))
            {
                string m = Natives.ModelName(p);
                return m.Contains("sheriff") ? "Sheriff" : m.Contains("deputy") ? "Deputy" : "Lawman";
            }
            PostKind k = PostKindOf(p);
            if (k == null || string.IsNullOrEmpty(k.Role))
                return null;
            string role = k.Role.StartsWith("the ") ? k.Role.Substring(4) : k.Role;
            return Grammar.UpperFirst(role.Replace("general store keeper", "store keeper"));
        }

        // A stranger greeted keeps the name over their head (Lex, 2026-10-30: "when I greet them, they get a different name than
        // they had before the greet"): one of our townsfolk, named on the card, whom enRAGE takes up as one of its own (at the
        // greet, or a moment after). The name we showed is noted each time the card shows it and at the greet; when enRAGE has
        // someone there within the minute, ReconcileName settles the body on enRAGE's shown name. Since 0.7.25 (Run 3, item 1)
        // that is always enRAGE's, and nothing of ours is written into enRAGE: SettleName has given ours enRAGE's name before
        // the greet (enRAGE names everyone within 40 m about once a second), so the card says the same name before and after.
        private readonly Dictionary<int, KeyValuePair<string, int>> _nameAtGreet = new Dictionary<int, KeyValuePair<string, int>>();

        private void NoteNameAtGreet(int h, int now)
        {
            ResBind b;
            if (h == 0 || _bridge.RecordAt(h) != null || !_resBind.TryGetValue(h, out b) || b == null || b.R == null || string.IsNullOrEmpty(b.R.Name))
                return;
            NoteOurName(h, b.R.Name, now);
        }

        private void NoteOurName(int h, string name, int now)
        {
            _nameAtGreet[h] = new KeyValuePair<string, int>(name, now);
            if (_nameAtGreet.Count > 40)
                _nameAtGreet.Remove(_nameAtGreet.OrderBy(kv => kv.Value.Value).First().Key);
        }

        private void KeepNameAtGreet(int h)
        {
            KeyValuePair<string, int> was;
            if (_nameAtGreet.Count == 0 || !_nameAtGreet.TryGetValue(h, out was))
                return;
            int now = Game.GameTime;
            if (now - was.Value > 60000 || now < was.Value)
            {
                _nameAtGreet.Remove(h);   // a while since we named them: whatever enRAGE has there isn't new to this
                return;
            }
            FolkRef ac = _bridge.AcquaintanceAt(h);
            if (ac == null)
                return;
            _nameAtGreet.Remove(h);
            ReconcileName(h, ac, was.Key);
        }

        // The same settling the moment any part of RealNPCs names them (ShownName: the minds, the street's scenes, the ticker),
        // not only when their card next comes round: a brawl's captions called a man Elphege while his card and the minds called
        // him Owen, enRAGE having taken him up between the two with the card off him (Lex, 2026-11-03). Once a body; a handle
        // the game reuses for another model is looked at again.
        private readonly Dictionary<int, uint> _nameReconciled = new Dictionary<int, uint>();

        /// The name a body we had someone in goes by now that enRAGE has someone there too: always enRAGE's, the one its card
        /// and prompt show (Run 3, item 1; Lex's /goal: "enRAGE's visible name (its card and prompt) everywhere, its
        /// acquaintances included, and nothing of ours written into enRAGE's records"). Ours goes by it from now on, everywhere
        /// we keep it. Until 0.7.25 someone enRAGE took up new was given OUR name instead (its record by SetName, its prompt by
        /// RenameNamedAt), so the card wouldn't change at the greet (Lex, 2026-10-30); but enRAGE gives a face it has seen
        /// the same name again by its look's print, so after a reload its card said its own name and its record ours (Friedrich
        /// Keller, Walter Odell, Tip Pierce). Nothing is written into enRAGE now: SettleName gives ours enRAGE's name before
        /// the greet (it names everyone within 40 m about once a second), so the card doesn't change at the greet either.
        private string ReconcileName(int h, FolkRef ac, string ours)
        {
            if (ac == null || string.IsNullOrEmpty(ours))
                return null;
            CarryWakeToEnrage(h, ac);
            string theirs = _bridge.EnrageNameAt(h);   // what enRAGE shows for the body, else its record's name
            if (string.IsNullOrWhiteSpace(theirs))
                theirs = ac.Name;
            if (string.IsNullOrWhiteSpace(theirs) || theirs == ours)
                return ours;
            try
            {
                ResBind rb;
                int changed = 0;
                if (_resBind.TryGetValue(h, out rb) && rb != null && rb.R != null && rb.R.Name == ours && OursToKeep(rb.R))
                {
                    bool labelled = _bridge.RenameNamedAt(h, ours);   // the session's label only, never enRAGE's records
                    Log.Write("NAME " + ours + " (" + (rb.R.Regular ? "a town regular" : "one of the test cast") + ") keeps their name; enRAGE's " + (labelled ? "label for the body says it now" : "card says " + theirs));
                    return ours;
                }
                if (_resBind.TryGetValue(h, out rb) && rb != null && rb.R != null && rb.R.Name == ours)
                {
                    rb.R.Name = theirs;
                    changed = 1 + _store.Rename(rb.R.Id, theirs);
                    MindWho w;
                    if (_mindWho.TryGetValue(h, out w) && w.Name == ours)
                        w.Name = theirs;   // the mind's note of them too, until it looks them up again
                    KeyValuePair<string, int> noted;
                    if (_nameAtGreet.TryGetValue(h, out noted) && noted.Key == ours)
                        _nameAtGreet[h] = new KeyValuePair<string, int>(theirs, noted.Value);
                }
                Log.Write("NAME " + ours + (ac.IsNew ? " was taken up by enRAGE just now" : " is someone enRAGE knew before") + ", " + theirs + " (seen " + ac.TimesSeen + " times): " +
                          (changed > 0 ? "ours goes by " + theirs + " now (" + changed + " records)" : "enRAGE's name shown") + "; nothing written into enRAGE");
                return theirs;
            }
            catch (Exception ex)
            {
                Log.Write("NAME couldn't keep " + ours + "'s name when enRAGE took them up: " + ex.Message);
                return null;
            }
        }

        // One name for one body (Lex, 2026-11-04: "Let's go with your suggestion for matching the names": enRAGE's name
        // everywhere). enRAGE names the people near you for its lock-on prompt (its _named list, Bridge.EnrageNameAt), often
        // without taking them up as its own; the scenes said that name (ShownName) while the card, the minds and the ledgers
        // said our resident's: a brawl and the Nearby line called a man Malachi Kidd (enRAGE's prompt) whose card and thoughts
        // said Per Nilsson (ours), 2026-11-04 13:35. ReconcileName settles a body only once enRAGE has taken someone up there,
        // so his never was. Now our resident takes enRAGE's name the moment any part of the add-on looks them up (ResidentOf)
        // or names them (ShownName), everywhere we keep it (Store.Rename), someone you have already met under ours included
        // (Lex, 2026-11-04, the second answer: "we should try to have enrage name win there"). Why that stays rare and steady:
        // enRAGE names every grown-up within 40 m of you (NamingRadius) about once a second, up to twelve new a scan, by their
        // look (NameByLook: a face it has fingerprinted before gets the same name back, even after a reload), and keeps the
        // name while they're about (an unmet stranger forgotten 15 s after leaving 45 m, the met after 30 min). So our name
        // gives way before you ever meet most people, and a name you've learned changes once at most: for someone we named
        // before enRAGE's scan reached them. Each body is looked at once in NameSettleGapMs at most (a reflection read of
        // enRAGE's list).
        private readonly Dictionary<int, int> _nameSettleAt = new Dictionary<int, int>();   // body -> when it may be looked at again
        private const int NameSettleGapMs = 2000;
        private int _namesTaken, _namesMetChanged;   // ours renamed to enRAGE's; of them, people you'd met under ours

        /// The name this resident goes by now that enRAGE has named the body: enRAGE's (ours renamed to it, everywhere). Null:
        /// nothing settled (no enRAGE name, the same name, or looked at just now).
        private string SettleName(int h, Resident r)
        {
            if (r == null || h == 0 || string.IsNullOrEmpty(r.Name) || _bridge == null || !_bridge.Present || _store == null)
                return null;
            int now = Game.GameTime, next;
            if (_nameSettleAt.TryGetValue(h, out next) && now < next && next - now <= NameSettleGapMs)
                return null;
            if (_nameSettleAt.Count > 500)
                _nameSettleAt.Clear();
            _nameSettleAt[h] = now + NameSettleGapMs;
            string theirs = _bridge.EnrageNameAt(h);
            if (string.IsNullOrWhiteSpace(theirs) || theirs == r.Name)
                return null;
            string ours = r.Name;
            try
            {
                if (OursToKeep(r))
                {
                    // a town regular or one of the test cast: their identity is written onto each body they're cast into, so it is
                    // theirs wherever they go; enRAGE's label for this body (its card and prompt, the session only) takes it
                    if (!_bridge.RenameNamedAt(h, ours))
                        return null;
                    Log.Write("NAME " + ours + " (" + (r.Regular ? "a town regular" : "one of the test cast") + ") keeps their name; enRAGE's label for the body was " + theirs);
                    return ours;
                }
                bool met = MetUnderOurs(h, r);   // only for the log: enRAGE's name wins either way
                r.Name = theirs;
                int changed = 1 + _store.Rename(r.Id, theirs);
                MindWho w;
                if (_mindWho.TryGetValue(h, out w) && w.Name == ours)
                    w.Name = theirs;   // the mind's note of them too, until it looks them up again
                KeyValuePair<string, int> noted;
                if (_nameAtGreet.TryGetValue(h, out noted) && noted.Key == ours)
                    _nameAtGreet[h] = new KeyValuePair<string, int>(theirs, noted.Value);   // the greet's own settling (KeepNameAtGreet) keeps this name, not the old one
                _namesTaken++;
                if (met)
                    _namesMetChanged++;
                Log.Write("NAME " + ours + " goes by " + theirs + ", enRAGE's name for the body, everywhere (" + changed + " records" + (met ? "; you'd met them as " + ours : "") + ")");
                return theirs;
            }
            catch (Exception ex)
            {
                Log.Write("NAME couldn't settle " + ours + "'s name: " + ex.Message);
                return null;
            }
        }

        /// People whose identity the add-on writes onto a body: a town regular (dressed in their own outfit wherever they're cast),
        /// the test cast, and a returner once they've come back in their own outfit (Run 3, item 5). Their name is theirs and wins
        /// over the one enRAGE gives the body by its look, which would be a new one in every body they're cast into (Run 3, item
        /// 1's follow-up: enRAGE's name everywhere else).
        private static bool OursToKeep(Resident r)
        {
            return r != null && (r.Regular || r.Cast != null || (r.HasOutfit && r.Returns > 0));
        }

        /// Whether you know this resident by our name: talked, told their name, flirted, any contact, they spoke up to you,
        /// or you greeted them lately (the card's "met"). Only the log says so now; enRAGE's name wins either way.
        private bool MetUnderOurs(int h, Resident r)
        {
            PersonState st = _store.Find(r.Id);
            int greetedAt;
            return (st != null && (st.Talks > 0 || st.KnowsName || st.Flirts > 0 || st.ContactAt > 0 || st.LastRemarkAt > 0)) || _greetedAt.TryGetValue(h, out greetedAt);
        }

        /// The name enRAGE shows for one of its acquaintances (its card and prompt: Bridge.ShownNameOf), else their record's
        /// full name (Run 3, item 1).
        private string FolkShown(FolkRef ac)
        {
            if (ac == null)
                return null;
            string shown = _bridge != null ? _bridge.ShownNameOf(ac) : null;
            return shown ?? ac.Name;   // the record's name without its title, as enRAGE's card gives it (the review)
        }

        private bool NameCardFillInner(Ped p, NameCard c, bool everyone)
        {
            int h = p.Handle;
            bool woman = !PED.IS_PED_MALE(h);
            RecordRef rec = _bridge.RecordAt(h);
            if (_enrageCardsOn && !_cfg.NameCardsBesideEnrage && (rec != null || _bridge.AcquaintanceAt(h) != null))
                return false;   // enRAGE draws its own over its people: one card a head
            if (rec != null && rec.IsAlive)
            {
                Person who = PersonOf(rec);
                int hate = who.Hate;
                int[] colour = hate >= 70 ? new[] { 205, 55, 45 } : hate >= 40 ? new[] { 215, 135, 50 } : new[] { 205, 185, 100 };
                string tier = hate >= 70 ? "Murderous" : hate >= 40 ? "Vengeful" : hate >= 15 ? "Sore" : "Wary";
                Set(c, who.Name, (who.Personality != null ? who.Personality + " - " : "") + tier, colour, colour, true);
                return true;
            }
            KeepNameAtGreet(h);
            FolkRef ac = _bridge.AcquaintanceAt(h);
            if (ac != null && !string.IsNullOrEmpty(ac.Name))
            {
                bool met = ac.Met;
                string temper = ac.Temper != null && ac.Temper.Length > 0 ? Grammar.UpperFirst(ac.Temper) : null;
                // no name until you've met: "Stranger" alone (Lex, 2026-10-30: "have their name show as Stranger until they are
                // greeted or interacted with")
                string shown = FolkShown(ac);   // the name enRAGE's own card and prompt show (Run 3, item 1)
                if (met)
                    Set(c, shown, temper ?? "", ColourTitle, ColourDim, true);
                else if (_cfg.NameCardsStrangerNames)
                    Set(c, shown, "Stranger", ColourStranger, ColourDim, true);   // the name with Stranger under it, if asked for
                else
                    Set(c, "Stranger", "", ColourStranger, ColourDim, true);
                return true;
            }
            ResBind b;
            if (!_resBind.TryGetValue(h, out b) && IsHermit(h))
                b = new ResBind { R = _hermit.R, Model = _hermit.Model };   // the hermit at their camp is one of ours, never bound to the town (Lex, 2026-10-30: no name over her)
            if (b != null && b.R != null && !b.R.Dead && !string.IsNullOrEmpty(b.R.Name))
            {
                // met: talked, greeted, flirted, any contact, or they saw you do something; else a stranger, whatever their temper
                // (Lex, 2026-10-29: "it should show Stranger until the main character has greeted or interacted with the NPC")
                PersonState st = _store != null ? _store.Find(b.R.Id) : null;
                int greetedAt;
                // met: you greeted, talked, flirted or had any contact, or they spoke up to you; only seeing you do something isn't
                // meeting (Lex, 2026-10-30)
                bool met = (st != null && (st.Talks > 0 || st.KnowsName || st.Flirts > 0 || st.ContactAt > 0 || st.LastRemarkAt > 0)) || _greetedAt.TryGetValue(h, out greetedAt);
                string temper = b.R.Personality != null && b.R.Personality.Length > 0 ? b.R.Personality : null;
                string trade = b.R.Trade != null && b.R.Trade.Length > 0 ? Grammar.UpperFirst(b.R.Trade.StartsWith("a ") ? b.R.Trade.Substring(2) : b.R.Trade.StartsWith("an ") ? b.R.Trade.Substring(3) : b.R.Trade) : null;
                string line2 = temper != null && trade != null ? temper + " - " + trade : temper ?? trade ?? "";
                NoteOurName(h, b.R.Name, Game.GameTime);   // shown (or meant) as ours while enRAGE has no one in this body
                if (st != null && st.Has(PickpocketMark))
                    line2 = "Pickpocket" + (temper != null ? " - " + temper : "");   // the trade he keeps to (Lex, 2026-10-30)
                if (st != null && _cfg.NameCardsRobbed && st.Has(RobbedYouMark))
                    line2 = "Robbed you" + (line2.Length > 0 ? " - " + line2 : "");   // the one who got away with yours (SEAMS item 9)
                if (met)
                    Set(c, b.R.Name, line2, ColourTitle, ColourDim, true);
                else if (_cfg.NameCardsStrangerNames)
                    Set(c, b.R.Name, "Stranger", ColourStranger, ColourDim, true);
                else
                    Set(c, "Stranger", "", ColourStranger, ColourDim, true);   // no name until you've met (Lex, 2026-10-30)
                return true;
            }
            // the game's own named people: the story characters and the uniques the game names (StoryCast.cs; Lex, 2026-10-29: the
            // sheriff of Valentine had his name under enRAGE and not under us)
            string ml = Models.Name(c.Model);
            StoryCast.Person sp;
            if (StoryCast.TryGet(ml, out sp))
            {
                Set(c, sp.Name, StoryCast.JobFor(ml) ?? sp.Temper ?? "", ColourTitle, ColourDim, true);   // "Valentine sheriff", else the temper
                return true;
            }
            string gameName = StoryCast.NameFor(ml);
            if (gameName != null)
            {
                Set(c, gameName, ml.Contains("_bht_") || ml.Contains("bounty") ? "Wanted" : "", ColourTitle, ColourDim, true);
                return true;
            }
            if (!everyone || Natives.StoryModel(p) || !IsGrownUp(p))
                return false;
            Set(c, Grammar.UpperFirst(Director.Kind(p, woman)), "Stranger", ColourStranger, ColourDim, false);
            return true;
        }

        private static void Set(NameCard c, string name, string line2, int[] colour, int[] colour2, bool known)
        {
            c.Name = name;
            c.Line2 = line2;
            c.R = colour[0]; c.G = colour[1]; c.B = colour[2];
            c.R2 = colour2[0]; c.G2 = colour2[1]; c.B2 = colour2[2];
            c.Known = known;
        }

        private static float OneEuroAlpha(float cutoff, float dt)
        {
            float tau = 1f / ((float)Math.PI * 2f * Math.Max(0.01f, cutoff));
            return 1f / (1f + tau / dt);
        }

        /// The width of text in the game's title font in its own units (enRAGE's table, with lower case at 0.8): x scale x 0.0205
        /// gives screen fractions; kept on a card while its text stands.
        private static float TitleUnits(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0f;
            float w = 0f;
            foreach (char c in s)
            {
                float u;
                switch (char.ToUpperInvariant(c))
                {
                    case 'I': u = 0.55f; break;
                    case 'J': u = 0.6f; break;
                    case 'L': u = 0.8f; break;
                    case 'F': case 'E': case 'S': case 'T': case 'Z': u = 0.85f; break;
                    case 'B': case 'P': case 'R': u = 0.92f; break;
                    case 'C': case 'K': case 'Y': u = 0.97f; break;
                    case 'D': case 'G': u = 1.07f; break;
                    case 'O': case 'Q': u = 1.06f; break;
                    case 'M': u = 1.3f; break;
                    case 'W': u = 1.45f; break;
                    case ' ': u = 0.45f; break;
                    case '-': u = 0.55f; break;
                    case ',': case '.': case ':': u = 0.35f; break;
                    case '\'': u = 0.18f; break;
                    default: u = char.IsDigit(c) ? 0.9f : 1f; break;
                }
                w += char.IsLower(c) ? u * 0.8f : u;
            }
            return w;
        }

        private string NameCardsStatus()
        {
            // who is near, by model and relationship group: which token or group marks a gang member (Lex's Lemoyne Raiders, 2026-10-29)
            Ped meP = Game.Player.Character;
            string near = string.Join(", ", Peds.All().Where(x => x != null && x.Exists() && x.Handle != meP.Handle && PED.IS_PED_HUMAN(x.Handle) && x.Position.DistanceTo(meP.Position) < 12f)
                .OrderBy(x => x.Position.DistanceTo(meP.Position)).Take(6)
                .Select(x => (_nameCards.ContainsKey(x.Handle) && _nameCards[x.Handle].Name != null ? _nameCards[x.Handle].Name : "?") + " " + Natives.ModelName(x) + " rel 0x" + PED.GET_PED_RELATIONSHIP_GROUP_HASH(x.Handle).ToString("X8") + (Factions.MemberOf(ENTITY.GET_ENTITY_MODEL(x.Handle)) is string g ? " (" + g + ")" : "")));
            Log.Write("TEST near models: " + near);
            if (_cfg.NameCards == "off")
                return "off ([Display] NameCards)";
            string left = _bridge != null && _bridge.Present && NameCardsLeftToEnrage(_now) ? "; left to enRAGE's own cards (NameCardsBesideEnrage off)" : "";
            return _cfg.NameCards + (_cfg.NameCardsSecondLine ? "" : ", names alone") + " within " + (int)_cfg.NameCardsMetres + "/" + (int)_cfg.NameCardsKnownMetres + " m; " + _nameCardsUp + " up now" + (_nameHeldFrames > 0 ? " (short of the frame's text in " + _nameHeldFrames + " frames this session)" : "") + ", " + _nameCardsNow.Count +
                   " in range, " + _nameCards.Count + " kept; " + _nameCardsMade + " drawn this session; " + _nameCardsDormant + " card-frames passed over behind a wall, faded out (FOURTEENHOUR item 2)" + left;
        }
    }
}
