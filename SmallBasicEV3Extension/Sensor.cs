﻿/*  EV3-Basic: A basic compiler to target the Lego EV3 brick
    Copyright (C) 2015 Reinhard Grafl

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.SmallBasic.Library;
using EV3Communication;

namespace SmallBasicEV3Extension
{
    /// <summary>
    /// Access sensors that are attached to the brick.
    /// To specify the sensor use the port number which is printed below the socket on the brick (for example 1).
    /// To access sensors of further bricks that are connected via daisy-chaining, use the next higher numbers instead (5 - 8 will access the sensors on the first daisy-chained brick, 9-12 the sensors on the next one and so on).
    /// </summary>
    [SmallBasicType]
    public static class Sensor
    {
        /// <summary>
        /// Get the name and mode of a currently connected sensor. 
        /// This function is mainly intended for diagnostic use because you normally know which sensor is plugged to which port on the model.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <returns>Description text (for example, "TOUCH")</returns>
        public static Primitive GetName(Primitive port)
        {
            int layer;
            int no;
            DecodePort(port, out layer, out no);

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x99);                // INPUT_DEVICE 
            c.CONST(0x15);             // CMD: GET_NAME = 0x15
            c.CONST(layer);
            c.CONST(no);
            c.CONST(32);
            c.GLOBVAR(0);
            byte[] result = EV3RemoteControler.DirectCommand(c, 32, 0);

            if(result==null)
            {
                return new Primitive("");
            }
            else
            {
                return new Primitive(Encoding.ASCII.GetString(result).Replace((char)0,' ').Trim());
            }
        }

        /// <summary>
        /// Get the numerical type identifier of a currently connected sensor.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <returns>Sensor type identifier (for example, 16 for a touch sensor)</returns>
        public static Primitive GetType(Primitive port)
        {
            int layer;
            int no;
            DecodePort(port, out layer, out no);

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x99);                // INPUT_DEVICE 
            c.CONST(0x05);             // CMD: GET_TYPEMODE = 0x05
            c.CONST(layer);
            c.CONST(no);
            c.GLOBVAR(0);
            c.GLOBVAR(1);
            byte[] result = EV3RemoteControler.DirectCommand(c, 2, 0);
            
            if (result==null || result.Length<2)
            {
                return new Primitive(0);
            }
            else
            {
                return new Primitive(result[0]);
            }
        }

        /// <summary>
        /// Get current operation mode of a sensor. 
        /// Many sensors can work in substantially different modes. For example, the color sensor can detect ambient light, reflected light or color. When the sensor is plugged in it will normally start with mode 0, but that can be changed later by the program.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <returns>Current operation mode (0 is always the default mode)</returns>
        public static Primitive GetMode(Primitive port)
        {
            int layer;
            int no;
            DecodePort(port, out layer, out no);

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x99);                // INPUT_DEVICE 
            c.CONST(0x05);             // CMD: GET_TYPEMODE = 0x05
            c.CONST(layer);
            c.CONST(no);
            c.GLOBVAR(0);
            c.GLOBVAR(1);
            byte[] result = EV3RemoteControler.DirectCommand(c, 2, 0);

            if (result == null || result.Length < 2)
            {
                return new Primitive(0);
            }
            else
            {
                return new Primitive(result[1]);
            }
        }
       
        /// <summary>
        /// Switches the mode of a sensor. 
        /// Many sensors can work in different modes giving quite different readings. The meaning of each mode number depends on the specific sensor type. For further info, see the sensor list in the appendix.
        /// Note that a sensor will stay in the selected mode even after the program stops and another (or the same) program is started. To avoid confusion, best practice is to always set the mode of all used sensors at program start. 
        /// This command blocks execution until mode switching is finished and first sensor data is available.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <param name="mode">New mode to switch to. This only succeeds when the mode is indeed supported by the sensor.</param>
        public static void SetMode(Primitive port, Primitive mode)
        {
            Wait(port);  // make sure a previous mode change/init was already finished before attempting switching mode

            int layer;
            int no;
            DecodePort(port, out layer, out no);
            int mod = mode;

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x9A);                // Input_Read
            c.CONST(layer);
            c.CONST(no);
            c.CONST(0);                // 0 = don't change type
            c.CONST(mod);              // set mode
            c.LOCVAR(0);
            EV3RemoteControler.DirectCommand(c, 0, 1);

            Wait(port);  // make sure mode switch is finished
        }

        /// <summary>
        /// Check if a sensor is currently busy switching mode or in process of initialization. After mode switching a sensor may take some time to become ready again.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <returns>"True" if the sensor is currently busy</returns>
        public static Primitive IsBusy(Primitive port)
        {
            int layer;
            int no;
            DecodePort(port, out layer, out no);

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x9B);    // Input_Test
            c.CONST(layer);
            c.CONST(no);
            c.GLOBVAR(0);
            byte[] response = EV3RemoteControler.DirectCommand(c, 1, 0);
            if (response == null || response.Length < 1 || response[0] == 0)
            {
                return new Primitive("False");
            }
            else
            {
                return new Primitive("True");
            }
        }

        /// <summary>
        /// Wait until a sensor has finished its reconfiguration. When no sensor is plugged into the port, this function returns immediately.
        /// Normally you would not need to call this command, because SetMode() blocks until the sensor is ready anyway. It can be useful in special circumstances, like when the mode was switched by a different thread, or when a sensor is plugged into the brick at runtime.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        public static void Wait(Primitive port)
        {
            int layer;
            int no;
            DecodePort(port, out layer, out no);

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x9B);    // Input_Test
            c.CONST(layer);
            c.CONST(no);
            c.GLOBVAR(0);
            for (;;)
            {
                byte[] response = EV3RemoteControler.DirectCommand(c, 1, 0);
                if (response == null || response.Length < 1 || response[0] == 0)
                {
                    return;
                }
                System.Threading.Thread.Sleep(2);
            }
        }


        /// <summary>
        /// Read the current sensor value and apply some sensible percentage scaling.
        /// Most sensors can translate the current reading to a meaningful single percentage value like light intensity or button press state.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <returns>The percentage value (For example, the touch sensor gives 100 for pressed and 0 for non-pressed)</returns>
        public static Primitive ReadPercent(Primitive port)
        {
            int layer;
            int no;
            DecodePort(port, out layer, out no);

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x9A);                // INPUT_READ 
            c.CONST(layer);
            c.CONST(no);
            c.CONST(0);                // 0 = don't change type
            c.CONST(-1);               // -1 = don't change mode
            c.GLOBVAR(0);
            byte[] result = EV3RemoteControler.DirectCommand(c, 1, 0);

            if (result == null || result.Length < 1)
            {
                return new Primitive(0);
            }
            else
            {
                int v = result[0];
                return new Primitive(v>127 ? 0 : v);
            }
        }

        /// <summary>
        /// Read current sensor value where the result from ReadPercent() is not specific enough.
        /// Some sensor modes deliver values that cannot be translated to percentage (for example a color index) or that contain multiple values at once (for example the individual red, green, blue light intensities that make up RGB). 
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <param name="values">Requested size of result array</param>
        /// <returns>An array holding the requested number of values. Index starts at 0. Elements that got no data are set to 0.</returns>
        public static Primitive ReadRaw(Primitive port, Primitive values)
        {
            int layer;
            int no;
            DecodePort(port, out layer, out no);

            int _values = values;
            if (_values<=0)
            {
                return new Primitive();  // no values requested - just return empty object
            }
            if (_values>8)
            { 
                _values = 8;
            }

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x9E);                // Input_ReadExt
            c.CONST(layer);
            c.CONST(no);
            c.CONST(0);                // 0 = don't change type
            c.CONST(-1);               // -1 = don't change mode
            c.CONST(18);               // FORMAT = raw (32bit)
            c.CONST(_values);           // return desired number of 32bit-values
            for (int i = 0; i < _values; i++)
            {
                c.GLOBVAR(4 * i);      // values should be stored in global variables 
            }

            byte[] result = EV3RemoteControler.DirectCommand(c, 4*_values, 0);

            Dictionary<Primitive, Primitive> map = new Dictionary<Primitive, Primitive>();
            for (int i = 0; i < _values; i++)
            {
                double v = 0;
                if (result != null && i*4+3 < result.Length)
                {
                    v = DecodeRaw(result, i * 4);
                }
                map[new Primitive((double)i)] = new Primitive(v<-1000000000 ? 0:v);
            }            
            return Primitive.ConvertFromMap(map);
        }

        /// <summary>
        /// Similar to ReadRaw, but returns only a single raw value instead of an array of raw values.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <param name="index">Index of the value that should be picked from the result array (starting with index 0)</param>
        /// <returns>One element of a raw sensor reading</returns>
        public static Primitive ReadRawValue(Primitive port, Primitive index)
        {
            int layer;
            int no;
            DecodePort(port, out layer, out no);

            int _index = index;
            if (_index < 0 || _index>7)
            {
                return new Primitive(0);  // index out of range - just return 0
            }
            int _values = _index + 1;

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x9E);                // Input_ReadExt
            c.CONST(layer);
            c.CONST(no);
            c.CONST(0);                // 0 = don't change type
            c.CONST(-1);               // -1 = don't change mode
            c.CONST(18);               // FORMAT = raw (32bit)
            c.CONST(_values);           // return desired number of 32bit-values
            for (int i = 0; i < _values; i++)
            {
                c.GLOBVAR(4 * i);      // values should be stored in global variables 
            }

            byte[] result = EV3RemoteControler.DirectCommand(c, 4 * _values, 0);
            if (result==null || result.Length<4*_values)
            {
                return new Primitive(0);
            }

            double v = DecodeRaw(result, _index * 4);
            return new Primitive(v < -1000000000 ? 0 : v);
        }


        /// <summary>
        /// Communicates with devices using the I2C protocol over one of the sensor ports.
        /// This command addresses one device on the I2C-bus and can send and receive multiple bytes. This feature could be used to attach a custom sensor or to communicate with any device that is capable of being connected to the I2C bus as a slave.
        /// Note that this command does not work over daisy-chaining.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <param name="address">Address (0 - 127) of the I2C slave on the I2C bus</param>
        /// <param name="writebytes">Number of bytes to write to the slave (maximum 31)</param>
        /// <param name="readbytes">Number of bytes to request from the slave (maximum 32, minimum 1)</param>
        /// <param name="writedata">Array holding the data bytes to be sent (starting at 0)</param>
        /// <returns>An array holding the requested number of values. Index starts at 0</returns>
        public static Primitive CommunicateI2C(Primitive port, Primitive address, Primitive writebytes, Primitive readbytes, Primitive writedata)
        {
            int layer;
            int no;
            // decode parameters
            DecodePort(port, out layer, out no);
            int addr = address;
            int wrt = writebytes;
            if (wrt<0)
            {
                wrt = 0;
            }
            if (wrt>31)     // can not write more than 31 bytes in one transmission
            {
                wrt = 31;
            }
            int rd = readbytes;
            if (rd<1)       // must read at least one byte to not confuse the firmware
            {
                rd = 1;
            }
            if (rd>32)      // can not read more than 32 bytes
            {
                rd = 32;
            }

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x2F);                // opInit_Bytes
            c.LOCVAR(0);               // prepare sending data from local variable
            c.CONST(1+wrt);            //  number of bytes (including the address)
            c.CONST(addr & 0x7f);      //  first byte: address (from 0 - 127)
            for (int i = 0; i < wrt; i++)  // extract the send bytes from the array
            {
                Primitive v = writedata == 0 ? null : Primitive.GetArrayValue(writedata, new Primitive((double)i));
                int d = v;
                c.CONST(((int)d) & 0xff);       //  optional payload bytes
            }

            c.OP(0x99);                // opInput_Device
            c.CONST(0x09);             // CMD: SETUP = 0x09
            c.CONST(layer);
            c.CONST(no);
            c.CONST(1);                // repeat
            c.CONST(0);                // time
            c.CONST(wrt+1);            // bytes to write (including address)
            c.LOCVAR(0);               // data to write is in local variables, beginning from 0
            c.CONST(rd);                // number of bytes to read (no address)
            c.GLOBVAR(0);              // buffer to read into is global variable, beginning from 0

            byte[] result = EV3RemoteControler.DirectCommand(c, rd, 1 + wrt);

            Dictionary<Primitive, Primitive> map = new Dictionary<Primitive, Primitive>();
            for (int i = 0; i < rd; i++)
            {
                map[new Primitive((double)i)] = new Primitive((result!=null && result.Length==rd) ? result[rd-1-i] : 0);
            }
            return Primitive.ConvertFromMap(map);
        }

        /// <summary>
        /// This command addresses one device on the I2C-bus and tries to receive the value of a single register of a connected I2C slave.
        /// Note that this command does not work over daisy-chaining.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <param name="address">Address (0 - 127) of the I2C slave on the I2C bus</param>
        /// <param name="registernumber">Number of register in the slave to read data from</param>
        /// <returns>The content of the register</returns>
        public static Primitive ReadI2CRegister(Primitive port, Primitive address, Primitive registernumber)
        {
            int layer;
            int no;
            // decode parameters
            DecodePort(port, out layer, out no);
            int addr = address;
            int reg = registernumber;

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x2F);                // opInit_Bytes
            c.LOCVAR(0);               // prepare sending data from local variable
            c.CONST(1 + 1);            // number of bytes: the address and the register number
            c.CONST(addr & 0x7f);      // first byte: address (from 0 - 127)
            c.CONST(reg & 0xff);       // second byte: register number (from 0 - 255)

            c.OP(0x99);                // opInput_Device
            c.CONST(0x09);             // CMD: SETUP = 0x09
            c.CONST(layer);
            c.CONST(no);
            c.CONST(1);                // repeat
            c.CONST(0);                // time
            c.CONST(2);                // bytes to write (address and register number)
            c.LOCVAR(0);               // data to write is in local variables, beginning from 0
            c.CONST(1);                // number of bytes to read (just the one register content)
            c.GLOBVAR(0);              // buffer to read into is global variable, beginning from 0

            byte[] result = EV3RemoteControler.DirectCommand(c, 1, 2);
            return new Primitive((result != null && result.Length == 1) ? result[0] : -1);
        }

        /// <summary>
        /// This command addresses one device on the I2C-bus and tries to receive the values of multiple registers of a connected I2C slave.
        /// Note that this command does not work over daisy-chaining.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <param name="address">Address (0 - 127) of the I2C slave on the I2C bus</param>
        /// <param name="registernumber">Number of the first register in the slave to read data from</param>
        /// <param name="readbytes">How many register to read (maximum 32)</param>
        /// <returns>An array holding the requested number of values. Index starts at 0</returns>
        public static Primitive ReadI2CRegisters(Primitive port, Primitive address, Primitive registernumber, Primitive readbytes)
        {
            int layer;
            int no;
            // decode parameters
            DecodePort(port, out layer, out no);
            int addr = address;
            int reg = registernumber; 
            int rd = readbytes;
            if (rd<1)       // must read at least one byte to not confuse the firmware
            {
                rd = 1;
            }
            if (rd>32)      // can not read more than 32 bytes
            {
                rd = 32;
            }

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x2F);                // opInit_Bytes
            c.LOCVAR(0);               // prepare sending data from local variable
            c.CONST(1 + 1);            // number of bytes: the address and the register number
            c.CONST(addr & 0x7f);      // first byte: address (from 0 - 127)
            c.CONST(reg & 0xff);       // second byte: register number (from 0 - 255)

            c.OP(0x99);                // opInput_Device
            c.CONST(0x09);             // CMD: SETUP = 0x09
            c.CONST(layer);
            c.CONST(no);
            c.CONST(1);                // repeat
            c.CONST(0);                // time
            c.CONST(2);                // bytes to write (address and register number)
            c.LOCVAR(0);               // data to write is in local variables, beginning from 0
            c.CONST(rd);               // number of bytes to read
            c.GLOBVAR(0);              // buffer to read into is global variable, beginning from 0

            byte[] result = EV3RemoteControler.DirectCommand(c, rd, 2);
            Dictionary<Primitive, Primitive> map = new Dictionary<Primitive, Primitive>();
            for (int i = 0; i < rd; i++)
            {
                map[new Primitive((double)i)] = new Primitive((result != null && result.Length == rd) ? result[rd - 1 - i] : 0);
            }
            return Primitive.ConvertFromMap(map);
        }

        /// <summary>
        /// This command addresses one device on the I2C-bus and tries to write the value of a single register of a connected I2C slave.
        /// Note that this command does not work over daisy-chaining.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <param name="address">Address (0 - 127) of the I2C slave on the I2C bus</param>
        /// <param name="registernumber">Number of the register in the slave to write data to</param>
        /// <param name="value">Value to write into the register.</param>
        public static void WriteI2CRegister(Primitive port, Primitive address, Primitive registernumber, Primitive value)
        {
            int layer;
            int no;
            // decode parameters
            DecodePort(port, out layer, out no);
            int addr = address;
            int reg = registernumber;
            int val = value;

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x2F);                // opInit_Bytes
            c.LOCVAR(0);               // prepare sending data from local variable
            c.CONST(4);                // number of bytes: the address and the register number and the value and dummy space for dummy read
            c.CONST(addr & 0x7f);      // first byte: address (from 0 - 127)
            c.CONST(reg & 0xff);       // second byte: register number (from 0 - 255)
            c.CONST(val & 0xff);       // third byte: value to write in register
            c.CONST(0);                // reserve a 4th byte to receive unused response into

            c.OP(0x99);                // opInput_Device
            c.CONST(0x09);             // CMD: SETUP = 0x09
            c.CONST(layer);
            c.CONST(no);
            c.CONST(1);                // repeat
            c.CONST(0);                // time
            c.CONST(3);                // bytes to write (address and register number)
            c.LOCVAR(0);               // data to write is in local variables, beginning from 0
            c.CONST(1);                // number of bytes to read (one byte is mandatory)
            c.LOCVAR(3);               // buffer to read into is local variable, beginning from 3

            EV3RemoteControler.DirectCommand(c, 0, 4);
        }

        /// <summary>
        /// This command addresses one device on the I2C-bus and tries to write the values of multiple registers of a connected I2C slave.
        /// Note that this command does not work over daisy-chaining.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <param name="address">Address (0 - 127) of the I2C slave on the I2C bus</param>
        /// <param name="registernumber">Number of the first register in the slave to write data to</param>
        /// <param name="writebytes">How many bytes to write into the registers (maximum 30)</param>
        /// <param name="writedata">Array holding the data bytes to be written (starting at 0)</param>
        public static void WriteI2CRegisters(Primitive port, Primitive address, Primitive registernumber, Primitive writebytes, Primitive writedata)
        {
            int layer;
            int no;
            // decode parameters
            DecodePort(port, out layer, out no);
            int addr = address;
            int reg = registernumber;
            int wrt = writebytes;
            if (wrt <= 0)
            {
                return;  // not writing anything
            }
            if (wrt > 31)     // can not write more than 31 bytes in one transmission
            {
                wrt = 31;
            }

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x2F);                // opInit_Bytes
            c.LOCVAR(0);               // prepare sending data from local variable
            c.CONST(3+wrt);            // number of bytes: the address and the register number and the payload and space for dummy read
            c.CONST(addr & 0x7f);      // first byte: address (from 0 - 127)
            c.CONST(reg & 0xff);       // second byte: register number (from 0 - 255)
            for (int i = 0; i < wrt; i++)
            {
                Primitive v = writedata == 0 ? null : Primitive.GetArrayValue(writedata, new Primitive((double)i));
                int d = v; 
                c.CONST(d & 0xff);    // values to write in registers
            }
            c.CONST(0);                // reserve one more byte to receive unused response into

            c.OP(0x99);                // opInput_Device
            c.CONST(0x09);             // CMD: SETUP = 0x09
            c.CONST(layer);
            c.CONST(no);
            c.CONST(1);                // repeat
            c.CONST(0);                // time
            c.CONST(2+wrt);            // bytes to write (address and register number and payload)
            c.LOCVAR(0);               // data to write is in local variables, beginning from 0
            c.CONST(1);                // number of bytes to read (one byte is mandatory)
            c.LOCVAR(2+wrt);           // buffer to read into is local variable

            EV3RemoteControler.DirectCommand(c, 0, 3+wrt);
        }


        /// <summary>
        /// Sends data to devices which are attached to the UART (universal asynchronous receiver transmitter) of one of the sensor ports. 
        /// This can be useful to send custom commands to custom made sensors/actuators.
        /// </summary>
        /// <param name="port">Number of the sensor port</param>
        /// <param name="writebytes">Number of bytes to write to the device (maximum 32)</param>
        /// <param name="writedata">Array holding the data bytes to be sent (starting at 0)</param>
        public static void SendUARTData(Primitive port, Primitive writebytes, Primitive writedata)
        {
            int layer;
            int no;
            // decode parameters
            DecodePort(port, out layer, out no);
            int wrt = writebytes;
            if (wrt < 0)
            {
                wrt = 0;
            }
            if (wrt > 32)     // can not write more than 32 bytes in one transmission
            {
                wrt = 32;
            }

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x2F);                // opInit_Bytes
            c.LOCVAR(0);               // prepare sending data from local variable
            c.CONST(wrt);              //  number of bytes
            for (int i = 0; i < wrt; i++)  // extract the send bytes from the array
            {
                Primitive v = writedata == 0 ? null : Primitive.GetArrayValue(writedata, new Primitive((double)i));
                int d = v;
                c.CONST(((int)d) & 0xff);       //  optional payload bytes
            }

            c.OP(0x9F);                // opInput_Write
            c.CONST(layer);
            c.CONST(no);
            c.CONST(wrt);              // bytes to write
            c.LOCVAR(0);               // data to write is in local variables, beginning from 0

            EV3RemoteControler.DirectCommand(c, 0, wrt);
        }



        private static void DecodePort(Primitive port, out int layer, out int no)
        {
            int portnumber = port;
            layer = 0;
            no = 0;
            if (portnumber>=1 && portnumber<=16)
            {
                layer = (portnumber - 1) / 4;
                no = (portnumber - 1) % 4;
            }
        }

        private static int DecodeRaw(byte[] result, int start)
        {
            int b0 = ((int) result[start]) & 0xff;
            int b1 = ((int) result[start+1]) & 0xff;
            int b2 = ((int) result[start+2]) & 0xff;
            int b3 = ((int) result[start+3]) & 0xff;
            return b0 | (b1<<8) | (b2<<16) | (b3<<24);
        }


    }
}
