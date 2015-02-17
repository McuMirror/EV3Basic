﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.SmallBasic.Library;
using EV3Communication;

namespace SmallBasicEV3Extension
{
    /// <summary>
    /// Use the built-in speaker of the brick to play tones or sound files.
    /// </summary>
    [SmallBasicType]
    public static class Speaker
    {
        /// <summary>
        /// Stop any currently playing sound or tone.
        /// </summary>
        public static void Stop()
        {
            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x94);       // opSOUND
            c.CONST(0x00);    // CMD: BREAK = 0x00
            EV3Communicator.DirectCommand(c, 0, 0);
        }

        /// <summary>
        /// Start playing a simple tone of defined frequency.
        /// </summary>
        /// <param name="volume">Volume can be 0 - 100</param>
        /// <param name="frequency">Frequency in Hz can be 250 - 10000</param>
        /// <param name="duration">Duration of the tone in milliseconds</param>
        public static void Tone(Primitive volume, Primitive frequency, Primitive duration)
        {
            int vol, frq, dur;
            Int32.TryParse(volume == null ? "" : volume.ToString(), out vol);
            Int32.TryParse(frequency == null ? "" : frequency.ToString(), out frq);
            Int32.TryParse(duration == null ? "" : duration.ToString(), out dur);

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x94);       // opSOUND
            c.CONST(0x01);    // CMD: TONE = 0x01
            c.CONST(vol);
            c.CONST(frq);
            c.CONST(dur);
            EV3Communicator.DirectCommand(c, 0, 0);
        }

        /// <summary>
        /// Start playing a simple tone defined by its text representation.
        /// </summary>
        /// <param name="volume">Volume can be 0 - 100</param>
        /// <param name="note">Text defining a note "C4" to "B7" or halftones like "C#5"</param>
        /// <param name="duration">Duration of the tone in milliseconds</param>
        public static void Note(Primitive volume, Primitive note, Primitive duration)
        {
            int vol, dur;
            Int32.TryParse(volume == null ? "" : volume.ToString(), out vol);
            Int32.TryParse(duration == null ? "" : duration.ToString(), out dur);

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x63);       // opNote_To_Freq
            c.STRING(note==null ? "":note.ToString());
            c.GLOBVAR(0);
            c.OP(0x94);       // opSOUND
            c.CONST(0x01);    // CMD: TONE = 0x01
            c.CONST(vol);
            c.GLOBVAR(0);
            c.CONST(dur);
            EV3Communicator.DirectCommand(c, 2, 0);
        }




        /// <summary>
        /// Start playing a sound from a sound file stored on the brick.
        /// </summary>
        /// <param name="volume">Volume can be 0 - 100</param>
        /// <param name="filename">Name of the sound file</param>
        public static void Play(Primitive volume, Primitive filename)
        {
            int vol;
            Int32.TryParse(volume==null?"":volume.ToString(), out vol);

            String fname = filename == null ? "" : filename.ToString();
            if (fname.Length > 100)
            {
                fname = fname.Substring(0, 100);
            }

            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x94);       // opSOUND
            c.CONST(0x02);    //CMD: PLAY = 0x02
            c.CONST(vol);
            c.STRING(fname);
            EV3Communicator.DirectCommand(c, 0, 0);
        }

        /// <summary>
        /// Check if the speaker is still busy playing a previous sound.
        /// </summary>
        /// <returns>"True", if there is a sound still playing</returns>
        public static Primitive IsBusy()
        {
            ByteCodeBuffer c = new ByteCodeBuffer();
            c.OP(0x95);       // opSound_Test (BUSY)
            c.GLOBVAR(0);
            byte[] reply = EV3Communicator.DirectCommand(c, 1, 0);
            return new Primitive((reply == null || reply[0] == 0) ? "False" : "True");
        }

        /// <summary>
        /// Wait until the current sound has finished playing.
        /// When no sound is playing, this function returns immediately.
        /// </summary>
        public static void Wait()
        {
            ByteCodeBuffer c = new ByteCodeBuffer();
            c.Clear();
            c.OP(0x95);       // opSound_Test (BUSY)
            c.GLOBVAR(0);

            for (; ; )
            {
                byte[] reply = EV3Communicator.DirectCommand(c, 1, 0);
                if (reply == null || reply[0] == 0)
                {
                    break;
                }
                System.Threading.Thread.Sleep(2);
            }
        }

    }
}