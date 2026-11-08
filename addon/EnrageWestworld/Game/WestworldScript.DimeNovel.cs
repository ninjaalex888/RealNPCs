// Dime novels in game (FIVEDAY reserve R12; Core/DimeNovel.cs, Core/Engine.DimeNovel.cs). Every thirty seconds in a town,
// while a novel about you is being read (thirty days from its Monday): one of the named people near thinks of it by their
// temper group (Minds.TemperGroup): the bold admire it, the fearful believe it, the cold scoff, the rest are tickled, a few
// think the man himself looks smaller. The title came with the box's notice. Nothing is spawned or directed. Behind
// [World] DimeNovels.
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _novelNextLook, _novelThoughts;

        private void NovelStep(int now)
        {
            if (_cfg == null || !_cfg.DimeNovels || _engine == null || _placeTown == null || now < _novelNextLook)
                return;
            _novelNextLook = now + 30000;
            if (_engine.NovelNow(Bridge.Now()) == null)
                return;
            Safe("dime novel", () =>
            {
                long day = Bridge.Now() / 1440;
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !Voice.Chance(w.Id + "|novel|" + day, 40))
                        continue;
                    _ticker.Push((w.Name ?? "Someone") + ", thinking", DimeNovel.ReaderThought(Minds.TemperGroup(w.P.Personality), _engine.Player.Surname, w.Id + "|" + day), "near", h);
                    _novelThoughts++;
                    break;
                }
            });
        }

        private string NovelStatus()
        {
            if (_cfg == null || !_cfg.DimeNovels)
                return "off ([World] DimeNovels)";
            string t = _engine.NovelNow(Bridge.Now());
            return (t != null ? "being read: " + t : "none being read") + "; " + _novelThoughts + " readers' thoughts this session";
        }
    }
}
