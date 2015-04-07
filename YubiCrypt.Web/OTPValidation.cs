using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using YubiCrypt.Web.Models;

namespace YubiCrypt.Web
{
    public class OTPValidation
    {

#if ALLOW_DUMMY_OTPS

        private const string DUMMY_TAG_VALUE = "TESTTAGPLEASEIGNORE";
#endif
        public enum Status
        {
            OK = 1,
            BAD_OTP = 2,
            REPLAYED_OTP = 3,
            DELAYED_OTP = 4,
            NO_CLIENT = 5,
            CORE_MELTDOWN = 0

        }

        public static int hexdec(string hex)
        {
            return Convert.ToInt32(hex, 16);
        }


        internal static string modhex2hex(string input)
        {
            string hex = "0123456789abcdef";
            string modhex = "cbdefghijklnrtuv";
            StringBuilder retVal = new StringBuilder();
            int pos;
            for (int i = 0; i < input.Length; i++)
            {
                pos = modhex.IndexOf(input[i]);
                if (pos > -1)
                    retVal.Append(hex[pos]);
                else
                    throw new Exception(input[i] + " is not a valid character");
            }
            return retVal.ToString();
        }


        internal static int CRC(string plaintext)
        {
            int b, n, crc = 0xffff;
            for (int i = 0; i < 16; i++)
            {
                b = hexdec(plaintext[i * 2].ToString() + plaintext[i * 2 + 1].ToString());
                crc = crc ^ (b & 0xff);
                for (int j = 0; j < 8; j++)
                {
                    n = crc & 1;
                    crc = crc >> 1;
                    if (n != 0)
                        crc = crc ^ 0x8408;
                }
            }
            return crc;
        }

        internal static bool isCRCValid(string plaintext)
        {
            return CRC(plaintext) == 0xf0b8;
        }

        internal static Tuple<string, string> SplitOTP(string otp)
        {
            var match = Regex.Match(otp, "([cbdefghijklnrtuv]{0,16})([cbdefghijklnrtuv]{32})", RegexOptions.IgnoreCase);
            if (!match.Success)
                return null;

            string publicId = match.Groups[1].Value;
            string token = modhex2hex(match.Groups[2].Value);

            return new Tuple<string, string>(publicId, token);
        }

        internal static Status Validate(string otp, string currentUserId)
        {
#if ALLOW_DUMMY_OTPS
            if (string.Equals(otp, DUMMY_TAG_VALUE, StringComparison.InvariantCulture))
                return Status.OK;
#endif
            if (otp.Length <= 32 || otp.Length > 48) //0-16char yubikey ID + 32char token
                return Status.BAD_OTP;

            var otpDetails = SplitOTP(otp);
            if (otp == null)
                return Status.BAD_OTP;

            var publicId = otpDetails.Item1;
            var token = otpDetails.Item2;


            using (var db = new ApplicationDbContext())
            {
                var userKey = db.YubiKeys.Where(k => k.UserProfile.Id == currentUserId && k.YubiKeyUID == publicId).SingleOrDefault();
                if (userKey == null)
                    return Status.BAD_OTP;

                string aesKey = userKey.AESKey;
                string internalName = userKey.Privateidentity;
                string plaintext = AESDecode(aesKey, token); // confirmar valores
                if (internalName != plaintext.Substring(0, 12))
                    return Status.BAD_OTP; // Yubikey rejected because the uid (6 byte secret) in the decrypted AES key (set with with ykpersonalise -ouid) does not match the secret key (internalname) in the database
                if (!isCRCValid(plaintext))
                    return Status.BAD_OTP;

                int internalCounter = hexdec(plaintext.Substring(14, 2) + plaintext.Substring(12, 2) + plaintext.Substring(22, 2));
                int timestamp = hexdec(plaintext.Substring(20, 2) + plaintext.Substring(18, 2) + plaintext.Substring(16, 2));
                int counter = userKey.Counter;
                int time = userKey.Time;
                if (counter >= internalCounter)
                    return Status.REPLAYED_OTP;
                if (time >= timestamp && ((counter >> 8) == (internalCounter >> 8)))
                    return Status.DELAYED_OTP;

                userKey.Counter = internalCounter;
                userKey.Time = timestamp;
                db.SaveChanges();

            }

            return Status.OK;
        }

        internal static string AESDecode(string key, string data)
        {
            using (var aesProvider = new AesCryptoServiceProvider())
            {
                aesProvider.Mode = CipherMode.ECB;
                aesProvider.Padding = PaddingMode.None;
                aesProvider.Key = Convert.FromBase64String(key);
                byte[] src = StringToByteArray(data);
                using (ICryptoTransform decrypt = aesProvider.CreateDecryptor())
                {

                    byte[] dest = decrypt.TransformFinalBlock(src, 0, src.Length);
                    return BitConverter.ToString(dest).Replace("-", "").ToLower();
                }
            }

        }

        //https://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
        internal static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}