// Known by the horse, game side (LIVES item 6a; [World] HorseTalk; Core/HorseTalk.cs): once a second, the horse the player
// rides, in words from its model, handed to the engine for the deeds and the tales.
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _horseTalkNext, _horseTalkMount;
        private string _horseTalkWords;

        private void HorseTalkStep(int now)
        {
            if (!HorseTalk.On || !_cfg.HorseTalk || _engine == null || _store == null || now < _horseTalkNext)
                return;
            _horseTalkNext = now + 1000;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            bool on = PED.IS_PED_ON_MOUNT(me.Handle);
            _engine.PlayerMounted = on;
            if (!on)
                return;
            int mount = PED.GET_MOUNT(me.Handle);
            if (mount != _horseTalkMount)
            {
                _horseTalkMount = mount;
                _horseTalkWords = mount != 0 && ENTITY.DOES_ENTITY_EXIST(mount) ? HorseTalk.Words(Natives.ModelName(new Ped(mount))) : null;   // read once a horse
                if (_horseTalkWords != null)
                    Log.Detail("HORSE talk: you ride " + _horseTalkWords);
            }
            _engine.PlayerHorse = _horseTalkWords;   // null on a mount whose words can't be read: not the last horse's (the review)
            _engine.PlayerHorseAt = _store.LastGameMin;
        }
    }
}
