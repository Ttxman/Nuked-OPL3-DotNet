using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using static Nuked_OPL3.Imf2Wav;

namespace Nuked_OPL3
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateTestWAV();

            //Songs from Wolfenstein 3D Shareware Demo
            foreach (string ImfFile in Directory.GetFiles("..\\..\\IMF", "*.imf"))
            {
                ConvertImf2Wav(ImfFile, Path.ChangeExtension(Path.GetFileName(ImfFile), ".wav"));
            }
        }

        static void CreateTestWAV()
        {
            Console.Write("Creating test.wav...");

            opl3 opl = new opl3();
            opl.OPL3_WriteRegBuffered(0x01, 0x20);    //set WSE=1
            waveheader head = new waveheader();

            Int16[] buffer = new Int16[opl.SampleRate * 2]; //Muliply by 2 since it's stereo so 2 channels
            using (FileStream fout = File.OpenWrite("test.wav"))
            {
                //write dummy wave header:
                fout.Write(head.ToByteArray(), 0, 46);

                //Command OPL to generate sound
                //Commands taken from "Making a Sound" section of http://shipbrook.net/jeff/sb.html
                opl.OPL3_WriteRegBuffered(0x20, 0x01);
                opl.OPL3_WriteRegBuffered(0x40, 0x10);
                opl.OPL3_WriteRegBuffered(0x60, 0xF0);
                opl.OPL3_WriteRegBuffered(0x80, 0x77);
                opl.OPL3_WriteRegBuffered(0xA0, 0x98);
                opl.OPL3_WriteRegBuffered(0x23, 0x01);
                opl.OPL3_WriteRegBuffered(0x43, 0x00);
                opl.OPL3_WriteRegBuffered(0x63, 0xF0);
                opl.OPL3_WriteRegBuffered(0x83, 0x77);
                opl.OPL3_WriteRegBuffered(0xB0, 0x31);
                
                opl.OPL3_GenerateStream(buffer, opl.SampleRate * 1); //generate 1 second worth of samples

                fout.Write(GetBufferBytes(buffer), 0, (buffer.Length * sizeof(Int16)));
                fout.Flush();

                uint size = (uint)fout.Position;

                //fill header with correct values:
                head.dSize = size - 46;
                head.rSize = size - 8;
                head.fHertz = opl.SampleRate;
                head.fBlockAlign = (ushort)(head.fChannels * (head.fBits / 8));
                head.fBytesPerSec = head.fBlockAlign * opl.SampleRate;

                //write real wave header:
                fout.Seek(SEEK_SET, SeekOrigin.Begin);
                fout.Write(head.ToByteArray(), 0, 46);
                fout.Flush();
            }

            opl.OPL3_Reset();

            Console.WriteLine("done!");

            return;
        }

    }
}
