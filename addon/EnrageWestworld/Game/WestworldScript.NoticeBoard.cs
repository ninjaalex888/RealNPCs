// The notice board in game (FIVEDAY item 16; Core/NoticeBoard.cs). Two places show it: the reader's "Notice board" page
// (made each time the reader opens, last but for "Who's who") and, when you come into a town that has something posted, one
// item in the "Around town" box (once a town a game day). enRAGE's funerals come from its plans, by the town their plot lies
// in. F7 "TEST notices:". Behind [World] NoticeBoard.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string _noticeTown;
        private int _nextNoticeLook, _noticePushed;
        private readonly Dictionary<string, long> _noticeDay = new Dictionary<string, long>();   // town -> the game day its box item was shown
        private Action _aNotices;

        /// Everything posted for the coming week.
        private List<Notice> NoticesNow(long g)
        {
            var extra = new List<Notice>();
            if (_bridge != null)
            {
                try
                {
                    foreach (PlanInfo p in _bridge.Plans())
                    {
                        if (p == null || p.Kind != "funeral" || p.Done || p.DueAt < g - 60 || p.DueAt > g + NoticeBoard.Days * 1440L)
                            continue;
                        string town = EnrageWestworld.InGame.Places.TownAt(p.Pos);
                        if (town != null)
                            extra.Add(new Notice { At = p.DueAt, Town = town, Kind = "funeral", Text = "the funeral" + (string.IsNullOrEmpty(p.Dead) ? "" : " of " + p.Dead) });
                    }
                }
                catch (Exception ex)
                {
                    Log.Detail("NOTICES enRAGE's funerals unread: " + ex.Message);
                }
            }
            var towns = _placeTown != null ? new[] { _placeTown } : new string[0];
            return NoticeBoard.Gather(_store, g, InnerLife.Weekday, towns, extra);
        }

        /// The reader's page, or null with the board off.
        private Paper NoticeBoardPaper()
        {
            if (_cfg == null || !_cfg.NoticeBoard || _store == null)
                return null;
            try
            {
                long g = Bridge.Now();
                return new Paper { Kind = "note", Title = "Notice board", From = "the town boards", Body = NoticeBoard.Body(NoticesNow(g), _placeTown, g, InnerLife.Weekday), Delivered = true, Read = true };
            }
            catch (Exception ex)
            {
                Log.Write("NOTICES page failed: " + ex.Message);
                return null;
            }
        }

        /// Into a town: what it has posted, in the box, once a town a game day.
        private void NoticeBoardStep(int now)
        {
            if (_cfg == null || !_cfg.NoticeBoard || _store == null || now < _nextNoticeLook)
                return;
            _nextNoticeLook = now + 5000;
            if (_placeTown == _noticeTown)
                return;
            _noticeTown = _placeTown;
            if (_placeTown == null)
                return;
            if (_aNotices == null)
                _aNotices = () =>
                {
                    long g = Bridge.Now();
                    long seen;
                    if (_noticeDay.TryGetValue(_noticeTown, out seen) && seen == g / 1440)
                        return;
                    string box = NoticeBoard.Box(NoticesNow(g), _noticeTown, g, InnerLife.Weekday);
                    if (box == null)
                        return;
                    _noticeDay[_noticeTown] = g / 1440;
                    _noticePushed++;
                    NewsPush("The board in " + _noticeTown, box, "notice");
                    Log.Write("NOTICES " + _noticeTown + ": " + box);
                };
            Safe("notice board", _aNotices);
        }

        private string NoticeBoardStatus()
        {
            if (_cfg == null || !_cfg.NoticeBoard)
                return "off ([World] NoticeBoard)";
            if (_store == null)
                return "no store yet";
            long g = Bridge.Now();
            List<Notice> all = NoticesNow(g);
            return all.Count + " on the boards this week (" + string.Join(", ", all.GroupBy(n => n.Kind).Select(k => k.Count() + " " + k.Key)) + "); shown in the box " + _noticePushed + " times this session";
        }
    }
}
