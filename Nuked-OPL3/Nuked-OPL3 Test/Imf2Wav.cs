/* Most of the code below came from K1n0_Duk3's IMF to WAV converter
 * Code Ported to C# and modified to use the Nuked-OPL3 emulator by Andrew Moore (bobapplemac)
 */

/*
K1n9_Duk3's IMF to WAV converter - Converts IMF files to WAV.
Copyright (C) 2013-2020 K1n9_Duk3

Based on Wolf4SDL by Moritz "Ripper" Kroll (http://www.chaos-software.de.vu)

The OPL emulator (fmopl.cpp, fmopl.h) is used under the terms of the
MAME license (see license-mame.txt for more details). 

Redistributions of this program may not be sold, nor may they be used 
in a commercial product or activity, unless a different OPL emulator 
is used.

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Nuked_OPL3
{
    static class Imf2Wav
    {
        public const int YM3812_RATE = 3579545;
        public const UInt16 OPL_SAMPLE_BITS = 16;
        public const UInt32 DEF_FREQ = 44100;
        public const int SEEK_SET = 0;

        public static void ConvertImf2Wav(string ImfFilePath, string WavFilePath)
        {
            Console.Write("Converting " + Path.GetFileName(ImfFilePath) + " to WAV...");

            opl3 opl;
            waveheader head = new waveheader();

            UInt32 imf_rate = 560, wav_rate = DEF_FREQ, samples_per_tick;

            imf_rate = 700;

            samples_per_tick = wav_rate / imf_rate;
            //if(YM3812Init(1, YM3812_RATE, wav_rate))
            //{
            //	printf("Unable to create virtual OPL!!\n");
            //	return 1;
            //}
            opl = new opl3(wav_rate, 2);
            opl.OPL3_WriteRegBuffered(1, 0x20);    //set WSE=1

            using (FileStream fin = File.OpenRead(ImfFilePath))
            {
                UInt32 size, cnt = 0, imfsize = 0xFFFFFFFF;
                bool has_notes = false;

                if (IMF_IsChunk(fin))
                {
                    UInt16 size1 = ReadInt16LE(fin);
                    //Console.WriteLine("IMF size is " + size1 + " Bytes.");
                    imfsize = (uint)(size1 >> 2);
                }
                else
                {
                    //Console.WriteLine("IMF size is not set.");
                }
                //Console.WriteLine("IMF rate is " + imf_rate + " Hz.");
                //printf("Channel mask is 0x%X\n", channel_mask);

                Int16[] buffer = new Int16[samples_per_tick * 2];
                //Console.WriteLine("buffer allocated.\n");
                //out = fopen(wavefile, "wb");
                //if (out)
                using (FileStream fout = File.OpenWrite(WavFilePath))
                {
                    UInt16 imfdelay, imfcommand;
                    //printf("%s opened for output.\n", wavefile);
                    //Console.WriteLine("Converting IMF data to PCM data\n");
                    //write dummy wave header:
                    fout.Write(head.ToByteArray(), 0, 46);

                    int totalsamples = 0;

                    //write converted PCM data:
                    while (imfsize > 0)
                    {
                        imfsize--;
                        try
                        {
                            imfcommand = ReadInt16LE(fin);
                            imfdelay = ReadInt16LE(fin);
                        }
                        catch
                        {
                            break;
                        }

                        opl.OPL3_WriteRegBuffered((ushort)(imfcommand & 0xFF), (byte)((imfcommand >> 8) & 0xFF));
                        while (imfdelay-- > 0)
                        {
                            opl.OPL3_GenerateStream(buffer, samples_per_tick);
                            fout.Write(GetBufferBytes(buffer), 0, buffer.Length * sizeof(Int16));
                            fout.Flush();

                            totalsamples += (int)samples_per_tick;
                        }
                        //if (!(cnt++ & 0xff))
                        //{
                        //	printf(".");
                        //	fflush(stdout);
                        //}
                    }
                    //Console.WriteLine("done!");
                    //else
                    //{
                    //	printf("ERROR: could not write %s\n", wavefile);
                    //}
                    //size = ftell(out);
                    size = (uint)fout.Position;

                    //fill header with correct values:
                    head.dSize = size - 46;
                    head.rSize = size - 8;
                    head.fHertz = wav_rate;
                    head.fBlockAlign = (ushort)(head.fChannels * (head.fBits / 8));
                    head.fBytesPerSec = head.fBlockAlign * wav_rate;

                    //write real wave header:
                    fout.Seek(SEEK_SET, SeekOrigin.Begin);
                    fout.Write(head.ToByteArray(), 0, 46);
                    fout.Flush();
                }
                //if (!has_notes)
                //{
                //	printf("The song did not play any notes.\n");
                //	exit(1);
                //}
                //fclose(in);
            }

            //YM3812Shutdown();
            opl.OPL3_Reset(wav_rate);

            Console.WriteLine("done!");

            return;
        }

        public class waveheader
        {
            public waveheader()
            {
                rID = 0x46464952;           //rID = "RIFF"
                rSize = 0;                  //rSize (dummy value)
                wID = 0x45564157;           //wID = "WAVE"
                fID = 0x20746D66;           //fID = "fmt "
                fSize = 18;                 //fSize
                fFormat = 1;                //fFormat
                fChannels = 2;              //fChannels
                fHertz = DEF_FREQ;          //fHertz
                fBytesPerSec = 0;           //fBytesPerSec (dummy value)
                fBlockAlign = 4;            //fBlockAlign
                fBits = OPL_SAMPLE_BITS;   //fBits
                fSpecific = 0;              //fSpecific
                dID = 0x61746164;           //dID = "data"
                dSize = 0;				    //dSize (dummy value)
            }

            public byte[] ToByteArray()
            {
                List<byte> data = new List<byte>();
                data.AddRange(BitConverter.GetBytes(rID));
                data.AddRange(BitConverter.GetBytes(rSize));
                data.AddRange(BitConverter.GetBytes(wID));
                data.AddRange(BitConverter.GetBytes(fID));
                data.AddRange(BitConverter.GetBytes(fSize));
                data.AddRange(BitConverter.GetBytes(fFormat));
                data.AddRange(BitConverter.GetBytes(fChannels));
                data.AddRange(BitConverter.GetBytes(fHertz));
                data.AddRange(BitConverter.GetBytes(fBytesPerSec));
                data.AddRange(BitConverter.GetBytes(fBlockAlign));
                data.AddRange(BitConverter.GetBytes(fBits));
                data.AddRange(BitConverter.GetBytes(fSpecific));
                data.AddRange(BitConverter.GetBytes(dID));
                data.AddRange(BitConverter.GetBytes(dSize));

                return data.ToArray();
            }

            public UInt32 rID;
            public UInt32 rSize;
            public UInt32 wID;
            public UInt32 fID;
            public UInt32 fSize;
            public UInt16 fFormat;
            public UInt16 fChannels;
            public UInt32 fHertz;
            public UInt32 fBytesPerSec;
            public UInt16 fBlockAlign;
            public UInt16 fBits;
            public UInt16 fSpecific;
            public UInt32 dID;
            public UInt32 dSize;
        }

        public static bool IMF_IsChunk(FileStream fin)
        {
            int feof = 0;
            byte[] chunkdata = new byte[sizeof(UInt16)];
            byte[] buffdata = new byte[sizeof(UInt16)];

            UInt16 chunksize;
            UInt16 buff;
            UInt16 i = 42;
            UInt32 sum1 = 0;
            UInt32 sum2 = 0;


            feof = fin.Read(chunkdata, 0, sizeof(UInt16));
            chunksize = BitConverter.ToUInt16(chunkdata, 0);
            while ((feof > 0) && (i > 0))
            {
                feof = fin.Read(buffdata, 0, sizeof(UInt16));
                buff = BitConverter.ToUInt16(buffdata, 0);
                sum1 += buff;
                feof = fin.Read(buffdata, 0, sizeof(UInt16));
                buff = BitConverter.ToUInt16(buffdata, 0);
                sum2 += buff;
                i--;
            }
            fin.Seek(SEEK_SET, SeekOrigin.Begin);
            return (sum1 > sum2);
        }

        public static UInt32 ReadInt32LE(FileStream fin)
        {
            byte[] data = new byte[sizeof(UInt32)];
            fin.Read(data, 0, sizeof(UInt32));
            return BitConverter.ToUInt32(data, 0);
        }

        public static UInt16 ReadInt16LE(FileStream fin)
        {
            byte[] data = new byte[sizeof(UInt16)];
            fin.Read(data, 0, sizeof(UInt16));
            return BitConverter.ToUInt16(data, 0);
        }

        public static Byte ReadByte(FileStream fin)
        {
            return (Byte)fin.ReadByte();
        }

        public static byte[] GetBufferBytes(Int16[] buffer)
        {
            List<byte> data = new List<byte>();
            foreach (Int16 num in buffer)
            {
                data.AddRange(BitConverter.GetBytes(num));
            }
            return data.ToArray();
        }
    }
}
