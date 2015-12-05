﻿
namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        byte[] hddInfo = new byte[] {
	    0x40, 0x00,//0x0040
	    0xFF, 0x3F,//16383,
	    0, 0,
	    16, 0, //16
	    0, 0, 0, 0,
	    63, 0,
	    0, 0, 0, 0, 0, 0,
	    (byte)'A', (byte)'G', // serial
	    (byte)'D', (byte)'V',
	    (byte)'9', (byte)'H',
	    (byte)'D', (byte)'D',
	    (byte)'0', (byte)'0',
	    (byte)'0', (byte)' ',
	    (byte)' ', (byte)' ',
	    (byte)' ', (byte)' ',
	    (byte)' ', (byte)' ',
	    (byte)' ', (byte)' ',
    /*	twochars('F', '1'),
	    twochars('Y', '0'),
	    twochars('7', 'V'),
	    twochars('W', '7'),
	    twochars(' ', ' '),
	    twochars(' ', ' '),
	    twochars(' ', ' '),
	    twochars(' ', ' '),
	    twochars(' ', ' '),
	    twochars(' ', ' '),*/
	    0, 0, 0, 0, 0, 0,
	    (byte)'F', (byte)'I', // firmware
	    (byte)'R', (byte)'M',
	    (byte)'1', (byte)'0',
	    (byte)'0', (byte)' ',
    /*	twochars('Z', '1'),
	    twochars('1', ' '),
	    twochars(' ', ' '),
	    twochars(' ', ' '),*/
        //(byte)'M', (byte)'E', // model
        //(byte)'G', (byte)'A',
        //(byte)'D', (byte)'E',
        //(byte)'V', (byte)'9',
        //(byte)' ', (byte)'H',
        //(byte)'D', (byte)'D',
        //(byte)' ', (byte)'V',
        //(byte)'E', (byte)'R',
        //(byte)' ', (byte)'1',
        //(byte)'.', (byte)'0',
        //(byte)'.', (byte)'0',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        (byte)'C', (byte)'L', // model
        (byte)'R', (byte)'-',
        (byte)'D', (byte)'E',
        (byte)'V', (byte)'9',
        (byte)' ', (byte)'H',
        (byte)'D', (byte)'D',
        (byte)' ', (byte)'V',
        (byte)'E', (byte)'R',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        (byte)' ', (byte)' ',
        //(byte)'S', (byte)'C',
        //(byte)'P', (byte)'H',
        //(byte)'-', (byte)'2',
        //(byte)'0', (byte)'4',
        //(byte)'0', (byte)'1',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)'.', (byte)' ',
        //(byte)'.', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
        //(byte)' ', (byte)' ',
	    1, 0,
	    0, 0,
	    0x00, 0x03,
	    0, 0,
	    0x00, 0x02,
	    0x00, 0x02,
	    0, 0,
	    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
	    0, 0,
	    0x00, 0x00, 0x00, 0x10		// Sector number (default : 8 GB, changed on config load)
						    // After that... nothing interesting, only zeroes
        };
    }
}
