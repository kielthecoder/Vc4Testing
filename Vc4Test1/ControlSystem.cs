using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.Diagnostics;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using Crestron.SimplSharpPro.DM.Streaming;

namespace Vc4Test1
{
    public enum SystemJoins
    {
        PowerOn = 10,
        PowerOff = 11,
        PowerToggle = 12,
        PowerTransition = 13
    }

    public enum SystemFb
    {
        PowerOnFb = 10,
        PowerOffFb = 11,
        PowerToggleFb = 12
    }

    public class ControlSystem : CrestronControlSystem
    {
        private XpanelForSmartGraphics tp1;
        private DmNvxE30 txLaptop, txCamera, txCodec;
        private DmNvxD30 rxDisplay, rxCodecCamera, rxCodecContent;
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
                // Touchpanels

                tp1 = new XpanelForSmartGraphics(0x03, this);
                
                tp1.OnlineStatusChange += tp_OnlineChange;
                tp1.UserSpecifiedObject = new Action<bool>(online => { if (online) UpdateFeedback(); });
                
                tp1.SigChange += tp_SigChange;
                tp1.BooleanOutput[(uint)SystemJoins.PowerToggle].UserObject = new Action<bool>(press => { if (press) ToggleSystemPower(); });
                tp1.BooleanOutput[(uint)SystemJoins.PowerTransition].UserObject = new Action<bool>(done => { if (done) UpdatePowerStatusText(); });
                
                tp1.Register();

                // Receivers

                rxDisplay = new DmNvxD30(0x13, this);
                rxDisplay.Register();

                rxCodecCamera = new DmNvxD30(0x14, this);
                rxCodecCamera.Register();

                rxCodecContent = new DmNvxD30(0x15, this);
                rxCodecContent.Register();

                // Transmitters

                txLaptop = new DmNvxE30(0x10, this);
                txLaptop.SourceTransmit.StreamChange += tx_StreamChange;
                txLaptop.UserSpecifiedObject = new Action<string>(url => { SetStreamURL(rxCodecContent, url); });
                txLaptop.Register();

                txCamera = new DmNvxE30(0x11, this);
                txCamera.SourceTransmit.StreamChange += tx_StreamChange;
                txCamera.UserSpecifiedObject = new Action<string>(url => { SetStreamURL(rxCodecCamera, url); });
                txCamera.Register();

                txCodec = new DmNvxE30(0x12, this);
                txCodec.SourceTransmit.StreamChange += tx_StreamChange;
                txCodec.UserSpecifiedObject = new Action<string>(url => { SetStreamURL(rxDisplay, url); });
                txCodec.Register();
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        public void tp_OnlineChange(GenericBase dev, OnlineOfflineEventArgs args)
        {
            var obj = dev.UserSpecifiedObject;

            if (obj is Action<bool>)
            {
                var func = (Action<bool>)obj;
                func(args.DeviceOnLine);
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

        public void tx_StreamChange(Stream stream, StreamEventArgs args)
        {
            var obj = (DmNvxE30)stream.Owner;
            var uo = obj.UserSpecifiedObject;

            if (uo is Action<string>)
            {
                var func = (Action<string>)uo;
                func(obj.Control.ServerUrlFeedback.StringValue);
            }
        }

        void UpdateFeedback()
        {
            tp1.BooleanInput[(uint)SystemFb.PowerOnFb].BoolValue = bSystemPowerOn;
            tp1.BooleanInput[(uint)SystemFb.PowerOffFb].BoolValue = !bSystemPowerOn;
            tp1.BooleanInput[(uint)SystemFb.PowerToggleFb].BoolValue = bSystemPowerOn;
        }

        void UpdatePowerStatusText()
        {
            if (bSystemPowerOn)
                tp1.StringInput[10].StringValue = "ON";
            else
                tp1.StringInput[10].StringValue = "OFF";
        }

        void ToggleSystemPower()
        {
            bSystemPowerOn = !bSystemPowerOn;

            UpdateFeedback();
        }

        void SetStreamURL(DmNvxD30 rx, string url)
        {
            rx.Control.ServerUrl.StringValue = url;
        }
    }
}