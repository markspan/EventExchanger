using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Encodings;
using HidSharp.Utility;

namespace ID
{
    public class EventExchangerLister
    {
        // ===================================================================================
        // ===================================================================================
        // Constants to define command-codes given to connected EventExchanger via USB port.                                     
        // ===================================================================================
        // ===================================================================================
        private object EventBufferLock = new object();

        const Byte CLEAROUTPUTPORT = 0;   // 0x00
        const Byte SETOUTPUTPORT = 1;   // 0x01
        const Byte SETOUTPUTLINES = 2;   // 0x02
        const Byte SETOUTPUTLINE = 3;   // 0x03
        const Byte PULSEOUTPUTLINES = 4;   // 0x04
        const Byte PULSEOUTPUTLINE = 5;   // 0x05

        const Byte SENDLASTOUTPUTBYTE = 10;   // 0x0A

        const Byte CONVEYEVENT2OUTPUT = 20;   // 0x14
        const Byte CONVEYEVENT2OUTPUTEX = 21;   // 0x15
        const Byte CANCELCONVEYEVENT2OUTPUT = 22;   // 0x16

        const Byte CANCELEVENTREROUTES = 30;   // 0x1E
        const Byte REROUTEEVENTINPUT = 31;   // 0x1F

        const Byte SETUPROTARYCONTROLLER = 40;        // 0x28
        const Byte SETROTARYCONTROLLERPOSITION = 41;  // 0x29

        const Byte CONFIGUREDEBOUNCE = 50;   // 0x32

        const Byte SETWS2811RGBLEDCOLOR = 60;  // 0x3C
        const Byte SENDLEDCOLORS = 61;          // 0x3D

        const Byte SWITCHALLLINESEVENTDETECTION = 100;   // 0x64
        const Byte SWITCHLINEEVENTDETECTION = 101;   // 0x65

        const Byte SETANALOGINPUTDETECTION = 102;   // 0x66
        const Byte REROUTEANALOGINPUT = 103;            // 0X67
        const Byte SETANALOGEVENTSTEPSIZE = 104;     // 0X68

        const Byte SWITCHDIAGNOSTICMODE = 200;   // 0xC8
        const Byte SWITCHEVENTTEST = 201;   // 0xC9

        Byte CurrentButtons = 0;
        Byte PreviousButtons = 0;

        DeviceList list = DeviceList.Local;

        private  struct status
        {
            public status(double _oldVal, double _newVal)
            {
                oldval = _oldVal;
                newval = _newVal;
            }

            public double oldval { get; set; }
            public double newval { get; set; }

            public override string ToString() => $"({oldval}, {newval})";
        }

        Dictionary<string, status> AxisAndButtons =
             new Dictionary<string, status>();

        Thread Poller;

        private IEnumerable<HidDevice> HIDdeviceList;
        private HidDevice device = null;
        private const String _Version = "0.99x";

        char[] AxisId = new char[] { 'X', 'Y', 'Z', 'A', 'B' };

        // ===========================================================================================
        public string Version()
        // ===========================================================================================
        {
            return _Version;
        }
        // ===========================================================================================
        // ===========================================================================================
        public string DeviceName()
        // -------------------------------------------------------------------------------------------
        //    Returns a string, containing The friendly name of the current output device.
        // -------------------------------------------------------------------------------------------
        {
            if (device != null)
                return device.GetFriendlyName();
            else
                return "EventExchanger.dll -> Device info is not accessible, device is not yet loaded ...";
        }

        // ===========================================================================================
        public List<string> GetProductNames()
        // ===========================================================================================
        {
            HIDdeviceList = list.GetHidDevices().ToArray();
            List<string> ProductNames = new List<String>();
            foreach (HidDevice idev in HIDdeviceList)
            {
                if (idev.GetProductName().Contains("EventExchanger"))
                {
                    ProductNames.Add(idev.GetProductName() + " #-# " + idev.GetSerialNumber());
                    device = idev;
                }
            }
            if (ProductNames.Count != 1)
                device = null;

            return ProductNames;
        }
        // ==========================================================================================
        public List<string> Attached()
        // ==========================================================================================
        {
            HIDdeviceList = list.GetHidDevices().ToArray();

            List<string> ProductNames = new List<String>();
            foreach (HidDevice idev in HIDdeviceList)
            {
                if (idev.GetProductName().Contains("EventExchanger"))
                {
                    ProductNames.Add(idev.GetProductName() + " #-# " + idev.GetSerialNumber());
                    device = idev;
                }
            }
            if (ProductNames.Count != 1)
                device = null;

            return ProductNames;
        }

