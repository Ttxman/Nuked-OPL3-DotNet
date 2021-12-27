// SPDX-License-Identifier: LGPL-2.1-or-later

/* Nuked OPL3
 * Copyright (C) 2013-2020 Nuke.YKT
 *
 * This file is part of Nuked OPL3.
 *
 * Nuked OPL3 is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation, either version 2.1
 * of the License, or (at your option) any later version.
 *
 * Nuked OPL3 is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with Nuked OPL3. If not, see <https://www.gnu.org/licenses/>.

 *  Nuked OPL3 emulator.
 *  Thanks:
 *      MAME Development Team(Jarek Burczynski, Tatsuyuki Satoh):
 *          Feedback and Rhythm part calculation information.
 *      forums.submarine.org.uk(carbon14, opl3):
 *          Tremolo and phase generator calculation information.
 *      OPLx decapsulated(Matthew Gambrell, Olli Niemitalo):
 *          OPL2 ROMs.
 *      siliconpr0n.org(John McMaster, digshadow):
 *          YMF262 and VRC VII decaps and die shots.
 *
 * version: 1.8
 */

/*
 * Ported to C# by Andrew J. Moore (bobapplemac)
 * Updated 2021/02/24
 * C# Version: 1.8.1
 * Copyright (C) 2020-2021 Andrew J. Moore
 * 
 * NOTE: This code doesn't always seem to output the same byte-level output that the C version does
 * I'm assuning there's a rounding error somewhere in all of the casting and bit-shifting, but haven't found the exact cause
 * However, it seems to be "close-enough" and I can't tell an audible difference
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuked_OPL3
{
    public class opl3
    {
        #region Constants
        const int OPL_WRITEBUF_SIZE = 1024;
        const int OPL_WRITEBUF_DELAY = 2;

        const int RSM_FRAC = 10;

        // Channel types
        const byte ch_2op = 0;
        const byte ch_4op = 1;
        const byte ch_4op2 = 2;
        const byte ch_drum = 3;

        // Envelope key types
        const byte egk_norm = 0x01;
        const byte egk_drum = 0x02;

        // envelope_gen_num
        const byte envelope_gen_num_attack = 0;
        const byte envelope_gen_num_decay = 1;
        const byte envelope_gen_num_sustain = 2;
        const byte envelope_gen_num_release = 3;

        //
        // logsin table
        //
        static readonly UInt16[] logsinrom = new UInt16[256] {
            0x859, 0x6c3, 0x607, 0x58b, 0x52e, 0x4e4, 0x4a6, 0x471,
            0x443, 0x41a, 0x3f5, 0x3d3, 0x3b5, 0x398, 0x37e, 0x365,
            0x34e, 0x339, 0x324, 0x311, 0x2ff, 0x2ed, 0x2dc, 0x2cd,
            0x2bd, 0x2af, 0x2a0, 0x293, 0x286, 0x279, 0x26d, 0x261,
            0x256, 0x24b, 0x240, 0x236, 0x22c, 0x222, 0x218, 0x20f,
            0x206, 0x1fd, 0x1f5, 0x1ec, 0x1e4, 0x1dc, 0x1d4, 0x1cd,
            0x1c5, 0x1be, 0x1b7, 0x1b0, 0x1a9, 0x1a2, 0x19b, 0x195,
            0x18f, 0x188, 0x182, 0x17c, 0x177, 0x171, 0x16b, 0x166,
            0x160, 0x15b, 0x155, 0x150, 0x14b, 0x146, 0x141, 0x13c,
            0x137, 0x133, 0x12e, 0x129, 0x125, 0x121, 0x11c, 0x118,
            0x114, 0x10f, 0x10b, 0x107, 0x103, 0x0ff, 0x0fb, 0x0f8,
            0x0f4, 0x0f0, 0x0ec, 0x0e9, 0x0e5, 0x0e2, 0x0de, 0x0db,
            0x0d7, 0x0d4, 0x0d1, 0x0cd, 0x0ca, 0x0c7, 0x0c4, 0x0c1,
            0x0be, 0x0bb, 0x0b8, 0x0b5, 0x0b2, 0x0af, 0x0ac, 0x0a9,
            0x0a7, 0x0a4, 0x0a1, 0x09f, 0x09c, 0x099, 0x097, 0x094,
            0x092, 0x08f, 0x08d, 0x08a, 0x088, 0x086, 0x083, 0x081,
            0x07f, 0x07d, 0x07a, 0x078, 0x076, 0x074, 0x072, 0x070,
            0x06e, 0x06c, 0x06a, 0x068, 0x066, 0x064, 0x062, 0x060,
            0x05e, 0x05c, 0x05b, 0x059, 0x057, 0x055, 0x053, 0x052,
            0x050, 0x04e, 0x04d, 0x04b, 0x04a, 0x048, 0x046, 0x045,
            0x043, 0x042, 0x040, 0x03f, 0x03e, 0x03c, 0x03b, 0x039,
            0x038, 0x037, 0x035, 0x034, 0x033, 0x031, 0x030, 0x02f,
            0x02e, 0x02d, 0x02b, 0x02a, 0x029, 0x028, 0x027, 0x026,
            0x025, 0x024, 0x023, 0x022, 0x021, 0x020, 0x01f, 0x01e,
            0x01d, 0x01c, 0x01b, 0x01a, 0x019, 0x018, 0x017, 0x017,
            0x016, 0x015, 0x014, 0x014, 0x013, 0x012, 0x011, 0x011,
            0x010, 0x00f, 0x00f, 0x00e, 0x00d, 0x00d, 0x00c, 0x00c,
            0x00b, 0x00a, 0x00a, 0x009, 0x009, 0x008, 0x008, 0x007,
            0x007, 0x007, 0x006, 0x006, 0x005, 0x005, 0x005, 0x004,
            0x004, 0x004, 0x003, 0x003, 0x003, 0x002, 0x002, 0x002,
            0x002, 0x001, 0x001, 0x001, 0x001, 0x001, 0x001, 0x001,
            0x000, 0x000, 0x000, 0x000, 0x000, 0x000, 0x000, 0x000
        };

        //
        // exp table
        //
        static readonly UInt16[] exprom = new UInt16[256] {
            0x7fa, 0x7f5, 0x7ef, 0x7ea, 0x7e4, 0x7df, 0x7da, 0x7d4,
            0x7cf, 0x7c9, 0x7c4, 0x7bf, 0x7b9, 0x7b4, 0x7ae, 0x7a9,
            0x7a4, 0x79f, 0x799, 0x794, 0x78f, 0x78a, 0x784, 0x77f,
            0x77a, 0x775, 0x770, 0x76a, 0x765, 0x760, 0x75b, 0x756,
            0x751, 0x74c, 0x747, 0x742, 0x73d, 0x738, 0x733, 0x72e,
            0x729, 0x724, 0x71f, 0x71a, 0x715, 0x710, 0x70b, 0x706,
            0x702, 0x6fd, 0x6f8, 0x6f3, 0x6ee, 0x6e9, 0x6e5, 0x6e0,
            0x6db, 0x6d6, 0x6d2, 0x6cd, 0x6c8, 0x6c4, 0x6bf, 0x6ba,
            0x6b5, 0x6b1, 0x6ac, 0x6a8, 0x6a3, 0x69e, 0x69a, 0x695,
            0x691, 0x68c, 0x688, 0x683, 0x67f, 0x67a, 0x676, 0x671,
            0x66d, 0x668, 0x664, 0x65f, 0x65b, 0x657, 0x652, 0x64e,
            0x649, 0x645, 0x641, 0x63c, 0x638, 0x634, 0x630, 0x62b,
            0x627, 0x623, 0x61e, 0x61a, 0x616, 0x612, 0x60e, 0x609,
            0x605, 0x601, 0x5fd, 0x5f9, 0x5f5, 0x5f0, 0x5ec, 0x5e8,
            0x5e4, 0x5e0, 0x5dc, 0x5d8, 0x5d4, 0x5d0, 0x5cc, 0x5c8,
            0x5c4, 0x5c0, 0x5bc, 0x5b8, 0x5b4, 0x5b0, 0x5ac, 0x5a8,
            0x5a4, 0x5a0, 0x59c, 0x599, 0x595, 0x591, 0x58d, 0x589,
            0x585, 0x581, 0x57e, 0x57a, 0x576, 0x572, 0x56f, 0x56b,
            0x567, 0x563, 0x560, 0x55c, 0x558, 0x554, 0x551, 0x54d,
            0x549, 0x546, 0x542, 0x53e, 0x53b, 0x537, 0x534, 0x530,
            0x52c, 0x529, 0x525, 0x522, 0x51e, 0x51b, 0x517, 0x514,
            0x510, 0x50c, 0x509, 0x506, 0x502, 0x4ff, 0x4fb, 0x4f8,
            0x4f4, 0x4f1, 0x4ed, 0x4ea, 0x4e7, 0x4e3, 0x4e0, 0x4dc,
            0x4d9, 0x4d6, 0x4d2, 0x4cf, 0x4cc, 0x4c8, 0x4c5, 0x4c2,
            0x4be, 0x4bb, 0x4b8, 0x4b5, 0x4b1, 0x4ae, 0x4ab, 0x4a8,
            0x4a4, 0x4a1, 0x49e, 0x49b, 0x498, 0x494, 0x491, 0x48e,
            0x48b, 0x488, 0x485, 0x482, 0x47e, 0x47b, 0x478, 0x475,
            0x472, 0x46f, 0x46c, 0x469, 0x466, 0x463, 0x460, 0x45d,
            0x45a, 0x457, 0x454, 0x451, 0x44e, 0x44b, 0x448, 0x445,
            0x442, 0x43f, 0x43c, 0x439, 0x436, 0x433, 0x430, 0x42d,
            0x42a, 0x428, 0x425, 0x422, 0x41f, 0x41c, 0x419, 0x416,
            0x414, 0x411, 0x40e, 0x40b, 0x408, 0x406, 0x403, 0x400
        };

        //
        // freq mult table multiplied by 2
        //
        // 1/2, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 12, 12, 15, 15
        //
        static readonly byte[] mt = new byte[16]
        {
            1, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 20, 24, 24, 30, 30
        };

        //
        // ksl table
        //
        static readonly byte[] kslrom = new byte[16]
        {
            0, 32, 40, 45, 48, 51, 53, 55, 56, 58, 59, 60, 61, 62, 63, 64
        };

        static readonly byte[] kslshift = new byte[4]
        {
            8, 1, 2, 0
        };

        //
        // envelope generator constants
        //
        static readonly byte[][] eg_incstep = new byte[4][]
        {
            new byte[4] { 0, 0, 0, 0 },
            new byte[4] { 1, 0, 0, 0 },
            new byte[4] { 1, 0, 1, 0 },
            new byte[4] { 1, 1, 1, 0 }
        };

        //
        // address decoding
        //
        static readonly sbyte[] ad_slot = new sbyte[0x20]
        {
            0, 1, 2, 3, 4, 5, -1, -1, 6, 7, 8, 9, 10, 11, -1, -1,
            12, 13, 14, 15, 16, 17, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };

        static readonly byte[] ch_slot = new byte[18]
        {
            0, 1, 2, 6, 7, 8, 12, 13, 14, 18, 19, 20, 24, 25, 26, 30, 31, 32
        };
        #endregion

        private opl3_chip opl;
        public int VolumeBoost
        {
            get
            {
                if (opl != null)
                {
                    return opl.volumeboost;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                else if (value > 4)
                {
                    value = 4;
                }
                opl.volumeboost = (byte)value;
            }
        }

        private UInt32 _SampleRate;
        public UInt32 SampleRate
        {
            get
            {
                return _SampleRate;
            }
            private set
            {
                _SampleRate = value;
            }
        }

        public opl3(UInt32 SampleRate = 44100, Int32 VolumeBoost = 0)
        {
            this.SampleRate = SampleRate;
            OPL3_Reset(SampleRate);
            this.VolumeBoost = VolumeBoost;
        }

        public void OPL3_Reset(UInt32 samplerate = 44100)
        {
            this.SampleRate = samplerate;
            OPL3_Reset(ref opl, samplerate);
            VolumeBoost = 0;
        }

        public void OPL3_WriteRegBuffered(UInt16 register, byte value)
        {
            OPL3_WriteRegBuffered(opl, register, value);
        }

        public void OPL3_GenerateStream(Int16[] sndbuffer, UInt32 numsamples)
        {
            OPL3_GenerateStream(opl, sndbuffer, numsamples);
        }

        //
        // Envelope generator
        //
        static Int16 OPL3_EnvelopeCalcExp(UInt32 level)
        {
            if (level > 0x1fff)
            {
                level = 0x1fff;
            }

            return (Int16)((exprom[level & 0xff] << 1) >> (Int32)(level >> 8));
        }

        static Int16 OPL3_EnvelopeCalcSin0(UInt16 phase, UInt16 envelope)
        {
            UInt16 output = 0;
            UInt16 neg = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                neg = 0xffff;
            }
            if ((phase & 0x100) != 0)
            {
                output = logsinrom[(phase & 0xff) ^ 0xff];
            }
            else
            {
                output = logsinrom[phase & 0xff];
            }
            return (Int16)(OPL3_EnvelopeCalcExp((UInt32)(output + (envelope << 3))) ^ neg);
        }

        static Int16 OPL3_EnvelopeCalcSin1(UInt16 phase, UInt16 envelope)
        {
            UInt16 output = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                output = 0x1000;
            }
            else if ((phase & 0x100) != 0)
            {
                output = logsinrom[(phase & 0xff) ^ 0xff];
            }
            else
            {
                output = logsinrom[phase & 0xff];
            }
            return OPL3_EnvelopeCalcExp((UInt32)(output + (envelope << 3)));
        }

        static Int16 OPL3_EnvelopeCalcSin2(UInt16 phase, UInt16 envelope)
        {
            UInt16 output = 0;
            phase &= 0x3ff;
            if ((phase & 0x100) != 0)
            {
                output = logsinrom[(phase & 0xff) ^ 0xff];
            }
            else
            {
                output = logsinrom[phase & 0xff];
            }
            return OPL3_EnvelopeCalcExp((UInt32)(output + (envelope << 3)));
        }

        static Int16 OPL3_EnvelopeCalcSin3(UInt16 phase, UInt16 envelope)
        {
            UInt16 output = 0;
            phase &= 0x3ff;
            if ((phase & 0x100) != 0)
            {
                output = 0x1000;
            }
            else
            {
                output = logsinrom[phase & 0xff];
            }
            return OPL3_EnvelopeCalcExp((UInt32)(output + (envelope << 3)));
        }

        static Int16 OPL3_EnvelopeCalcSin4(UInt16 phase, UInt16 envelope)
        {
            UInt16 output = 0;
            UInt16 neg = 0;
            phase &= 0x3ff;
            if ((phase & 0x300) == 0x100)
            {
                neg = 0xffff;
            }
            if ((phase & 0x200) != 0)
            {
                output = 0x1000;
            }
            else if ((phase & 0x80) != 0)
            {
                output = logsinrom[((phase ^ 0xff) << 1) & 0xff];
            }
            else
            {
                output = logsinrom[(phase << 1) & 0xff];
            }
            return (Int16)(OPL3_EnvelopeCalcExp((UInt32)(output + (envelope << 3))) ^ neg);
        }

        static Int16 OPL3_EnvelopeCalcSin5(UInt16 phase, UInt16 envelope)
        {
            UInt16 output = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                output = 0x1000;
            }
            else if ((phase & 0x80) != 0)
            {
                output = logsinrom[((phase ^ 0xff) << 1) & 0xff];
            }
            else
            {
                output = logsinrom[(phase << 1) & 0xff];
            }
            return OPL3_EnvelopeCalcExp((UInt32)(output + (envelope << 3)));
        }

        static Int16 OPL3_EnvelopeCalcSin6(UInt16 phase, UInt16 envelope)
        {
            UInt16 neg = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                neg = 0xffff;
            }
            return (Int16)(OPL3_EnvelopeCalcExp((UInt32)(envelope << 3)) ^ neg);
        }

        static Int16 OPL3_EnvelopeCalcSin7(UInt16 phase, UInt16 envelope)
        {
            UInt16 output = 0;
            UInt16 neg = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                neg = 0xffff;
                phase = (UInt16)((phase & 0x1ff) ^ 0x1ff);
            }
            output = (UInt16)(phase << 3);
            return (Int16)(OPL3_EnvelopeCalcExp((UInt32)(output + (envelope << 3))) ^ neg);
        }

        static Int16 envelope_sin(byte index, UInt16 phase, UInt16 envelope)
        {
            switch (index)
            {
                case 0:
                    return OPL3_EnvelopeCalcSin0(phase, envelope);
                case 1:
                    return OPL3_EnvelopeCalcSin1(phase, envelope);
                case 2:
                    return OPL3_EnvelopeCalcSin2(phase, envelope);
                case 3:
                    return OPL3_EnvelopeCalcSin3(phase, envelope);
                case 4:
                    return OPL3_EnvelopeCalcSin4(phase, envelope);
                case 5:
                    return OPL3_EnvelopeCalcSin5(phase, envelope);
                case 6:
                    return OPL3_EnvelopeCalcSin6(phase, envelope);
                case 7:
                    return OPL3_EnvelopeCalcSin7(phase, envelope);
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        static void OPL3_EnvelopeUpdateKSL(opl3_slot slot)
        {
            Int16 ksl = (Int16)((kslrom[slot.channel.f_num >> 6] << 2) - ((0x08 - slot.channel.block) << 5));
            if (ksl < 0)
            {
                ksl = 0;
            }
            slot.eg_ksl = (byte)ksl;
        }

        static void OPL3_EnvelopeCalc(opl3_slot slot)
        {
            bool nonzero;
            byte rate;
            byte rate_hi;
            byte rate_lo;
            byte reg_rate = 0;
            byte ks;
            byte eg_shift, shift;
            UInt16 eg_rout;
            Int16 eg_inc;
            byte eg_off;
            bool reset = false;
            slot.eg_out = (UInt16)(slot.eg_rout + (slot.reg_tl << 2) + (slot.eg_ksl >> kslshift[slot.reg_ksl]) + slot.trem.Value);
            if ((slot.key != 0) && (slot.eg_gen == envelope_gen_num_release))
            {
                reset = true;
                reg_rate = slot.reg_ar;
            }
            else
            {
                switch (slot.eg_gen)
                {
                    case envelope_gen_num_attack:
                        reg_rate = slot.reg_ar;
                        break;
                    case envelope_gen_num_decay:
                        reg_rate = slot.reg_dr;
                        break;
                    case envelope_gen_num_sustain:
                        if (slot.reg_type == 0)
                        {
                            reg_rate = slot.reg_rr;
                        }
                        break;
                    case envelope_gen_num_release:
                        reg_rate = slot.reg_rr;
                        break;
                }
            }
            slot.pg_reset = reset;
            ks = (byte)(slot.channel.ksv >> ((slot.reg_ksr ^ 1) << 1));
            nonzero = (reg_rate != 0);
            rate = (byte)(ks + (reg_rate << 2));
            rate_hi = (byte)(rate >> 2);
            rate_lo = (byte)(rate & 0x03);
            if ((rate_hi & 0x10) != 0)
            {
                rate_hi = 0x0f;
            }
            eg_shift = (byte)(rate_hi + slot.chip.eg_add);
            shift = 0;
            if (nonzero)
            {
                if (rate_hi < 12)
                {
                    if (slot.chip.eg_state != 0)
                    {
                        switch (eg_shift)
                        {
                            case 12:
                                shift = 1;
                                break;
                            case 13:
                                shift = (byte)((rate_lo >> 1) & 0x01);
                                break;
                            case 14:
                                shift = (byte)(rate_lo & 0x01);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    shift = (byte)((rate_hi & 0x03) + eg_incstep[rate_lo][slot.chip.timer & 0x03]);
                    if ((shift & 0x04) != 0)
                    {
                        shift = 0x03;
                    }
                    if (shift == 0)
                    {
                        shift = slot.chip.eg_state;
                    }
                }
            }
            eg_rout = (UInt16)slot.eg_rout;
            eg_inc = 0;
            eg_off = 0;
            // Instant attack
            if (reset && (rate_hi == 0x0f))
            {
                eg_rout = 0x00;
            }
            // Envelope off
            if ((slot.eg_rout & 0x1f8) == 0x1f8)
            {
                eg_off = 1;
            }
            if ((slot.eg_gen != envelope_gen_num_attack) && !reset && (eg_off != 0))
            {
                eg_rout = 0x1ff;
            }
            switch (slot.eg_gen)
            {
                case envelope_gen_num_attack:
                    if (slot.eg_rout == 0)
                    {
                        slot.eg_gen = envelope_gen_num_decay;
                    }
                    else if ((slot.key != 0) && (shift > 0) && (rate_hi != 0x0f))
                    {
                        //eg_inc = (Int16)(((~slot.eg_rout) << shift) >> 4);
                        eg_inc = (Int16)(~slot.eg_rout >> (4 - shift));
                    }
                    break;
                case envelope_gen_num_decay:
                    if ((slot.eg_rout >> 4) == slot.reg_sl)
                    {
                        slot.eg_gen = envelope_gen_num_sustain;
                    }
                    else if ((eg_off == 0) && !reset && (shift > 0))
                    {
                        eg_inc = (Int16)(1 << (shift - 1));
                    }
                    break;
                case envelope_gen_num_sustain:
                case envelope_gen_num_release:
                    if ((eg_off == 0) && !reset && shift > 0)
                    {
                        eg_inc = (Int16)(1 << (shift - 1));
                    }
                    break;
            }
            slot.eg_rout = (UInt16)((eg_rout + eg_inc) & 0x1ff);
            // Key off
            if (reset)
            {
                slot.eg_gen = envelope_gen_num_attack;
            }
            if (slot.key == 0)
            {
                slot.eg_gen = envelope_gen_num_release;
            }
        }

        static void OPL3_EnvelopeKeyOn(opl3_slot slot, byte type)
        {
            //slot.key |= type;
            slot.key = (byte)(slot.key | type);
        }

        static void OPL3_EnvelopeKeyOff(opl3_slot slot, byte type)
        {
            //slot.key &= ~type;
            slot.key = (byte)(slot.key & ~type);
        }

        //
        // Phase Generator
        //
        static void OPL3_PhaseGenerate(opl3_slot slot)
        {
            opl3_chip chip;
            UInt16 f_num;
            UInt32 basefreq;
            byte rm_xor, n_bit;
            UInt32 noise;
            UInt16 phase;

            chip = slot.chip;
            f_num = slot.channel.f_num;
            if (slot.reg_vib != 0)
            {
                sbyte range;
                byte vibpos;

                range = (sbyte)((f_num >> 7) & 7);
                vibpos = slot.chip.vibpos;

                if ((vibpos & 3) == 0)
                {
                    range = 0;
                }
                else if ((vibpos & 1) != 0)
                {
                    range >>= 1;
                }
                range >>= slot.chip.vibshift;

                if ((vibpos & 4) != 0)
                {
                    range = (sbyte)(-range);
                }
                f_num += (UInt16)range;
            }
            basefreq = (UInt32)((f_num << slot.channel.block) >> 1);
            phase = (UInt16)(slot.pg_phase >> 9);
            if (slot.pg_reset)
            {
                slot.pg_phase = 0;
            }
            slot.pg_phase += (basefreq * mt[slot.reg_mult]) >> 1;
            // Rhythm mode
            noise = chip.noise;
            slot.pg_phase_out = phase;
            if (slot.slot_num == 13) // hh
            {
                chip.rm_hh_bit2 = (byte)((phase >> 2) & 1);
                chip.rm_hh_bit3 = (byte)((phase >> 3) & 1);
                chip.rm_hh_bit7 = (byte)((phase >> 7) & 1);
                chip.rm_hh_bit8 = (byte)((phase >> 8) & 1);
            }
            if (slot.slot_num == 17 && ((chip.rhy & 0x20) != 0)) // tc
            {
                chip.rm_tc_bit3 = (byte)((phase >> 3) & 1);
                chip.rm_tc_bit5 = (byte)((phase >> 5) & 1);
            }
            if ((chip.rhy & 0x20) != 0)
            {
                rm_xor = (byte)((chip.rm_hh_bit2 ^ chip.rm_hh_bit7) | (chip.rm_hh_bit3 ^ chip.rm_tc_bit5) | (chip.rm_tc_bit3 ^ chip.rm_tc_bit5));
                switch (slot.slot_num)
                {
                    case 13: // hh
                        slot.pg_phase_out = (UInt16)(rm_xor << 9);
                        if ((rm_xor ^ (noise & 1)) != 0)
                        {
                            slot.pg_phase_out |= 0xd0;
                        }
                        else
                        {
                            slot.pg_phase_out |= 0x34;
                        }
                        break;
                    case 16: // sd
                        slot.pg_phase_out = (UInt16)(((Int32)chip.rm_hh_bit8 << 9) | ((Int32)(chip.rm_hh_bit8 ^ (noise & 1)) << 8));
                        break;
                    case 17: // tc
                        slot.pg_phase_out = (UInt16)(((Int32)rm_xor << 9) | 0x80);
                        break;
                    default:
                        break;
                }
            }
            n_bit = (byte)(((noise >> 14) ^ noise) & 0x01);
            chip.noise = (UInt32)((noise >> 1) | ((UInt32)n_bit << 22));
        }

        //
        // Slot
        //
        static void OPL3_SlotWrite20(opl3_slot slot, byte data)
        {
            if (((data >> 7) & 0x01) != 0)
            {
                slot.trem = slot.chip.tremolo;
            }
            else
            {
                //slot.trem = (byte)slot.chip.zeromod;
                slot.trem = slot.chip.zerotrem;
            }
            slot.reg_vib = (byte)((data >> 6) & 0x01);
            slot.reg_type = (byte)((data >> 5) & 0x01);
            slot.reg_ksr = (byte)((data >> 4) & 0x01);
            slot.reg_mult = (byte)(data & 0x0f);
        }

        static void OPL3_SlotWrite40(opl3_slot slot, byte data)
        {
            slot.reg_ksl = (byte)((data >> 6) & 0x03);
            slot.reg_tl = (byte)(data & 0x3f);
            OPL3_EnvelopeUpdateKSL(slot);
        }

        static void OPL3_SlotWrite60(opl3_slot slot, byte data)
        {
            slot.reg_ar = (byte)((data >> 4) & 0x0f);
            slot.reg_dr = (byte)(data & 0x0f);
        }

        static void OPL3_SlotWrite80(opl3_slot slot, byte data)
        {
            slot.reg_sl = (byte)((data >> 4) & 0x0f);
            if (slot.reg_sl == 0x0f)
            {
                slot.reg_sl = 0x1f;
            }
            slot.reg_rr = (byte)(data & 0x0f);
        }

        static void OPL3_SlotWriteE0(opl3_slot slot, byte data)
        {
            slot.reg_wf = (byte)(data & 0x07);
            if (slot.chip.newm == 0x00)
            {
                slot.reg_wf &= 0x03;
            }
        }

        static void OPL3_SlotGenerate(opl3_slot slot)
        {
            slot.output.Value = envelope_sin(slot.reg_wf, (UInt16)(slot.pg_phase_out + slot.mod.Value), (UInt16)(slot.eg_out));
        }

        static void OPL3_SlotCalcFB(opl3_slot slot)
        {
            if (slot.channel.fb != 0x00)
            {
                slot.fbmod.Value = (Int16)((slot.prout.Value + slot.output.Value) >> (0x09 - slot.channel.fb));
            }
            else
            {
                slot.fbmod.Value = 0;
            }
            slot.prout = slot.output;
        }

        //
        // Channel
        //
        static void OPL3_ChannelUpdateRhythm(opl3_chip chip, byte data)
        {
            opl3_channel channel6;
            opl3_channel channel7;
            opl3_channel channel8;
            byte chnum;

            chip.rhy = (byte)(data & 0x3f);
            if ((chip.rhy & 0x20) != 0)
            {
                channel6 = chip.channel[6];
                channel7 = chip.channel[7];
                channel8 = chip.channel[8];
                channel6.output[0] = channel6.slots[1].output;
                channel6.output[1] = channel6.slots[1].output;
                channel6.output[2] = chip.zeromod;
                channel6.output[3] = chip.zeromod;
                channel7.output[0] = channel7.slots[0].output;
                channel7.output[1] = channel7.slots[0].output;
                channel7.output[2] = channel7.slots[1].output;
                channel7.output[3] = channel7.slots[1].output;
                channel8.output[0] = channel8.slots[0].output;
                channel8.output[1] = channel8.slots[0].output;
                channel8.output[2] = channel8.slots[1].output;
                channel8.output[3] = channel8.slots[1].output;
                for (chnum = 6; chnum < 9; chnum++)
                {
                    chip.channel[chnum].chtype = ch_drum;
                }
                OPL3_ChannelSetupAlg(channel6);
                OPL3_ChannelSetupAlg(channel7);
                OPL3_ChannelSetupAlg(channel8);
                //hh
                if ((chip.rhy & 0x01) != 0)
                {
                    OPL3_EnvelopeKeyOn(channel7.slots[0], egk_drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(channel7.slots[0], egk_drum);
                }
                //tc
                if ((chip.rhy & 0x02) != 0)
                {
                    OPL3_EnvelopeKeyOn(channel8.slots[1], egk_drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(channel8.slots[1], egk_drum);
                }
                //tom
                if ((chip.rhy & 0x04) != 0)
                {
                    OPL3_EnvelopeKeyOn(channel8.slots[0], egk_drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(channel8.slots[0], egk_drum);
                }
                //sd
                if ((chip.rhy & 0x08) != 0)
                {
                    OPL3_EnvelopeKeyOn(channel7.slots[1], egk_drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(channel7.slots[1], egk_drum);
                }
                //bd
                if ((chip.rhy & 0x10) != 0)
                {
                    OPL3_EnvelopeKeyOn(channel6.slots[0], egk_drum);
                    OPL3_EnvelopeKeyOn(channel6.slots[1], egk_drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(channel6.slots[0], egk_drum);
                    OPL3_EnvelopeKeyOff(channel6.slots[1], egk_drum);
                }
            }
            else
            {
                for (chnum = 6; chnum < 9; chnum++)
                {
                    chip.channel[chnum].chtype = ch_2op;
                    OPL3_ChannelSetupAlg(chip.channel[chnum]);
                    OPL3_EnvelopeKeyOff(chip.channel[chnum].slots[0], egk_drum);
                    OPL3_EnvelopeKeyOff(chip.channel[chnum].slots[1], egk_drum);
                }
            }
        }

        static void OPL3_ChannelWriteA0(opl3_channel channel, byte data)
        {
            if ((channel.chip.newm != 0) && (channel.chtype == ch_4op2))
            {
                return;
            }
            channel.f_num = (UInt16)((channel.f_num & 0x300) | data);
            channel.ksv = (byte)((channel.block << 1) | ((channel.f_num >> (0x09 - channel.chip.nts)) & 0x01));
            OPL3_EnvelopeUpdateKSL(channel.slots[0]);
            OPL3_EnvelopeUpdateKSL(channel.slots[1]);
            if ((channel.chip.newm != 0) && (channel.chtype == ch_4op))
            {
                channel.pair.f_num = channel.f_num;
                channel.pair.ksv = channel.ksv;
                OPL3_EnvelopeUpdateKSL(channel.pair.slots[0]);
                OPL3_EnvelopeUpdateKSL(channel.pair.slots[1]);
            }
        }

        static void OPL3_ChannelWriteB0(opl3_channel channel, byte data)
        {
            if ((channel.chip.newm != 0) && (channel.chtype == ch_4op2))
            {
                return;
            }
            channel.f_num = (UInt16)((channel.f_num & 0xff) | ((data & 0x03) << 8));
            channel.block = (byte)((data >> 2) & 0x07);
            channel.ksv = (byte)((channel.block << 1) | ((channel.f_num >> (0x09 - channel.chip.nts)) & 0x01));
            OPL3_EnvelopeUpdateKSL(channel.slots[0]);
            OPL3_EnvelopeUpdateKSL(channel.slots[1]);
            if ((channel.chip.newm != 0) && (channel.chtype == ch_4op))
            {
                channel.pair.f_num = channel.f_num;
                channel.pair.block = channel.block;
                channel.pair.ksv = channel.ksv;
                OPL3_EnvelopeUpdateKSL(channel.pair.slots[0]);
                OPL3_EnvelopeUpdateKSL(channel.pair.slots[1]);
            }
        }

        static void OPL3_ChannelSetupAlg(opl3_channel channel)
        {
            if (channel.chtype == ch_drum)
            {
                if (channel.ch_num == 7 || channel.ch_num == 8)
                {
                    channel.slots[0].mod = channel.chip.zeromod;
                    channel.slots[1].mod = channel.chip.zeromod;
                    return;
                }
                switch (channel.alg & 0x01)
                {
                    case 0x00:
                        channel.slots[0].mod = channel.slots[0].fbmod;
                        channel.slots[1].mod = channel.slots[0].output;
                        break;
                    case 0x01:
                        channel.slots[0].mod = channel.slots[0].fbmod;
                        channel.slots[1].mod = channel.chip.zeromod;
                        break;
                }
                return;
            }
            if ((channel.alg & 0x08) != 0)
            {
                return;
            }
            if ((channel.alg & 0x04) != 0)
            {
                channel.pair.output[0] = channel.chip.zeromod;
                channel.pair.output[1] = channel.chip.zeromod;
                channel.pair.output[2] = channel.chip.zeromod;
                channel.pair.output[3] = channel.chip.zeromod;
                switch (channel.alg & 0x03)
                {
                    case 0x00:
                        channel.pair.slots[0].mod = channel.pair.slots[0].fbmod;
                        channel.pair.slots[1].mod = channel.pair.slots[0].output;
                        channel.slots[0].mod = channel.pair.slots[1].output;
                        channel.slots[1].mod = channel.slots[0].output;
                        channel.output[0] = channel.slots[1].output;
                        channel.output[1] = channel.chip.zeromod;
                        channel.output[2] = channel.chip.zeromod;
                        channel.output[3] = channel.chip.zeromod;
                        break;
                    case 0x01:
                        channel.pair.slots[0].mod = channel.pair.slots[0].fbmod;
                        channel.pair.slots[1].mod = channel.pair.slots[0].output;
                        channel.slots[0].mod = channel.chip.zeromod;
                        channel.slots[1].mod = channel.slots[0].output;
                        channel.output[0] = channel.pair.slots[1].output;
                        channel.output[1] = channel.slots[1].output;
                        channel.output[2] = channel.chip.zeromod;
                        channel.output[3] = channel.chip.zeromod;
                        break;
                    case 0x02:
                        channel.pair.slots[0].mod = channel.pair.slots[0].fbmod;
                        channel.pair.slots[1].mod = channel.chip.zeromod;
                        channel.slots[0].mod = channel.pair.slots[1].output;
                        channel.slots[1].mod = channel.slots[0].output;
                        channel.output[0] = channel.pair.slots[0].output;
                        channel.output[1] = channel.slots[1].output;
                        channel.output[2] = channel.chip.zeromod;
                        channel.output[3] = channel.chip.zeromod;
                        break;
                    case 0x03:
                        channel.pair.slots[0].mod = channel.pair.slots[0].fbmod;
                        channel.pair.slots[1].mod = channel.chip.zeromod;
                        channel.slots[0].mod = channel.pair.slots[1].output;
                        channel.slots[1].mod = channel.chip.zeromod;
                        channel.output[0] = channel.pair.slots[0].output;
                        channel.output[1] = channel.slots[0].output;
                        channel.output[2] = channel.slots[1].output;
                        channel.output[3] = channel.chip.zeromod;
                        break;
                }
            }
            else
            {
                switch (channel.alg & 0x01)
                {
                    case 0x00:
                        channel.slots[0].mod = channel.slots[0].fbmod;
                        channel.slots[1].mod = channel.slots[0].output;
                        channel.output[0] = channel.slots[1].output;
                        channel.output[1] = channel.chip.zeromod;
                        channel.output[2] = channel.chip.zeromod;
                        channel.output[3] = channel.chip.zeromod;
                        break;
                    case 0x01:
                        channel.slots[0].mod = channel.slots[0].fbmod;
                        channel.slots[1].mod = channel.chip.zeromod;
                        channel.output[0] = channel.slots[0].output;
                        channel.output[1] = channel.slots[1].output;
                        channel.output[2] = channel.chip.zeromod;
                        channel.output[3] = channel.chip.zeromod;
                        break;
                }
            }
        }

        static void OPL3_ChannelWriteC0(opl3_channel channel, byte data)
        {
            channel.fb = (byte)((data & 0x0e) >> 1);
            channel.con = (byte)(data & 0x01);
            channel.alg = channel.con;
            if (channel.chip.newm != 0)
            {
                if (channel.chtype == ch_4op)
                {
                    channel.pair.alg = (byte)(0x04 | (channel.con << 1) | (channel.pair.con));
                    channel.alg = 0x08;
                    OPL3_ChannelSetupAlg(channel.pair);
                }
                else if (channel.chtype == ch_4op2)
                {
                    channel.alg = (byte)(0x04 | (channel.pair.con << 1) | (channel.con));
                    channel.pair.alg = 0x08;
                    OPL3_ChannelSetupAlg(channel);
                }
                else
                {
                    OPL3_ChannelSetupAlg(channel);
                }
            }
            else
            {
                OPL3_ChannelSetupAlg(channel);
            }
            if (channel.chip.newm != 0)
            {
                channel.cha = (UInt16)((((data >> 4) & 0x01) != 0) ? ~0 : 0);
                channel.chb = (UInt16)((((data >> 5) & 0x01) != 0) ? ~0 : 0);
            }
            else
            {
                channel.cha = channel.chb = UInt16.MaxValue;
            }
        }

        static void OPL3_ChannelKeyOn(opl3_channel channel)
        {
            if (channel.chip.newm != 0)
            {
                if (channel.chtype == ch_4op)
                {
                    OPL3_EnvelopeKeyOn(channel.slots[0], egk_norm);
                    OPL3_EnvelopeKeyOn(channel.slots[1], egk_norm);
                    OPL3_EnvelopeKeyOn(channel.pair.slots[0], egk_norm);
                    OPL3_EnvelopeKeyOn(channel.pair.slots[1], egk_norm);
                }
                else if (channel.chtype == ch_2op || channel.chtype == ch_drum)
                {
                    OPL3_EnvelopeKeyOn(channel.slots[0], egk_norm);
                    OPL3_EnvelopeKeyOn(channel.slots[1], egk_norm);
                }
            }
            else
            {
                OPL3_EnvelopeKeyOn(channel.slots[0], egk_norm);
                OPL3_EnvelopeKeyOn(channel.slots[1], egk_norm);
            }
        }

        static void OPL3_ChannelKeyOff(opl3_channel channel)
        {
            if (channel.chip.newm != 0)
            {
                if (channel.chtype == ch_4op)
                {
                    OPL3_EnvelopeKeyOff(channel.slots[0], egk_norm);
                    OPL3_EnvelopeKeyOff(channel.slots[1], egk_norm);
                    OPL3_EnvelopeKeyOff(channel.pair.slots[0], egk_norm);
                    OPL3_EnvelopeKeyOff(channel.pair.slots[1], egk_norm);
                }
                else if (channel.chtype == ch_2op || channel.chtype == ch_drum)
                {
                    OPL3_EnvelopeKeyOff(channel.slots[0], egk_norm);
                    OPL3_EnvelopeKeyOff(channel.slots[1], egk_norm);
                }
            }
            else
            {
                OPL3_EnvelopeKeyOff(channel.slots[0], egk_norm);
                OPL3_EnvelopeKeyOff(channel.slots[1], egk_norm);
            }
        }

        static void OPL3_ChannelSet4Op(opl3_chip chip, byte data)
        {
            byte bit;
            byte chnum;
            for (bit = 0; bit < 6; bit++)
            {
                chnum = bit;
                if (bit >= 3)
                {
                    chnum += 9 - 3;
                }
                if (((data >> bit) & 0x01) != 0)
                {
                    chip.channel[chnum].chtype = ch_4op;
                    chip.channel[chnum + 3].chtype = ch_4op2;
                }
                else
                {
                    chip.channel[chnum].chtype = ch_2op;
                    chip.channel[chnum + 3].chtype = ch_2op;
                }
            }
        }

        static Int16 OPL3_ClipSample(Int32 sample, byte volumeboost = 0)
        {
            //volume boost
            sample <<= volumeboost;

            if (sample > 32767)
            {
                sample = 32767;
            }
            else if (sample < -32768)
            {
                sample = -32768;
            }
            return (Int16)sample;
        }

        static void OPL3_Generate(opl3_chip chip, Int16[] buf)
        {
            byte ii;
            byte jj;
            Int16 accm;
            byte shift = 0;

            buf[1] = OPL3_ClipSample(chip.mixbuff[1], chip.volumeboost);

            for (ii = 0; ii < 15; ii++)
            {
                OPL3_SlotCalcFB(chip.slot[ii]);
                OPL3_EnvelopeCalc(chip.slot[ii]);
                OPL3_PhaseGenerate(chip.slot[ii]);
                OPL3_SlotGenerate(chip.slot[ii]);
            }

            chip.mixbuff[0] = 0;
            for (ii = 0; ii < 18; ii++)
            {
                accm = 0;
                for (jj = 0; jj < 4; jj++)
                {
                    accm += chip.channel[ii].output[jj].Value;
                }
                chip.mixbuff[0] += (Int16)(accm & chip.channel[ii].cha);
            }

            for (ii = 15; ii < 18; ii++)
            {
                OPL3_SlotCalcFB(chip.slot[ii]);
                OPL3_EnvelopeCalc(chip.slot[ii]);
                OPL3_PhaseGenerate(chip.slot[ii]);
                OPL3_SlotGenerate(chip.slot[ii]);
            }

            buf[0] = OPL3_ClipSample(chip.mixbuff[0], chip.volumeboost);

            for (ii = 18; ii < 33; ii++)
            {
                OPL3_SlotCalcFB(chip.slot[ii]);
                OPL3_EnvelopeCalc(chip.slot[ii]);
                OPL3_PhaseGenerate(chip.slot[ii]);
                OPL3_SlotGenerate(chip.slot[ii]);
            }

            chip.mixbuff[1] = 0;
            for (ii = 0; ii < 18; ii++)
            {
                accm = 0;
                for (jj = 0; jj < 4; jj++)
                {
                    accm += chip.channel[ii].output[jj].Value;
                }
                chip.mixbuff[1] += (Int16)(accm & chip.channel[ii].chb);
            }

            for (ii = 33; ii < 36; ii++)
            {
                OPL3_SlotCalcFB(chip.slot[ii]);
                OPL3_EnvelopeCalc(chip.slot[ii]);
                OPL3_PhaseGenerate(chip.slot[ii]);
                OPL3_SlotGenerate(chip.slot[ii]);
            }

            if ((chip.timer & 0x3f) == 0x3f)
            {
                chip.tremolopos = (byte)((chip.tremolopos + 1) % 210);
            }
            if (chip.tremolopos < 105)
            {
                chip.tremolo.Value = (byte)(chip.tremolopos >> chip.tremoloshift);
            }
            else
            {
                chip.tremolo.Value = (byte)((210 - chip.tremolopos) >> chip.tremoloshift);
            }

            if ((chip.timer & 0x3ff) == 0x3ff)
            {
                chip.vibpos = (byte)((chip.vibpos + 1) & 7);
            }

            chip.timer++;

            chip.eg_add = 0;
            if (chip.eg_timer != 0)
            {
                while (shift < 36 && ((chip.eg_timer >> shift) & 1) == 0)
                {
                    shift++;
                }
                if (shift > 12)
                {
                    chip.eg_add = 0;
                }
                else
                {
                    chip.eg_add = (byte)(shift + 1);
                }
            }

            if ((chip.eg_timerrem != 0) || (chip.eg_state != 0))
            {
                if (chip.eg_timer == 0xfffffffff)
                {
                    chip.eg_timer = 0;
                    chip.eg_timerrem = 1;
                }
                else
                {
                    chip.eg_timer++;
                    chip.eg_timerrem = 0;
                }
            }

            chip.eg_state ^= 1;

            while (chip.writebuf[chip.writebuf_cur].time <= chip.writebuf_samplecnt)
            {
                if ((chip.writebuf[chip.writebuf_cur].reg & 0x200) == 0)
                {
                    break;
                }
                chip.writebuf[chip.writebuf_cur].reg &= 0x1ff;
                OPL3_WriteReg(chip, chip.writebuf[chip.writebuf_cur].reg,
                              chip.writebuf[chip.writebuf_cur].data);
                chip.writebuf_cur = (chip.writebuf_cur + 1) % OPL_WRITEBUF_SIZE;
            }
            chip.writebuf_samplecnt++;
        }

        static void OPL3_GenerateResampled(opl3_chip chip, Int16[] buf, int index)
        {
            while (chip.samplecnt >= chip.rateratio)
            {
                chip.oldsamples[0] = chip.samples[0];
                chip.oldsamples[1] = chip.samples[1];
                OPL3_Generate(chip, chip.samples);
                chip.samplecnt -= chip.rateratio;
            }
            buf[index] = (Int16)((chip.oldsamples[0] * (chip.rateratio - chip.samplecnt) + chip.samples[0] * chip.samplecnt) / chip.rateratio);
            buf[index + 1] = (Int16)((chip.oldsamples[1] * (chip.rateratio - chip.samplecnt) + chip.samples[1] * chip.samplecnt) / chip.rateratio);
            chip.samplecnt += 1 << RSM_FRAC;
        }

        static void OPL3_Reset(ref opl3_chip chip, UInt32 samplerate)
        {
            byte slotnum;
            byte channum;

            //memset(chip, 0, sizeof(opl3_chip));
            chip = new opl3_chip();
            for (slotnum = 0; slotnum < 36; slotnum++)
            {
                chip.slot[slotnum].chip = chip;
                chip.slot[slotnum].mod = chip.zeromod;
                chip.slot[slotnum].eg_rout = 0x1ff;
                chip.slot[slotnum].eg_out = 0x1ff;
                chip.slot[slotnum].eg_gen = envelope_gen_num_release;
                //chip.slot[slotnum].trem = (byte)chip.zeromod;
                chip.slot[slotnum].trem = chip.zerotrem;
                chip.slot[slotnum].slot_num = slotnum;
            }
            for (channum = 0; channum < 18; channum++)
            {
                chip.channel[channum].slots[0] = chip.slot[ch_slot[channum]];
                chip.channel[channum].slots[1] = chip.slot[ch_slot[channum] + 3];
                chip.slot[ch_slot[channum]].channel = chip.channel[channum];
                chip.slot[ch_slot[channum] + 3].channel = chip.channel[channum];
                if ((channum % 9) < 3)
                {
                    chip.channel[channum].pair = chip.channel[channum + 3];
                }
                else if ((channum % 9) < 6)
                {
                    chip.channel[channum].pair = chip.channel[channum - 3];
                }
                chip.channel[channum].chip = chip;
                chip.channel[channum].output[0] = chip.zeromod;
                chip.channel[channum].output[1] = chip.zeromod;
                chip.channel[channum].output[2] = chip.zeromod;
                chip.channel[channum].output[3] = chip.zeromod;
                chip.channel[channum].chtype = ch_2op;
                chip.channel[channum].cha = 0xffff;
                chip.channel[channum].chb = 0xffff;
                chip.channel[channum].ch_num = channum;
                OPL3_ChannelSetupAlg(chip.channel[channum]);
            }
            chip.noise = 1;
            chip.rateratio = (Int32)((samplerate << RSM_FRAC) / 49716);
            chip.tremoloshift = 4;
            chip.vibshift = 1;
        }

        static void OPL3_WriteReg(opl3_chip chip, UInt16 reg, byte v)
        {
            byte high = (byte)((reg >> 8) & 0x01);
            byte regm = (byte)(reg & 0xff);
            switch (regm & 0xf0)
            {
                case 0x00:
                    if (high != 0)
                    {
                        switch (regm & 0x0f)
                        {
                            case 0x04:
                                OPL3_ChannelSet4Op(chip, v);
                                break;
                            case 0x05:
                                chip.newm = (byte)(v & 0x01);
                                break;
                        }
                    }
                    else
                    {
                        switch (regm & 0x0f)
                        {
                            case 0x08:
                                chip.nts = (byte)((v >> 6) & 0x01);
                                break;
                        }
                    }
                    break;
                case 0x20:
                case 0x30:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWrite20(chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0x40:
                case 0x50:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWrite40(chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0x60:
                case 0x70:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWrite60(chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0x80:
                case 0x90:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWrite80(chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0xe0:
                case 0xf0:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWriteE0(chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0xa0:
                    if ((regm & 0x0f) < 9)
                    {
                        OPL3_ChannelWriteA0(chip.channel[9 * high + (regm & 0x0f)], v);
                    }
                    break;
                case 0xb0:
                    if ((regm == 0xbd) && (high == 0))
                    {
                        chip.tremoloshift = (byte)((((v >> 7) ^ 1) << 1) + 2);
                        chip.vibshift = (byte)(((v >> 6) & 0x01) ^ 1);
                        OPL3_ChannelUpdateRhythm(chip, v);
                    }
                    else if ((regm & 0x0f) < 9)
                    {
                        OPL3_ChannelWriteB0(chip.channel[9 * high + (regm & 0x0f)], v);
                        if ((v & 0x20) != 0)
                        {
                            OPL3_ChannelKeyOn(chip.channel[9 * high + (regm & 0x0f)]);
                        }
                        else
                        {
                            OPL3_ChannelKeyOff(chip.channel[9 * high + (regm & 0x0f)]);
                        }
                    }
                    break;
                case 0xc0:
                    if ((regm & 0x0f) < 9)
                    {
                        OPL3_ChannelWriteC0(chip.channel[9 * high + (regm & 0x0f)], v);
                    }
                    break;
            }
        }

        static void OPL3_WriteRegBuffered(opl3_chip chip, UInt16 reg, byte v)
        {
            UInt64 time1, time2;

            if ((chip.writebuf[chip.writebuf_last].reg & 0x200) != 0)
            {
                OPL3_WriteReg(chip, (UInt16)(chip.writebuf[chip.writebuf_last].reg & 0x1ff), chip.writebuf[chip.writebuf_last].data);

                chip.writebuf_cur = (chip.writebuf_last + 1) % OPL_WRITEBUF_SIZE;
                chip.writebuf_samplecnt = chip.writebuf[chip.writebuf_last].time;
            }

            chip.writebuf[chip.writebuf_last].reg = (UInt16)(reg | 0x200);
            chip.writebuf[chip.writebuf_last].data = v;
            time1 = chip.writebuf_lasttime + OPL_WRITEBUF_DELAY;
            time2 = chip.writebuf_samplecnt;

            if (time1 < time2)
            {
                time1 = time2;
            }

            chip.writebuf[chip.writebuf_last].time = time1;
            chip.writebuf_lasttime = time1;
            chip.writebuf_last = (chip.writebuf_last + 1) % OPL_WRITEBUF_SIZE;
        }

        static void OPL3_GenerateStream(opl3_chip chip, Int16[] sndbuffer, UInt32 numsamples)
        {
            UInt32 i;
            int sndptr = 0;

            for (i = 0; i < numsamples; i++)
            {
                OPL3_GenerateResampled(chip, sndbuffer, sndptr);
                sndptr += 2;
            }
        }

        class opl3_slot
        {
            public opl3_channel channel;
            public opl3_chip chip;
            public Int16Container output = new Int16Container();
            public Int16Container fbmod = new Int16Container();
            public Int16Container mod;
            public Int16Container prout = new Int16Container();
            public UInt16 eg_rout;
            public UInt16 eg_out;
            public byte eg_inc;
            public byte eg_gen;
            public byte eg_rate;
            public byte eg_ksl;
            public ByteContainer trem = new ByteContainer();
            public byte reg_vib;
            public byte reg_type;
            public byte reg_ksr;
            public byte reg_mult;
            public byte reg_ksl;
            public byte reg_tl;
            public byte reg_ar;
            public byte reg_dr;
            public byte reg_sl;
            public byte reg_rr;
            public byte reg_wf;
            public byte key;
            public bool pg_reset;
            public UInt32 pg_phase;
            public UInt16 pg_phase_out;
            public byte slot_num;
        }

        class opl3_channel
        {
            public opl3_slot[] slots = new opl3_slot[2];
            public opl3_channel pair;
            public opl3_chip chip;
            public Int16Container[] output = new Int16Container[4];
            public byte chtype;
            public UInt16 f_num;
            public byte block;
            public byte fb;
            public byte con;
            public byte alg;
            public byte ksv;
            public UInt16 cha, chb;
            public byte ch_num;
        }

        class opl3_writebuf
        {
            public UInt64 time;
            public UInt16 reg;
            public byte data;
        }

        class opl3_chip
        {
            public opl3_chip()
            {
                for (int i = 0; i < channel.Length; ++i)
                {
                    channel[i] = new opl3_channel();
                }

                for (int i = 0; i < slot.Length; ++i)
                {
                    slot[i] = new opl3_slot();
                }

                for (int i = 0; i < writebuf.Length; ++i)
                {
                    writebuf[i] = new opl3_writebuf();
                }
            }

            public opl3_channel[] channel = new opl3_channel[18];
            public opl3_slot[] slot = new opl3_slot[36];
            public UInt16 timer;
            public UInt64 eg_timer;
            public byte eg_timerrem;
            public byte eg_state;
            public byte eg_add;
            public byte newm;
            public byte nts;
            public byte rhy;
            public byte vibpos;
            public byte vibshift;
            public ByteContainer tremolo = new ByteContainer();
            public byte tremolopos;
            public byte tremoloshift;
            public UInt32 noise;
            public Int16Container zeromod = new Int16Container();
            public ByteContainer zerotrem = new ByteContainer();
            public Int32[] mixbuff = new Int32[2];
            public byte rm_hh_bit2;
            public byte rm_hh_bit3;
            public byte rm_hh_bit7;
            public byte rm_hh_bit8;
            public byte rm_tc_bit3;
            public byte rm_tc_bit5;
            //OPL3L
            public Int32 rateratio;
            public Int32 samplecnt;
            public Int16[] oldsamples = new Int16[2];
            public Int16[] samples = new Int16[2];

            public UInt64 writebuf_samplecnt;
            public UInt32 writebuf_cur;
            public UInt32 writebuf_last;
            public UInt64 writebuf_lasttime;
            public opl3_writebuf[] writebuf = new opl3_writebuf[OPL_WRITEBUF_SIZE];

            public byte volumeboost;
        };

        //Replaces pointers from c++ code - Lets us pass value type by reference
        class Int16Container
        {
            public Int16 Value;
        }
        class ByteContainer
        {
            public Byte Value;
        }
    }
}
