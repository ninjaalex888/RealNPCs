// Doctored brands, the game side (DEPTH item 8; Core/Brands.cs; [Scenes] Brands). The accusation itself is a street reason
// (Core/Reasons.cs) the Director plays like any other; here: the rustlers' fire you came upon on the road (WestworldScript.Wayside.cs
// tells BrandsSawRustlers; in memory, two game days), and a look every 500 ms (the frame pass's scheduler; Tick04's own call with it
// off) that, while a "blamed" scene plays within 14 m of you and you saw the fire, offers J once a scene: tell him who it was. Then
// the rest of the scene is his apology and the neighbour's thanks (Director.Rewrite), and a "cleared" tie undoes the blame.
// Runners: the Director's street scenes and the J menu (both seen); the road table's rustlers (built, not yet seen).
using System;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private long _rustlersSeenAt = -1;
        private Action _aBrands;
        private string _rustlersTown, _brandsOfferedFor, _brandsLast;
        private int _brandsNextLook, _brandsOffered, _brandsTold, _brandsSeenFires;

        /// From the road scene: you came upon the rustlers at their branding fire (near this town, if any).
        private void BrandsSawRustlers(string town)
        {
            _rustlersSeenAt = Bridge.Now();
            _rustlersTown = town;
            _brandsSeenFires++;
            Log.Write("BRANDS you saw rustlers at a branding fire" + (town != null ? " near " + town : "") + ": a ranch man who blames his neighbour can be told, for two game days");
        }

        private bool BrandsCanTell(long g)
        {
            return _rustlersSeenAt >= 0 && g - _rustlersSeenAt >= 0 && g - _rustlersSeenAt < Brands.SawWithinMinutes;
        }

        private void BrandsStep(int now)
        {
            if (!Brands.On || now < _brandsNextLook)
                return;
            _brandsNextLook = now + 500;
            Safe("brands", _aBrands ?? (_aBrands = () => BrandsLook(_now)));   // a kept delegate: no closure a frame (review 1)
        }

        private void BrandsLook(int now)
        {
            if (!_director.Busy || _director.CurrentKind != Brands.Kind || _director.CurrentIntruded || _director.FightOnFor(now) > 0)
                return;
            string seed = _director.CurrentSeed;
            if (seed == null || seed == _brandsOfferedFor || !BrandsCanTell(Bridge.Now()))
                return;
            if (_eventMenu != null || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil))
                return;
            Ped a = _director.CastAt(0), b = _director.CastAt(1), me = Game.Player.Character;
            if (a == null || b == null || !a.Exists() || !b.Exists() || PED.IS_PED_ON_MOUNT(me.Handle) || Math.Min(me.Position.DistanceTo(a.Position), me.Position.DistanceTo(b.Position)) > Brands.NearMetres)
                return;
            _brandsOfferedFor = seed;
            _brandsOffered++;
            PromptSet(Brands.Kind, new TownEvent { Kind = Brands.Kind, Town = _placeTown, AName = _director.NameAt(0), Extra = _director.NameAt(1) }, 8000);
        }

        private void BrandsChoose(string id)
        {
            if (id != "brands:tell")
                return;
            Safe("brands", () => BrandsTell(Game.GameTime));
        }

        private void BrandsTell(int now)
        {
            Ped me = Game.Player.Character, a = _director.CastAt(0), b = _director.CastAt(1);
            if (!_director.Busy || _director.CurrentKind != Brands.Kind || _director.CurrentSeed != _brandsOfferedFor || a == null || b == null)
            {
                Log.Write("BRANDS: too late to tell them, it's over");
                return;
            }
            string seed = "brands|" + _brandsOfferedFor;
            SpeechCaption("You", Brands.YourWord(seed), me.Handle, 3500, false, false);
            Scene s = Brands.Truth(_director.CurrentInfo, seed);
            if (!_director.Rewrite(s, "you told them about the rustlers' fire"))
            {
                Log.Write("BRANDS: the scene couldn't take it");
                return;
            }
            MindWho wa = MindKnown(a.Handle, now), wb = MindKnown(b.Handle, now);
            if (wa != null && wb != null && wa.Id != null && wb.Id != null && _store != null)
                Ties.Note(_store.Ties, wa.Id, wa.Name, wb.Id, wb.Name, _placeTown, "cleared", Bridge.Now());   // the blame taken back
            _brandsTold++;
            _brandsLast = (wa != null ? wa.Name : "the ranch man") + " took it back from " + (wb != null ? wb.Name : "his neighbour");
            Log.Write("BRANDS you told them it was rustlers: " + _brandsLast);
        }

        private string BrandsStatus()
        {
            if (!Brands.On)
                return "off ([Scenes] Brands)";
            return "rustlers' fires seen " + _brandsSeenFires + (BrandsCanTell(Bridge.Now()) ? " (you can tell, now)" : "") + ", offered " + _brandsOffered + ", told " + _brandsTold + (_brandsLast != null ? "; the last: " + _brandsLast : "");
        }
    }
}
