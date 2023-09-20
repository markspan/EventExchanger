//#define usingpython
using HidSharp;
using HidSharp.Reports;
#if usingpython
using Python.Runtime;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace ID
{
    public   class EventExchangerLister
    {
        // ===================================================================================
        // ===================================================================================
        // Constants to define command-codes given to connected EventExchanger via USB port.                                     
        // ===================================================================================
        // ===================================================================================
        private   readonly object EventBufferLock = new object();
        private const byte CLEAROUTPUTPORT = 0;   // 0x00
        private const byte SETOUTPUTPORT = 1;   // 0x01
        private const byte SETOUTPUTLINES = 2;   // 0x02
        private const byte SETOUTPUTLINE = 3;   // 0x03
        private const byte PULSEOUTPUTLINES = 4;   // 0x04
        private const byte PULSEOUTPUTLINE = 5;   // 0x05

        private const byte SENDLASTOUTPUTBYTE = 10;   // 0x0A

        private const byte CONVEYEVENT2OUTPUT = 20;   // 0x14
        private const byte CONVEYEVENT2OUTPUTEX = 21;   // 0x15
        private const byte CANCELCONVEYEVENT2OUTPUT = 22;   // 0x16

        private const byte CANCELEVENTREROUTES = 30;   // 0x1E
        private const byte REROUTEEVENTINPUT = 31;   // 0x1F

        private const byte SETUPROTARYCONTROLLER = 40;        // 0x28
        private const byte SETROTARYCONTROLLERPOSITION = 41;  // 0x29

        private const byte CONFIGUREDEBOUNCE = 50;   // 0x32

        private const byte SETWS2811RGBLEDCOLOR = 60;  // 0x3C
        private const byte SENDLEDCOLORS = 61;          // 0x3D

        private const byte SWITCHALLLINESEVENTDETECTION = 100;   // 0x64
        private const byte SWITCHLINEEVENTDETECTION = 101;   // 0x65

        private const byte SETANALOGINPUTDETECTION = 102;   // 0x66
        private const byte REROUTEANALOGINPUT = 103;            // 0X67
        private const byte SETANALOGEVENTSTEPSIZE = 104;     // 0X68

        private const byte SWITCHDIAGNOSTICMODE = 200;   // 0xC8
        private const byte SWITCHEVENTTEST = 201;   // 0xC9

        private   byte CurrentButtons = 0;
        private   int lastbtn;
        private   readonly DeviceList list = DeviceList.Local;

        private struct status
        {
            public status(double _oldVal, double _newVal)
            {
                oldval = _oldVal;
                newval = _newVal;
            }

            public double oldval { get; set; }
            public double newval { get; set; }

            public override string ToString() => $"({oldval}, {newval})";
#if usingpython
            public PyTuple ToTuple()
            {
                PyObject[] a = new PyObject[] { new PyFloat(oldval), new PyFloat(newval) };
                return new PyTuple(a);
            }
#endif
        }

        private struct EventTime
        {
            public EventTime(int _btn, double _rt)
            {
                btn = _btn;
                rt = _rt;
            }

            public int btn { get; set; }
            public double rt { get; set; }

            public override string ToString() => $"{btn} :: {rt}";
#if usingpython
            public PyTuple ToTuple()
            {
                PyObject[] a = new PyObject[] { new PyInt(btn), new PyFloat(rt) };
                return new PyTuple(a);
            }
#endif
        }

        private   readonly Dictionary<string, status> AxisAndButtons =
             new Dictionary<string, status>();
        private   Thread Poller;

        private   IEnumerable<HidDevice> HIDdeviceList;
        private   HidDevice device = null;
        private const string _Version = "0.99x  ";
        private   readonly char[] AxisId = new char[] { 'X', 'Y', 'Z', 'A', 'B' };

        // ===========================================================================================
        public   string Version()
        // ===========================================================================================
        {
            return _Version;
        }
        // ===========================================================================================
        // ===========================================================================================
        public   string DeviceName()
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
        public   string GetSerialNumber()
        {
            if (device != null)
                return device.GetSerialNumber();
            else
                return "EventExchanger.dll -> Device info is not accessible, device is not yet loaded ...";
        }
        public   List<string> GetProductNames()
        // ===========================================================================================
        {
            HIDdeviceList = list.GetHidDevices().ToArray();
            List<string> ProductNames = new List<string>();
            foreach (HidDevice idev in HIDdeviceList)
            {
                try
                {
                    if (idev.GetProductName().Contains("EventExchanger"))
                    {
                        ProductNames.Add(idev.GetProductName() + " SN## " + idev.GetSerialNumber());
                        device = idev;
                    }
                }
                catch (Exception)
                { }
            }
            if (ProductNames.Count != 1)
                device = null;

            return ProductNames;
        }
        // ==========================================================================================
        public   List<string> Attached()
        // ==========================================================================================
        {
            HIDdeviceList = list.GetHidDevices().ToArray();

            List<string> ProductNames = new List<string>();
            foreach (HidDevice idev in HIDdeviceList)
            {
                try
                {
                    if (idev.GetProductName().Contains("EventExchanger"))
                    {
                        ProductNames.Add(idev.GetProductName() + " SN## " + idev.GetSerialNumber());
                        device = idev;
                    }
                }
                catch (Exception)
                {
                }
            }
            if (ProductNames.Count != 1)
                device = null;

            return ProductNames;
        }

        public   List<string> Select(string partName)
        // ==========================================================================================
        {
            string[] sep = { " SN## " };

            if (partName.Contains(sep[0])) // Preselected name/serial combination
            {
                string[] id = partName.Split(sep, StringSplitOptions.None);
                return new List<string> { Select(id[0], id[1]) };
            }

            HIDdeviceList = list.GetHidDevices().ToArray();
            List<string> ProductNames = new List<string>();
            foreach (HidDevice idev in HIDdeviceList)
            {
                try
                {
                    if (idev.GetProductName().Contains(partName))
                    {
                        ProductNames.Add(idev.GetProductName() + " SN## " + idev.GetSerialNumber());
                        device = idev;
                    }
                }
                catch (Exception)
                {
                }
            }
            if (ProductNames.Count != 1)
                device = null;

            return ProductNames;
        }

        public   string Selected()
        {
            if (device == null) return "None";
            return device.GetProductName();
        }
        public   string Select(string partName, string serialNumber)
        // ==========================================================================================
        {
            device = null;
            HIDdeviceList = list.GetHidDevices().ToArray();
            foreach (HidDevice idev in HIDdeviceList)
            {
                try
                {
                    if (idev.GetProductName().Contains(partName) && idev.GetSerialNumber().Contains(serialNumber))
                    {
                        device = idev;
                    }
                }
                catch (Exception)
                {
                }
            }

            if (device != null)
                return device.GetProductName();
            else
                return null;
        }

        // ==========================================================================================
        public   void PollReports()
        {
            ReportDescriptor reportDescriptor = device.GetReportDescriptor();
            foreach (DeviceItem deviceItem in reportDescriptor.DeviceItems)
            {
                if (device.TryOpen(out HidStream hidStream))
                {
                    hidStream.ReadTimeout = Timeout.Infinite;

                    using (hidStream)
                    {
                        byte[] inputReportBuffer = new byte[device.GetMaxInputReportLength()];
                        HidSharp.Reports.Input.HidDeviceInputReceiver inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
                        HidSharp.Reports.Input.DeviceItemInputParser inputParser = deviceItem.CreateDeviceItemInputParser();
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
                                            string Type = ((Usage)dataValue.Usages.FirstOrDefault()).ToString();
                                            status _status = new status((double)previousDataValue.GetPhysicalValue(),
                                                    (double)dataValue.GetPhysicalValue());

                                            lock (EventBufferLock)
                                            {
                                                if (Type.Contains("Button"))
                                                {
                                                    int btn = Convert.ToByte(Type.Last()) - 49;
                                                    if (_status.newval == 1)
                                                    {
                                                        CurrentButtons = (byte)(CurrentButtons | (1 << btn));
                                                        lastbtn = btn;
                                                    }
                                                    else
                                                    {
                                                        CurrentButtons = (byte)(CurrentButtons & ~(1 << btn));
                                                    }
                                                }
                                                try
                                                {
                                                    AxisAndButtons[Type] = _status;
                                                }
                                                catch (Exception)
                                                {
                                                    AxisAndButtons.Add(Type, _status);
                                                }
                                            }
                                        }
                                        catch (Exception)
                                        { }

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public   void Start()
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

        public   void Stop()
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
        public   int GetButtons()
        // ===================================================================================
        {
            lock (EventBufferLock)
            {
                return CurrentButtons;
            }
        }
        // ===================================================================================
        // ===================================================================================
        public   double GetAxis(int ax)
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
                }
            }
        }

        public   List<string> Get_Axis_Names()
        // ===================================================================================
        {
            lock (EventBufferLock)
            {

                try
                {
                    List<string> keyList = new List<string>(AxisAndButtons.Keys);
                    return keyList;
                }
                catch (Exception)
                {
                    return new List<string>();
                }
            }
        }
        // ===================================================================================

#if usingpython
        public   PyTuple WaitForDigEvents(byte AllowedEventLines, int TimeoutMSecs)
#else
        public   string WaitForDigEvents(byte AllowedEventLines, int TimeoutMSecs)
#endif
        {
            AxisAndButtons.Clear();
            CurrentButtons = 0;
            Start();
            DateTime startTime = DateTime.Now;
            double ElapsedMs = 0;
            while (true)
            {
                lock (EventBufferLock)
                {
                    if ((CurrentButtons & AllowedEventLines) != 0)
                        break;
                }

                ElapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
                if (TimeoutMSecs != -1)
                    if (ElapsedMs >= TimeoutMSecs)
                    {
                        lastbtn = -1;
                        break;
                    }
            }
            EventTime Retval = new EventTime(lastbtn, ElapsedMs);
            Stop();
#if usingpython
            return Retval.ToTuple();
#else
            return Retval.ToString();
#endif
        }
        // ===================================================================================
        private   void OnTimeoutEvent(object source, System.Timers.ElapsedEventArgs e)
        // ===================================================================================
        {
        }
        // ===========================================================================================
        // ===========================================================================================


        // ===========================================================================================
        public   void SetAnalogEventStepSize(byte NumberOfSamplesPerStep)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, SETANALOGEVENTSTEPSIZE, NumberOfSamplesPerStep, 0, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public   void SetLines(byte OutValue)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, SETOUTPUTLINES, OutValue, 0, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public   void PulseLines(byte OutValue, int DurationInMillisecs)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, PULSEOUTPUTLINES, OutValue, (byte)DurationInMillisecs,
                                                                       (byte)(DurationInMillisecs >> 8), 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public   void RerouteEventInput(byte InputLine, byte OutputBit)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, REROUTEEVENTINPUT, InputLine, OutputBit, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public   void CancelEventReroutes(byte dummy)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, CANCELEVENTREROUTES, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public   void RENC_SetUp(int Range, int MinimumValue, int Position, byte InputChange, byte PulseInputDivider)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, SETUPROTARYCONTROLLER, (byte)Range,        (byte)(Range >> 8),
                                                                  (byte)MinimumValue, (byte)(MinimumValue >> 8),
                                                                  (byte)Position,     (byte)(Position >> 8),
                                                                   InputChange, PulseInputDivider, 0 };
                Console.WriteLine("SetUp " + BitConverter.ToString(USBbytes));
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public   void RENC_SetPosition(int Position)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, SETROTARYCONTROLLERPOSITION, (byte)Position, (byte)(Position >> 8),
                                                                        0, 0, 0, 0, 0, 0, 0 };
                Console.WriteLine("SetPosition " + BitConverter.ToString(USBbytes));
                hidStream.Write(USBbytes);
                Console.WriteLine(hidStream.CanWrite);
            }
        }
        // ===========================================================================================
        // ===========================================================================================
        public   void ConveyEvent2Output(byte EventLine, byte OutputLine, byte InitialBitValue,
                                        byte Mode, short Duration)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, CONVEYEVENT2OUTPUT, EventLine, OutputLine, InitialBitValue,
                                        Mode, (byte)Duration, (byte)(Duration >> 8) , 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public   void SetLedColor(byte RedValue, byte GreenValue, byte BlueValue, byte LedNumber, byte Mode)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, SETWS2811RGBLEDCOLOR, RedValue, GreenValue, BlueValue, LedNumber, Mode, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
        // ===========================================================================================
        public   void SendColors(byte NumberOfLeds, byte Mode)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, SENDLEDCOLORS, NumberOfLeds, Mode, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }

        // ===========================================================================================
        public   void ChangeInputLineStatus(byte Mode, byte LineNumber)
        // ===========================================================================================
        {
            HidStream hidStream;
            if (device.TryOpen(out hidStream))
            {
                byte[] USBbytes = new byte[] { 0, SWITCHLINEEVENTDETECTION, Mode, LineNumber, 0, 0, 0, 0, 0, 0, 0 };
                hidStream.Write(USBbytes);
            }
        }
    }
}
