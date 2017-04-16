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
using RaspiMidiUwp.Utilities;
using AdafruitClassLibrary;
using RaspiMidiUwp.Classes;
using System.Diagnostics;
using Windows.System.Threading;

namespace RaspiMidiUwp
{
    public sealed partial class MainPage : Page
    {
        private const int BAUD_RATE = 115200;
        private const ushort PIN_MASK = 65534;

        /// <summary>
        /// Private variables
        /// </summary>
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;
        private ThreadPoolTimer timer;

        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;
        private DeviceInformation _defaultDevice;
        Mcp23017 _mcp;

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

                _mcp = new Mcp23017();
                await _mcp.InitMCP23017Async(I2CBase.I2CSpeed.I2C_100kHz);
                timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(25));

            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendNoteButton.IsEnabled = false;
            }
        }

        private ushort _oldState = 65535;

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            ushort newState = _mcp.readGPIOAB();
            bool bass = false;
            bool snare = false;

            if (newState != _oldState)
            {
                if (newState > 0)
                {
                    bass = _mcp.digitalRead(1) == Mcp23017.Level.HIGH;
                    snare = _mcp.digitalRead(2) == Mcp23017.Level.HIGH;


                    Debug.WriteLine("bass: " + bass);
                    Debug.WriteLine("snare: " + snare);

                    if (bass)
                    {
                        SendNote(36, 80, 144);

                    }
                    if (snare)
                    {
                        SendNote(37, 80, 144);
                    }
                }

                Debug.WriteLine(string.Format("{0}:{1}", _oldState, newState));
                _oldState = newState;
            }
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
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
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
                rcvdText.Text = "Waiting for data...";

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
        /// - Create a DataWriter object with the OutputStream of the SerialDevice
        /// - Create an async task that performs the write operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendNoteButton_Click(object sender, RoutedEventArgs e)
        {
            SendNote(Convert.ToUInt32(txtNote.Text), Convert.ToUInt32(txtVelocity.Text), Convert.ToUInt32(txtChannel.Text));

        }

        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        //private async Task WriteAsync()
        //{
        //    Task<UInt32> storeAsyncTask;

        //    byte[] bytes = { };

        //    // loop through and send the byte values of our message
        //    dataWriteObject.ByteOrder = ByteOrder.BigEndian;

        //    dataWriteObject.WriteByte(Convert.ToByte(36)); // note (60 = middle c)
        //    dataWriteObject.WriteByte(Convert.ToByte(127)); // velocity (127 = loudest)
        //    dataWriteObject.WriteByte(Convert.ToByte(144)); // note on channel 0

        //    // Launch an async task to complete the write operation
        //    storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

        //    UInt32 bytesWritten = await storeAsyncTask;

        //    await Task.Delay(TimeSpan.FromSeconds(1));

        //    dataWriteObject.WriteByte(Convert.ToByte(36)); // note (60 = middle c)
        //    dataWriteObject.WriteByte(Convert.ToByte(127)); // velocity (127 = loudest)
        //    dataWriteObject.WriteByte(Convert.ToByte(128)); // note off channel 0
            
        //    storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

        //    bytesWritten += await storeAsyncTask;

        //    if (bytesWritten > 0)
        //    {
        //        //status.Text = sendText.Text + ", ";
        //        status.Text += bytesWritten + " bytes written successfully!";
        //    }
        //}

        private async void SendNote(uint note, uint velocity, uint channel, bool onOrOff = true)
        {
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

                    // Launch an async task synchronously so we don't play one note before another
                    await dataWriteObject.StoreAsync();
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
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (TaskCanceledException tce)
            {
                status.Text = "Reading task was cancelled, closing device and cleaning up";
                CloseDevice();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            using (var childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // Create a task object to wait for data on the serialPort.InputStream
                loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(childCancellationTokenSource.Token);

                // Launch the task and wait
                UInt32 bytesRead = await loadAsyncTask;
                if (bytesRead > 0)
                {
                    rcvdText.Text = string.Empty;

                    //rcvdText.Text = dataReaderObject.ReadString(bytesRead);
                    byte[] bytes = { };
                    var byteOrder = dataReaderObject.ByteOrder;

                    // red them in reverse since it's BigEndian
                    for (uint i = dataReaderObject.UnconsumedBufferLength; i > 0; i--)
                    {
                        rcvdText.Text += " " + dataReaderObject.ReadByte().ToString();
                    }
                    //rcvdText.Text = string.Format("byte count: {0}\r\n", bytes.Length);
                    //foreach(var b in bytes)
                    //{
                    //    rcvdText.Text += Convert.ToString(b);
                    //}

                    status.Text = "bytes read successfully!";
                }
            }
        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {
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
                CancelReadTask();
                CloseDevice();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }
    }
}
