using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace YubiCrypt.Configurator
{

    public class Yubikey
    {
        public enum YkSlot
        {
            Slot1,
            Slot2
        }

        //ToDo: NDEF stuff

        #region InteropNinjaYOLOHardcoreProgrammingStuff

        private const byte SLOT_CHAL_HMAC1 = 0x30;
        private const byte SLOT_CHAL_HMAC2 = 0x38;

        //https://github.com/Yubico/yubikey-personalization/blob/944e83959d61890503610b902165872a8e9f9e81/ykpers.h

        [DllImport("libykpers-1-1.dll")]
        private static extern IntPtr yk_open_first_key();

        [DllImport("libykpers-1-1.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int yk_challenge_response(IntPtr yk, byte yk_cmd, int may_block, uint challenge_len, byte[] challenge, uint response_len, byte[] response);

        [DllImport("libykpers-1-1.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int yk_get_status(IntPtr yk, ref YkStatus status);

        [DllImport("libykpers-1-1.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int yk_get_serial(IntPtr yk, byte slot, UInt32 flags, ref UInt32 serial);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ykpers_check_version(string version);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ykp_alloc();

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_command(IntPtr cfg);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_configure_command(IntPtr cfg, byte command);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ykp_configure_version(IntPtr cfg, IntPtr st);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_set_tktflag_CHAL_RESP(IntPtr cfg, int enable);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_set_tktflag_APPEND_CR(IntPtr cfg, int enable);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_set_cfgflag_CHAL_HMAC(IntPtr cfg, int enable);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_set_cfgflag_HMAC_LT64(IntPtr cfg, int enable);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_set_cfgflag_CHAL_BTN_TRIG(IntPtr cfg, int enable);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_set_extflag_SERIAL_BTN_VISIBLE(IntPtr cfg, int enable);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_set_extflag_SERIAL_API_VISIBLE(IntPtr cfg, int enable);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_HMAC_key_from_hex(IntPtr cfg, string key);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int yk_write_config(IntPtr yk, IntPtr cfg, int confnum, byte acc_code);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ykp_core_config(IntPtr cfg); // YK_CONFIG *ykp_core_config(YKP_CONFIG *cfg);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_config_num(IntPtr cfg); // int ykp_config_num(YKP_CONFIG *cfg);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int yk_write_command(IntPtr yk, IntPtr cfg, int command, byte acc_code);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _ykp_errno_location();

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_set_fixed(IntPtr cfg, byte[] publicID, uint publicIDLen); //Public ID, 1-16 bytes

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_set_uid(IntPtr cfg, byte[] uid, uint uidLen); // Private ID, 6 bytes

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_AES_key_from_hex(IntPtr cfg, string hexKey); // Secret Key, 16 bytes, 32char string

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_AES_key_from_raw(IntPtr cfg, byte[] hexKey); // Secret Key, 16 bytes

        //NDEF stuff
        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ykp_alloc_ndef();

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ykp_construct_ndef_text(IntPtr ndef, string text, string lang, bool isutf16);

        [DllImport("libykpers-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int yk_write_ndef2(IntPtr yk, IntPtr ndef, int confnum);

        #endregion

        public static bool YkChallengeResponse(byte[] challenge, out byte[] response, YkSlot slot)
        {
            uint yubiBufferLen = 64;
            //Send the challenge to yubikey and get response
            IntPtr yk = IntPtr.Zero;
            while (yk == IntPtr.Zero)
            {
                yk = yk_open_first_key();
                if (yk == IntPtr.Zero)
                {
                    throw new YubikeyNotFoundException();
                }

            }
            bool success = false;
            byte[] temp = new byte[yubiBufferLen];
            success = yk_challenge_response(yk, slot == YkSlot.Slot1 ? SLOT_CHAL_HMAC1 : SLOT_CHAL_HMAC2, 1, Convert.ToUInt32(challenge.Length), challenge, yubiBufferLen, temp) == 1;
            response = new byte[20];
            Array.Copy(temp, response, response.Length);
            return success;
        }

        public static YkStatus YkStatus()
        {
            IntPtr yk = IntPtr.Zero;
            YkStatus st = new YkStatus();

            yk = yk_open_first_key();
            if (yk == IntPtr.Zero)
            {
                throw new YubikeyNotFoundException();
            }
            bool result = yk_get_status(yk, ref st) == 1;
            return st;
        }

        public static bool YkPresent()
        {
            IntPtr yk = IntPtr.Zero;

            yk = yk_open_first_key();
            if (yk == IntPtr.Zero)
                return false;
            return true;
        }

        public static int YkSerial()
        {
            IntPtr yk = IntPtr.Zero;
            yk = yk_open_first_key();
            if (yk == IntPtr.Zero)
            {
                throw new YubikeyNotFoundException();
            }
            UInt32 serial = 0;
            bool result = yk_get_serial(yk, 1, 0, ref serial) == 1;
            return (int)serial;
        }

        public static string YkLibVersion()
        {
            var p = ykpers_check_version(null);
            return Marshal.PtrToStringAnsi(p);
        }

        public static bool EraseConfig(YkSlot slot)
        {
            return yk_write_command(GetYubikeyHandle(), IntPtr.Zero, slot == YkSlot.Slot1 ? YKConsts.SLOT_CONFIG : YKConsts.SLOT_CONFIG2, 0) == 1;
        }

        /// <summary>
        /// Write a HMAC-SHA1 Challenge-Response configuration to the Yubikey
        /// </summary>
        /// <param name="key">The secret cryptographic key to be used in the HMAC-SHA1 function.</param>
        /// <param name="slot">The Yubikey slot to where the configuration will be written to.</param>
        /// <returns></returns>
        public static bool WriteChallengeResponse(string key, YkSlot slot)
        {
            //key must be hex encoded
            if (!IsHexString(key))
                return false;

            //Key must be 20 bytes
            if (key.Length != 40)
                return false;

            IntPtr yk = GetYubikeyHandle();

            var status = YkStatus();
            IntPtr statusPtr = Marshal.AllocHGlobal(Marshal.SizeOf(status));
            Marshal.StructureToPtr(status, statusPtr, true);

            IntPtr cfg = ykp_alloc();

            ykp_configure_version(cfg, statusPtr);

            var res = ykp_set_tktflag_CHAL_RESP(cfg, 1);
            if (res != 1)
                return false;

            res = ykp_set_cfgflag_CHAL_HMAC(cfg, 1);
            if (res != 1)
                return false;

            res = ykp_set_cfgflag_HMAC_LT64(cfg, 1);
            if (res != 1)
                return false;

            res = ykp_set_cfgflag_CHAL_BTN_TRIG(cfg, 0);
            if (res != 1)
                return false;

            res = ykp_configure_command(cfg, slot == YkSlot.Slot1 ? YKConsts.SLOT_CONFIG : YKConsts.SLOT_CONFIG2);
            if (res != 1)
                return false;

            res = ykp_HMAC_key_from_hex(cfg, key);

            if (res != 0)
                return false;

            var success = yk_write_command(yk, ykp_core_config(cfg), ykp_command(cfg), 0) == 1;

            return success;
        }

        public static bool WriteYubicoOTP(string publicID, string privateID, string privateKey, YkSlot slot)
        {
            //parameters must be hex encoded
            if (!IsHexString(publicID) || !IsHexString(privateID) || !IsHexString(privateKey))
                return false;

            //publicID must be 1-16 bytes
            if (publicID.Length < 2 && publicID.Length > 32)
                return false;

            //privateID must be 6 bytes
            if (privateID.Length != 12)
                return false;

            //privateKey must be 16 bytes
            if (privateKey.Length != 32)
                return false;

            IntPtr yk = GetYubikeyHandle();

            var status = YkStatus();
            IntPtr statusPtr = Marshal.AllocHGlobal(Marshal.SizeOf(status));
            Marshal.StructureToPtr(status, statusPtr, true);

            IntPtr cfg = ykp_alloc();

            ykp_configure_version(cfg, statusPtr);


            byte[] publicIDBytes = StringToByteArray(publicID);
            var res = ykp_set_fixed(cfg, publicIDBytes, (uint)publicIDBytes.Length);
            if (res != 1)
                return false;

            byte[] privateIDBytes = StringToByteArray(privateID);
            res = ykp_set_uid(cfg, privateIDBytes, 6);
            if (res != 1)
                return false;

            byte[] secretKey = StringToByteArray(privateKey);
            res = ykp_AES_key_from_raw(cfg, secretKey);
            if (res != 0)
                return false;

            res = ykp_configure_command(cfg, YKConsts.SLOT_CONFIG);
            if (res != 1)
                return false;

            res = ykp_set_tktflag_APPEND_CR(cfg, 1);
            if (res != 1)
                return false;

            var flagsResult = WriteConfigFlags(cfg);
            if (!flagsResult)
                return false;

            var success = yk_write_command(yk, ykp_core_config(cfg), ykp_command(cfg), 0) == 1;

            return success;
        }

        private static bool WriteConfigFlags(IntPtr cfg)
        {
            int res = -1;

            res = ykp_set_extflag_SERIAL_API_VISIBLE(cfg, 1);
            if (res != 1)
                return false;

            return true;
        }

        #region NDEF
        public static bool WriteNDEF(YkSlot slot)
        {
            IntPtr ndef = ykp_alloc_ndef();

            var res = ykp_construct_ndef_text(ndef, "YubiCryptOTP=", "en", false);
            if (res != 1)
                return false;

            res = yk_write_ndef2(GetYubikeyHandle(), ndef, slot == YkSlot.Slot1 ? 1 : 2);
            if (res != 1)
                return false;

            return true;
        }

        #endregion

        #region TestStuff
        public static bool WriteTestChallengeResponse()
        {
            IntPtr yk = GetYubikeyHandle();

            var status = YkStatus();
            IntPtr statusPtr = Marshal.AllocHGlobal(Marshal.SizeOf(status));
            Marshal.StructureToPtr(status, statusPtr, true);

            IntPtr cfg = ykp_alloc();

            ykp_configure_version(cfg, statusPtr);

            var testeVer = Marshal.PtrToStructure(cfg, typeof(YKP_CONFIG));

            var res = ykp_set_tktflag_CHAL_RESP(cfg, 1);
            var err = _ykp_errno_location();
            var no = Marshal.ReadInt32(err);

            res = ykp_set_cfgflag_CHAL_HMAC(cfg, 1);
            err = _ykp_errno_location();
            no = Marshal.ReadInt32(err);

            res = ykp_set_cfgflag_HMAC_LT64(cfg, 1);
            err = _ykp_errno_location();
            no = Marshal.ReadInt32(err);

            res = ykp_set_cfgflag_CHAL_BTN_TRIG(cfg, 0);
            err = _ykp_errno_location();
            no = Marshal.ReadInt32(err);

            var testeFlags = Marshal.PtrToStructure(cfg, typeof(YKP_CONFIG));

            ykp_configure_command(cfg, YKConsts.SLOT_CONFIG);

            var error = ykp_HMAC_key_from_hex(cfg, "ffffffffffffffffffffffffffffffffffffffff") != 0;

            if (error)
                return false;

            var testeKey = Marshal.PtrToStructure(cfg, typeof(YKP_CONFIG));

            var success = yk_write_command(yk, ykp_core_config(cfg), ykp_command(cfg), 0) == 1;

            return success;
        }

        public static bool WriteTestYubicoOTP()
        {

            IntPtr yk = GetYubikeyHandle();

            var status = YkStatus();
            IntPtr statusPtr = Marshal.AllocHGlobal(Marshal.SizeOf(status));
            Marshal.StructureToPtr(status, statusPtr, true);

            IntPtr cfg = ykp_alloc();

            ykp_configure_version(cfg, statusPtr);

            var testeVer = Marshal.PtrToStructure(cfg, typeof(YKP_CONFIG));


            //No need to set any flags (I think...)

            //ykp_set_fixed(cfg, pubId, pubIdLen);

            byte[] publicID = StringToByteArray("3a7fc2e3d5e6");
            var fixedRes = ykp_set_fixed(cfg, publicID, (uint)publicID.Length);

            var testeFixed = Marshal.PtrToStructure(cfg, typeof(YKP_CONFIG));

            byte[] privateID = StringToByteArray("62916d7d745c");
            var uidRes = ykp_set_uid(cfg, privateID, 6);

            var testeUid = Marshal.PtrToStructure(cfg, typeof(YKP_CONFIG));

            byte[] secretKey = StringToByteArray("c71cd2fe10f84a4cacb81820f4d1f036");
            var secretRes = ykp_AES_key_from_raw(cfg, secretKey);

            var testeSecret = Marshal.PtrToStructure(cfg, typeof(YKP_CONFIG));

            ykp_configure_command(cfg, YKConsts.SLOT_CONFIG);

            //ToDo: Flags for CR at the end and stuff
            //but it works!

            var res = ykp_set_tktflag_APPEND_CR(cfg, 1);

            var testePreWrite = Marshal.PtrToStructure(cfg, typeof(YKP_CONFIG));

            var writeRes = yk_write_command(yk, ykp_core_config(cfg), ykp_command(cfg), 0);

            return fixedRes == 1 && uidRes == 1 && secretRes == 0 && writeRes == 1;
        }
        #endregion

        private static IntPtr GetYubikeyHandle()
        {
            IntPtr yk = IntPtr.Zero;
            yk = yk_open_first_key();
            if (yk == IntPtr.Zero)
            {
                throw new YubikeyNotFoundException();
            }
            return yk;
        }

        //https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static bool IsHexString(string test)
        {
            //ty senhores do StackOverflow
            //https://stackoverflow.com/questions/223832/check-a-string-to-see-if-all-characters-are-hexadecimal-values
            // For C-style hex notation (0xFF) you can use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }


    }
}
