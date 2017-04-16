// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.System.Threading;
using Windows.Devices.Gpio;
using RaspiMidiUwp.Classes;
using System.Collections.Generic;

namespace RaspiMidiUwp
{
    public sealed partial class MainPage : Page
    {
        private const int BAUD_RATE = 115200;
        private const ushort PIN_MASK = 65534;
        private const uint MIDI_NOTE_ON = 144;
        private const uint MIDI_NOTE_OFF = 128;
        private Queue<MidiMessage> msgQueue;
        private DispatcherTimer timer;

        /// <summary>
        /// Private variables
        /// </summary>
        private SerialDevice serialPort = null;

        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;
        private DeviceInformation _defaultDevice;

        private GpioController _gpio;

        public MainPage()
        {
            this.InitializeComponent();
            comPortInput.IsEnabled = false;
            sendNoteButton.IsEnabled = false;
            listOfDevices = new ObservableCollection<DeviceInformation>();

            SetupPage();
        }

        private async void SetupPage()
        {
            string aqs = SerialDevice.GetDeviceSelector();
            var dis = await DeviceInformation.FindAllAsync(aqs);

            _defaultDevice = dis[0];

            // Disable the 'Connect' button 
            comPortInput.IsEnabled = false;

            try
            {
                await ConnectToCOMPort();

                _gpio = GpioController.GetDefault();
                var pin4 = PrepareInputPin(4);
                var pin18 = PrepareInputPin(18);
                var pin17 = PrepareInputPin(17);


                msgQueue = new Queue<MidiMessage>();

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(10);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendNoteButton.IsEnabled = false;
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            timer.Stop();

            if (msgQueue.Count > 0)
            {
                var msg = msgQueue.Dequeue();
                if (msg != null)
                {
                    SendNote(msg.Note, msg.Velocity, msg.MsgChannel);
                }
            }

            timer.Start();
        }

        private GpioPin PrepareInputPin(int pinNumber)
        {
            var pin = _gpio.OpenPin(pinNumber);
            pin.DebounceTimeout = TimeSpan.FromTicks(0);
            pin.ValueChanged += Pin_ValueChanged;

            return pin;
        }

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {

            uint msgChannel = args.Edge == GpioPinEdge.RisingEdge ? MIDI_NOTE_ON : MIDI_NOTE_OFF; // note on for channel 0 (MIDI #1)
            uint velocity = 100;
            uint note;

            switch (sender.PinNumber)
            {
                case 4:
                    note = 36;
                    break;
                case 17:
                    note = 42;
                    break;
                case 18:
                    note = 38;
                    break;
                default:
                    note = 60; // middle C
                    break;
            }

            //SendNote(note, velocity, msgChannel);
            MidiMessage msg = new MidiMessage
            {
                Note = note,
                Velocity = velocity,
                MsgChannel = msgChannel
            };

            msgQueue.Enqueue(msg);

            Debug.WriteLine(string.Format("Pin {0}: {1}", sender, args.Edge));
        }

        /// <summary>
        /// comPortInput_Click: Action to take when 'Connect' button is clicked
        /// - Get the selected device index and use Id to create the SerialDevice object
        /// - Configure default settings for the serial port
        /// - Create the ReadCancellationTokenSource token
        /// - Start listening on the serial port input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void comPortInput_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToCOMPort();
        }

        private async Task ConnectToCOMPort()
        {
            DeviceInformation entry = _defaultDevice;

            serialPort = await SerialDevice.FromIdAsync(_defaultDevice.Id);
            if (serialPort == null) return;

            try
            {
                if (serialPort == null) return;

                // Disable the 'Connect' button 
                comPortInput.IsEnabled = false;

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(100);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = BAUD_RATE;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                // Display configured settings
                status.Text = "Serial port configured successfully: ";
                status.Text += serialPort.BaudRate + "-";
                status.Text += serialPort.DataBits + "-";
                status.Text += serialPort.Parity.ToString() + "-";
                status.Text += serialPort.StopBits;

                // Set the RcvdText field to invoke the TextChanged callback
                // The callback launches an async Read task to wait for data
                //rcvdText.Text = "Waiting for data...";

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                // Enable 'WRITE' button to allow sending data
                sendNoteButton.IsEnabled = true;

                //Listen();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendNoteButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// sendTextButton_Click: Action to take when 'WRITE' button is clicked
        /// - Send a note, velocity, and message/channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendNoteButton_Click(object sender, RoutedEventArgs e)
        {
            SendNote(Convert.ToUInt32(txtNote.Text), Convert.ToUInt32(txtVelocity.Text), Convert.ToUInt32(txtChannel.Text));
        }

        private void SendNote(uint note, uint velocity, uint channel)
        {
            DataWriter dataWriteObject = null;

            try
            {
                if (serialPort != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(serialPort.OutputStream);

                    dataWriteObject.ByteOrder = ByteOrder.BigEndian;
                    dataWriteObject.WriteByte(Convert.ToByte(note));
                    dataWriteObject.WriteByte(Convert.ToByte(velocity));
                    dataWriteObject.WriteByte(Convert.ToByte(channel));

                    dataWriteObject.StoreAsync().AsTask();
                }
                else
                {
                    status.Text = "No default device found";
                }
            }
            catch (Exception ex)
            {
                status.Text = this.Name + ": " + ex.Message;
            }
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {
            timer.Stop();

            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;

            comPortInput.IsEnabled = true;
            sendNoteButton.IsEnabled = false;
            rcvdText.Text = "";
            listOfDevices.Clear();
        }

        /// <summary>
        /// closeDevice_Click: Action to take when 'Disconnect and Refresh List' is clicked on
        /// - Cancel all read operations
        /// - Close and dispose the SerialDevice object
        /// - Enumerate connected devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                status.Text = "";
                CloseDevice();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }
    }
}
