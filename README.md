# raspi-midi-uwp
Using MIDI with UWP and .NET (C#)

This code is based off of the [SerialUART sample](https://github.com/ms-iot/samples/tree/develop/SerialUART/CS) from the [Microsoft IoT samples site](https://github.com/ms-iot/samples).

This project uses the serial port to send a simple MIDI message using decimals (unsigned integers, to be more precise).

It requires a virtual MIDI port. I am using the [LoopBe1](http://www.nerds.de/en/loopbe1.html) virtual port.

It also requires a serial-to-MIDI integration piece. I am using [SpikenzieLabs serial MIDI](http://www.spikenzielabs.com/SpikenzieLabs/Serial_MIDI.html).

To understand what values to use, you'll need to do a little Binary to decimal conversion (you can use the Windows calculator in Programmer Mode to help out).

For instance, a "Note On" message is 1001, but you also have to send the channel in the same byte, so to send a note on to channel 0000 (MIDI channel 1), you would need the decimal equivalent of 10010000 = 144.
To send note off (1000) to channel 0001 (MIDI channel 2), you would need 10000001 = 129.

Notes are a little easier, because we already think of them in terms of decimal, so any valid MIDI note value will work.

The velocity is between 0-127 with 127 being the loudest.