        public List<string> Select(string partName)
        // ==========================================================================================
        {
            string[] sep = { " #-# " };

            if (partName.Contains(sep[0])) // Preselected name/serial combination
            {
                string[] id = partName.Split(sep, StringSplitOptions.None);
                return new List<string> { Select(id[0], id[1]) };
            }

            HIDdeviceList = list.GetHidDevices().ToArray();
            List<string> ProductNames = new List<String>();
            foreach (HidDevice idev in HIDdeviceList)
            {
                if (idev.GetProductName().Contains(partName))
                {
                    try
                    {
                        ProductNames.Add(idev.GetProductName() + " #-# " + idev.GetSerialNumber());
                        device = idev;
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
            if (ProductNames.Count != 1)
                device = null;

            return ProductNames;
        }

        public string Select(string partName, string serialNumber)
        // ==========================================================================================
        {
            device = null;
            HIDdeviceList = list.GetHidDevices().ToArray();
            foreach (HidDevice idev in HIDdeviceList)
            {
                if (idev.GetProductName().Contains(partName) && idev.GetSerialNumber().Contains(serialNumber))
                {
                    try
                    {
                        device = idev;
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            if (device != null)
                return device.GetProductName();
            else
                return null;
        }

        // ==========================================================================================
        public void PollReports()
        {
            var reportDescriptor = device.GetReportDescriptor();
            foreach (var deviceItem in reportDescriptor.DeviceItems)
            {
                if (device.TryOpen(out HidStream hidStream))
                {
                    hidStream.ReadTimeout = Timeout.Infinite;

                    using (hidStream)
                    {
                        var inputReportBuffer = new byte[device.GetMaxInputReportLength()];
                        var inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
                        var inputParser = deviceItem.CreateDeviceItemInputParser();
                        inputReceiver.Start(hidStream);

                        int startTime = Environment.TickCount;
                        while (true)
                        {
                            if (!inputReceiver.IsRunning) { break; } // Disconnected?

                            Report report; // Periodically check if the receiver has any reports.
                            while (inputReceiver.TryRead(inputReportBuffer, 0, out report))
                            {
                                bool first = true;
                                // Parse the report if possible.
                                // This will return false if (for example) the report applies to a different DeviceItem.
                                if (inputParser.TryParseReport(inputReportBuffer, 0, report))
                                {
                                    while (inputParser.HasChanged || first)
                                    {
                                        first = false;
                                        int changedIndex = inputParser.GetNextChangedIndex();
                                        try
                                        {
                                            DataValue previousDataValue = inputParser.GetPreviousValue(changedIndex);

                                            DataValue dataValue = inputParser.GetValue(changedIndex);
                                            String Type = ((Usage)dataValue.Usages.FirstOrDefault()).ToString();
                                            status _status = new status((double)previousDataValue.GetPhysicalValue(),
                                                    (double)dataValue.GetPhysicalValue());

                                            lock (EventBufferLock)
                                            {
                                                if (Type.Contains("Button"))
                                                {
                                                    int btn = Convert.ToByte(Type.Last()) - 49;
                                                    if (_status.newval == 1)
                                                        CurrentButtons = (byte)(CurrentButtons | (1 << btn));
                                                    else
                                                        CurrentButtons = (byte)(CurrentButtons & ~(1 << btn));
                                                }
                                                try
                                                {
                                                    AxisAndButtons[Type] = _status;
                                                }
                                                catch (Exception e)
                                                {
                                                    AxisAndButtons.Add(Type, _status);
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        { }

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void Start()
        // ==========================================================================================
        {

            if (device == null)
                throw new Exception("No device selected yet: Use the \"Attached \" function");

            if (Poller != null)
                if (Poller.IsAlive)
                    Poller.Abort();

            Poller = new Thread(PollReports);
            Poller.Start();
        }

        public void Stop()
        // ==========================================================================================
        {

            if (device == null)
                throw new Exception("No device selected yet: Use the \"Attached \" function");
            if (Poller != null)
                if (Poller.IsAlive)
                    Poller.Abort();
        }
        // ==========================================================================================
        // ===================================================================================
        public int GetButtons()
        // ===================================================================================
        {
            lock (EventBufferLock)
            {
                return CurrentButtons;
            }
        }
        // ===================================================================================
        // ===================================================================================
        public double GetAxis(int ax)
        // ===================================================================================
        {
            lock (EventBufferLock)
            {
                try
                {
                    string AxisName = "GenericDesktop" + AxisId[ax - 1];
                    return AxisAndButtons[AxisName].newval;
                }
                catch (Exception)
                {
                    return double.NaN;
                }            }
        }
        public List<string> Get_Axis_Names()
        // ===================================================================================
        {
            lock (EventBufferLock)
            {

                try
                {
                    List<string> keyList = new List<string>(AxisAndButtons.Keys);
                    return keyList;
                }
                catch (Exception e)
                {
                    return new List<string>();
                }
            }
        }
        // ===================================================================================
        public Double WaitForDigEvents(Byte AllowedEventLines, int TimeoutMSecs)
        {
            DateTime startTime = DateTime.Now;
            Double ElapsedMs = 0;
            while (true)
            {
                lock (EventBufferLock)
                {
                    if ((CurrentButtons & AllowedEventLines) != 0)
                        break;
                }

                ElapsedMs = ((TimeSpan)(DateTime.Now - startTime)).TotalMilliseconds;
                if (ElapsedMs >= TimeoutMSecs)
                    break;
            }
            return ElapsedMs;
        }
        // ===================================================================================
        private void OnTimeoutEvent(Object source, System.Timers.ElapsedEventArgs e)
        // ===================================================================================
        {
        }







        // ===========================================================================================
        // ===========================================================================================


        // ===========================================================================================
        public void SetAnalogEventStepSize(Byte NumberOfSamplesPerStep)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, SETANALOGEVENTSTEPSIZE, NumberOfSamplesPerStep, 0, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public void SetLines(Byte OutValue)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, SETOUTPUTLINES, OutValue, 0, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public void PulseLines(Byte OutValue, int DurationInMillisecs)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, PULSEOUTPUTLINES, OutValue, (byte)DurationInMillisecs,
                                                                       (byte)(DurationInMillisecs >> 8), 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public void RerouteEventInput(Byte InputLine, Byte OutputBit)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, REROUTEEVENTINPUT, InputLine, OutputBit, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public void CancelEventReroutes(Byte dummy)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, CANCELEVENTREROUTES, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public void RENC_SetUp(int Range, int MinimumValue, int Position, byte InputChange, byte PulseInputDivider)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, SETUPROTARYCONTROLLER, (byte)Range,        (byte)(Range >> 8),
                                                                  (byte)MinimumValue, (byte)(MinimumValue >> 8),
                                                                  (byte)Position,     (byte)(Position >> 8),
                                                                   InputChange, PulseInputDivider, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public void RENC_SetPosition(int Position)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, SETROTARYCONTROLLERPOSITION, (byte)Position, (byte)(Position >> 8),
                                                                        0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        // ===========================================================================================
        public void ConveyEvent2Output(Byte EventLine, Byte OutputLine, Byte InitialBitValue,
                                        Byte Mode, Int16 Duration)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, CONVEYEVENT2OUTPUT, EventLine, OutputLine, InitialBitValue,
                                        Mode, (Byte)Duration, (Byte)(Duration >> 8) , 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public void SetLedColor(Byte RedValue, Byte GreenValue, Byte BlueValue, Byte LedNumber, Byte Mode)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, SETWS2811RGBLEDCOLOR, RedValue, GreenValue, BlueValue, LedNumber, Mode, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public void SendColors(Byte NumberOfLeds, Byte Mode)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, SENDLEDCOLORS, NumberOfLeds, Mode, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }

        // ===========================================================================================
        public void ChangeInputLineStatus(Byte Mode, Byte LineNumber)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                var USBbytes = new Byte[] { 0, SWITCHLINEEVENTDETECTION, Mode, LineNumber, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }









    }


    //internal class ReadHandlerDelegate
    //{
    //    private Action<HidReport> readHandler;

    //    public ReadHandlerDelegate(Action<HidReport> readHandler)
    //    {
    //        this.readHandler = readHandler;
    //    }
    //}
}
