using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.Diagnostics;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;

namespace Vc4Test1
{
    public class ControlSystem : CrestronControlSystem
    {
        private XpanelForSmartGraphics tp1;
        private bool bSystemPowerOn;

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                // System defaults to OFF
                bSystemPowerOn = false;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in ControlSystem constructor: {0}", e.Message);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                tp1 = new XpanelForSmartGraphics(0x03, this);
                tp1.SigChange += tp_SigChange;
                tp1.BooleanOutput[12].UserObject = new Action<bool>(b => { if (b) ToggleSystemPower(); });
                tp1.Register();
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        public void tp_SigChange(BasicTriList dev, SigEventArgs args)
        {
            var obj = args.Sig.UserObject;

            if (obj is Action<bool>)
            {
                var func = (Action<bool>)obj;
                func(args.Sig.BoolValue);
            }
        }

        void ToggleSystemPower()
        {
            bSystemPowerOn = !bSystemPowerOn;

            tp1.BooleanInput[10].BoolValue = bSystemPowerOn;
            tp1.BooleanInput[11].BoolValue = !bSystemPowerOn;
        }
    }
}