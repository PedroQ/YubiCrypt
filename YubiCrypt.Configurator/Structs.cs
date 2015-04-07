using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace YubiCrypt.Configurator
{
    internal class YKConsts
    {
        internal const byte SLOT_NAV = 2; /* V1 only */
        internal const byte SLOT_CONFIG = 1; /* First (default / V1) configuration */
        internal const byte SLOT_CONFIG2 = 3; /* Second (V2) configuration */
        internal const byte SLOT_UPDATE1 = 4; /* Update slot 1 */
        internal const byte SLOT_UPDATE2 = 5; /* Update slot 2 */
        internal const byte SLOT_SWAP = 6; /* Swap slot 1 and 2 */
        internal const byte SLOT_NDEF = 8; /* Write NDEF record */
        internal const byte SLOT_NDEF2 = 9; /* Write NDEF record for slot 2 */
    }

    //    typedef struct {
    //    unsigned char versionMajor;				    // Firmware version information
    //    unsigned char versionMinor;
    //    unsigned char versionBuild;
    //    unsigned char pgmSeq;					    // Programming sequence number. 0 if no valid configuration
    //    unsigned short touchLevel;				    // Level from touch detector
    //} STATUS;

    [StructLayout(LayoutKind.Sequential)]
    public struct YkStatus
    {
        public byte versionMajor;
        public byte versionMinor;
        public byte versionBuild;
        public byte pgmSeq;
        public ushort touchLevel;
    }

    //struct ykp_config_t {
    //    unsigned int yk_major_version;
    //    unsigned int yk_minor_version;
    //    unsigned int yk_build_version;
    //    unsigned int command;

    //    YK_CONFIG ykcore_config;

    //    unsigned int ykp_acccode_type;
    //};

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct YKP_CONFIG
    {
        public uint yk_major_version;
        public uint yk_minor_version;
        public uint yk_build_version;
        public uint command;

        YK_CONFIG ykcore_config;

        public uint ykp_acccode_type;
    }

    //#define FIXED_SIZE 16 /* Max size of fixed field */
    //#define UID_SIZE		6	/* Size of secret ID field */
    //#define KEY_SIZE 16 /* Size of AES key */
    //#define KEY_SIZE_OATH 20 /* Size of OATH-HOTP key (key field + first 4 of UID field) */
    //#define ACC_CODE_SIZE 6 /* Size of access code to re-program device */
    //struct config_st {
    //    unsigned char fixed[FIXED_SIZE];/* Fixed data in binary format */
    //    unsigned char uid[UID_SIZE];	/* Fixed UID part of ticket */
    //    unsigned char key[KEY_SIZE];	/* AES key */
    //    unsigned char accCode[ACC_CODE_SIZE]; /* Access code to re-program device */
    //    unsigned char fixedSize;	/* Number of bytes in fixed field (0 if not used) */
    //    unsigned char extFlags;	/* Extended flags - YubiKey 2.? and above */
    //    unsigned char tktFlags;	/* Ticket configuration flags */
    //    unsigned char cfgFlags;	/* General configuration flags */
    //    unsigned char rfu[2];	/* Reserved for future use */
    //    unsigned short crc;	/* CRC16 value of all fields */
    //};

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct YK_CONFIG
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] fixd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] uid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] key;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] accCode;
        public byte fixedSize;
        public byte extFlags;
        public byte tktFlags;
        public byte cfgFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] rfu;
        public ushort crc;

    }

    //#define	NDEF_DATA_SIZE			54

    //typedef struct ndef_st YKNDEF;

    //struct ndef_st {
    //unsigned char len;	/* Payload length */
    //unsigned char type;	/* NDEF type specifier */
    //unsigned char data[NDEF_DATA_SIZE];	/* Payload size */
    //unsigned char curAccCode[ACC_CODE_SIZE];	/* Access code */
    //};

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct YK_NDEF
    {
        byte len;
        byte type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        byte[] data;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        byte[] curAccCode;
    }

}
