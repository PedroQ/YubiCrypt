using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YubiCrypt.Configurator
{
    public class YubikeyConfigurator
    {
        public Action<string, object[]> WriteWarning { get; set; }
        public Action<string, object[]> WriteSuccess { get; set; }
        public Action<string, object[]> WriteError { get; set; }
        public Action<string, object[]> WriteInfo { get; set; }

        public void Provision(string backupPassphrase, bool verbose)
        {
            try
            {
                var ykStatus = Yubikey.YkStatus();
                var ykSerial = Yubikey.YkSerial();
                WriteInfoInternal("Found Yubikey!");
                WriteInfoInternal("Firmware Version : {0}.{1}.{2}", ykStatus.versionMajor, ykStatus.versionMinor, ykStatus.versionBuild);
                WriteInfoInternal("Serial #: {0}", ykSerial);

                YubiCryptConfig configFile = new YubiCryptConfig();

                var rng = new RNGCryptoServiceProvider();

                byte[] hmacKey = new byte[20];
                byte[] otpKey = new byte[16];
                byte[] publicID = new byte[6];
                byte[] privateID = new byte[6];


                configFile.YubikeySerialNumber = ykSerial;
                configFile.YubikeyVersion = string.Format("{0}.{1}.{2}", ykStatus.versionMajor, ykStatus.versionMinor, ykStatus.versionBuild);




                Console.WriteLine("Provisioning Slot #1 (Yubico OTP)");

                rng.GetBytes(publicID);
                rng.GetBytes(privateID);
                rng.GetBytes(otpKey);

                var step1Success = Yubikey.WriteYubicoOTP(BytesToHexString(publicID), BytesToHexString(privateID), BytesToHexString(otpKey), Yubikey.YkSlot.Slot1);
                if (step1Success)
                {
                    configFile.OTPPublicID = ModHex.Encode(publicID);
                    Array.Clear(publicID, 0, publicID.Length);
                    configFile.OTPPrivateID = BytesToHexString(privateID);
                    Array.Clear(privateID, 0, privateID.Length);
                    configFile.OTPAESKey = BytesToHexString(otpKey);
                    Array.Clear(otpKey, 0, otpKey.Length);

                    WriteSuccessInternal("Successfully wrote the configuration to Slot #{0}", 1);
                }
                else
                {
                    WriteErrorInternal("An error occurred writing the configuration to Slot #{0}", 1);
                }

                Console.WriteLine("Provisioning Slot #2 (HMAC-SHA1 Challenge-Response)");

                rng.GetBytes(hmacKey);

                var step2Success = Yubikey.WriteChallengeResponse(BytesToHexString(hmacKey), Yubikey.YkSlot.Slot2);
                if (step2Success)
                {
                    if (backupPassphrase != null)
                    {
                        byte[] salt = new byte[32];
                        rng.GetBytes(salt);
                        configFile.ChallengeResponseKey = EncryptTFKDKey(hmacKey, backupPassphrase, salt);
                    }
                    Array.Clear(hmacKey, 0, hmacKey.Length);
                    WriteSuccessInternal("Successfully wrote the configuration to Slot #{0}", 2);
                }
                else
                {
                    WriteErrorInternal("An error occurred writing the configuration to Slot #{0}", 2);
                }

                var step3Success = false;
                if (ykStatus.versionMajor < 3)
                {
                    configFile.NDEFEnabled = false;
                    step3Success = true;
                    WriteWarningInternal("Your Yubikey does not support NDEF. This configuration will not be written.");
                }
                else
                {
                    configFile.NDEFEnabled = true;
                    step3Success = Yubikey.WriteNDEF(Yubikey.YkSlot.Slot1);
                    if (step3Success)
                        WriteSuccessInternal("Successfully wrote the NDEF configuration for Slot #{0}", 1);
                    else
                        WriteErrorInternal("An error occurred writing the NDEF configuration for Slot #{0}", 1);
                }

                if (step1Success && step2Success && step3Success)
                {
                    WriteInfoInternal("Writing config.json...", verbose);
                    configFile.WriteToFile();
                    WriteSuccessInternal("Yubikey successfully provisioned.");
                }
            }
            catch (CryptographicException ex)
            {
                WriteErrorInternal("There was a problem with the Cryptographic RNG.\n{0}", ex.Message);
                return;
            }
            catch (YubikeyNotFoundException)
            {
                WriteWarningInternal("No Yubikey found. Please connect your Yubikey!");
            }
        }

        public void Provision(bool verbose)
        {
            Provision(null, verbose);
        }

        private void WriteWarningInternal(string message, params object[] args)
        {
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine("Warning: " + string.Format(message, args));
            //Console.ResetColor();
            if (WriteWarning != null)
                WriteWarning(message, args);
        }

        private void WriteErrorInternal(string message, params object[] args)
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            //Console.WriteLine("Error: " + string.Format(message, args));
            //Console.ResetColor();
            if (WriteError != null)
                WriteError(message, args);
        }
        private void WriteSuccessInternal(string message, params object[] args)
        {
            //Console.ForegroundColor = ConsoleColor.Green;
            //Console.WriteLine(string.Format(message, args));
            //Console.ResetColor();
            if (WriteSuccess != null)
                WriteSuccess(message, args);
        }

        private void WriteInfoInternal(string message, params object[] args)
        {
            //Console.ForegroundColor = ConsoleColor.Cyan;
            //Console.WriteLine("Info: " + string.Format(message, args));
            //Console.ResetColor();
            if (WriteInfo != null)
                WriteInfo(message, args);
        }

        private void WriteInfoInternal(string message, bool verbose, params object[] args)
        {
            if (verbose)
                WriteInfoInternal(message, args);
        }

        private string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public string EncryptTFKDKey(byte[] hmacSecret, string backupPassphrase, byte[] salt)
        {
            string encryptedHmacSecret;

            using (RijndaelManaged cipher = new RijndaelManaged())
            {
                byte[] encryptionKey = new byte[cipher.KeySize / 8];
                byte[] encryptionIV = new byte[cipher.BlockSize / 8];

                cipher.Padding = PaddingMode.PKCS7;

                CreateKey(backupPassphrase, salt, ref encryptionKey, ref encryptionIV);

                ICryptoTransform encryptor = cipher.CreateEncryptor(encryptionKey, encryptionIV);

                using (MemoryStream encMemoryStream = new MemoryStream())
                {
                    using (CryptoStream encCryptoStream = new CryptoStream(encMemoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        encCryptoStream.Write(hmacSecret, 0, hmacSecret.Length);
                        encCryptoStream.FlushFinalBlock();
                    }
                    encryptedHmacSecret = BytesToHexString(encMemoryStream.ToArray());
                }
            }

            return string.Format("{0}:{1}", encryptedHmacSecret, BytesToHexString(salt));

        }

        private static void CreateKey(string backupPassphrase, byte[] salt, ref byte[] key, ref byte[] iv)
        {
            var keyGenerator = new Rfc2898DeriveBytes(backupPassphrase, salt, 64000);
            key = keyGenerator.GetBytes(key.Length);
            iv = keyGenerator.GetBytes(iv.Length);
        }
    }
}
