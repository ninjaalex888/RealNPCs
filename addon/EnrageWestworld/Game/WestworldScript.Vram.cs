// The VRAM gauge (Lex, 2026-11-01: "a small VRAM gauge at the top left of the screen informing users the current VRAM
// load on their GPU"; "Top left to see how it looks"): "VRAM 11.3 / 15.6 GB" in small type at the top left, below the
// game's own updates (where the territory and gang news goes too), white, amber over 85% of the card, red over 95%
// (Core/Vram.cs). [Display] VramGauge: off, on, or model (the default: only while [Mind] Language, the AI model, is on).
// The numbers are read every 2.5 s off the game thread (Game/VramReader.cs, started the first time the gauge is shown);
// the frame reads its latest Sample (one volatile reference) and draws one text over a faint box, the box's width worked
// out once per new reading. Hidden with the HUD's rules: the settings panel, the papers, the pause menu, a hidden HUD,
// a fade, a cutscene, enRAGE's menus (TickerVisible). F7: TEST vram.
using System;
using System.Diagnostics;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        // where it sits (one place to move it): the left edge the Talk menu's words use, under the game's top-left updates
        private const float VramX = 0.035f, VramY = 0.215f, VramScale = 0.25f;

        private int _vramMode = Vram.WithModel;
        private bool _vramShown;
        private VramReader.Sample _vramDrawn;
        private float _vramW;
        private int _vramR = 255, _vramG = 255, _vramB = 255;
        private Action _aVram;

        /// From ApplySettings: the mode, whether the gauge shows now, and the reader told.
        private void VramApply()
        {
            _vramMode = Vram.ModeOf(_cfg.VramGauge);
            _vramShown = Vram.Shown(_vramMode, _cfg.Language);
            VramReader.Want(_vramShown);
        }

        private void VramStep()
        {
            if (!_vramShown)
                return;
            if (_aVram == null)
                _aVram = () => VramFrame(_now);
            Safe("vram gauge", _aVram);
        }

        private void VramFrame(int now)
        {
            if (!VramReader.Started)
            {
                int pid = 0;
                try { pid = Process.GetCurrentProcess().Id; } catch { }
                VramReader.Start(text => Draw.Formed(text), Log.Write, pid);
            }
            VramReader.Sample s = VramReader.Latest;
            if (s == null || PanelOpen || _reader.Open || !TickerVisible(now))
                return;
            if (!ReferenceEquals(s, _vramDrawn))
            {
                _vramDrawn = s;
                _vramW = Draw.Width(s.Text, VramScale);
                Vram.Colour(s.Tier, out _vramR, out _vramG, out _vramB);
            }
            const float padX = 0.005f, padY = 0.003f;
            Draw.Rect(VramX - padX, VramY - padY, _vramW + padX * 2f, Draw.LineHeight(VramScale) + padY * 2f, 12, 10, 8, 140);
            Draw.TextFormed(s.Formed, VramX, VramY, VramScale, _vramR, _vramG, _vramB, 235);
        }

        /// F7: the setting, whether it shows now, and the reader's own account.
        private string VramStatus()
        {
            string mode = _vramMode == Vram.Off ? "off" : _vramMode == Vram.On ? "on" : "model (only while the AI model is on)";
            return mode + ", " + (_vramShown ? "shown" : "not shown") + " ([Display] VramGauge); " + VramReader.Status();
        }
    }
}
