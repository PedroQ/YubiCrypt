using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using YubiCrypt.ClientLibrary.Encryption;
using YubiCrypt.ClientLibrary.KeyDerivation;
using YubiCrypt.ClientLibrary.MAC;
namespace YubiCrypt.ClientLibrary
{
    public class YubiCryptEngine
    {
        private readonly byte[] YC_FILE_SPEC_BYTES = { 0x59, 0x43, 0x46 };
        private readonly byte YC_FILE_SPEC_VERSION = 0x01;

        public IEnumerable<ISymmetricCipherProvider> SymmetricCipherProviders { get; private set; }
        public IEnumerable<ITwoFactorKeyDerivationFunctionProvider> KeyDerivationFunctionProviders { get; private set; }
        public IEnumerable<IMACProvider> MACProviders { get; set; }

        public YubiCryptEngine()
            : this(null)
        {

        }

        public YubiCryptEngine(Assembly callingAssembly)
        {
            List<Assembly> assemblies = new List<Assembly>();
            if (callingAssembly != null)
                assemblies.Add(callingAssembly);
            assemblies.Add(typeof(YubiCryptEngine).GetTypeInfo().Assembly);

            var configuration = new ContainerConfiguration().WithAssemblies(assemblies);

            using (var container = configuration.CreateContainer())
            {
                SymmetricCipherProviders = container.GetExports<ISymmetricCipherProvider>();
                KeyDerivationFunctionProviders = container.GetExports<ITwoFactorKeyDerivationFunctionProvider>();
                MACProviders = container.GetExports<IMACProvider>();
            }
#if !DEBUG
            if (!KeyDerivationFunctionProviders.Any())
                throw new CompositionFailedException("No Key Derivation Function providers were found.");
                
            if (!SymmetricCipherProviders.Any())
                throw new CompositionFailedException("No Symmetric Cipher providers were found.");

            if (!MACProviders.Any())
                throw new CompositionFailedException("No Message Authentication Code providers were found.");

#endif
            KeyDerivationFunctionProviders = KeyDerivationFunctionProviders.OrderBy(p => p.Name);
            SymmetricCipherProviders = SymmetricCipherProviders.OrderBy(p => p.Name);
            MACProviders = MACProviders.OrderBy(p => p.Name);

        }

        // Why accept an outputStream instead of returning a Stream? Flexibility. 
        // Client apps can write the encrypted output directly to an Network Stream, a File, a Memory Stream, etc
        // It's up to the client implementation where to direct the output
        public void EncryptFile(Stream inputFileStream, Stream outputFileStream, string passphrase, CipherSuite cipherSuite)
        {
            //Check if we support this cipher suite

            ITwoFactorKeyDerivationFunctionProvider twoFactorKeyDerivationProvider = KeyDerivationFunctionProviders.Where(p => p.IDByte == cipherSuite.KeyDerivationFunctionIDByte).SingleOrDefault();
            ISymmetricCipherProvider symmetricCipherProvider = SymmetricCipherProviders.Where(p => p.IDByte == cipherSuite.SymmetricCipherIDByte).SingleOrDefault();
            IMACProvider macProvider = MACProviders.Where(p => p.IDByte == cipherSuite.MACIDByte).SingleOrDefault();

            if (twoFactorKeyDerivationProvider == null)
                throw new Exception("Unsupported Key Derivation Function with ID byte " + cipherSuite.KeyDerivationFunctionIDByte);

            if (symmetricCipherProvider == null)
                throw new Exception("Unsupported Symmetric Cipher with ID byte " + cipherSuite.SymmetricCipherIDByte);

            if (macProvider == null)
                throw new Exception("Unsupported Message Authentication Code with ID byte " + cipherSuite.MACIDByte);


            Random randomGen = new Random();

            var salt = new byte[32];
            randomGen.NextBytes(salt); //random salt (RNG doesn't have to be cryptographically strong)
            var passphraseBytes = Encoding.UTF8.GetBytes(passphrase);


            byte[] encryptionKey = new byte[symmetricCipherProvider.KeySize / 8];
            byte[] hmacKey = new byte[symmetricCipherProvider.KeySize / 8];
            byte[] iv = new byte[symmetricCipherProvider.BlockSize / 8];

            twoFactorKeyDerivationProvider.DeriveHMACAndEncryptionKey(passphraseBytes, salt, ref hmacKey, ref encryptionKey);

            randomGen.NextBytes(iv);

            var tokenSerialNumberBytes = twoFactorKeyDerivationProvider.GetExternalTokenSerial();
            var inputValidation = Combine(passphraseBytes, tokenSerialNumberBytes);
            var inputValidationHash = CalculateInputValidation(inputValidation);

            //Write Header
            var headerStream = new BinaryWriter(outputFileStream);
            //Header: YCF1 (4bytes) + Cipher Suite (4bytes) + Salt Seed (32bytes) + IV (16bytes) + Iterations (4bytes) + Input Validation (32bytes)
            headerStream.Write(YC_FILE_SPEC_BYTES);
            headerStream.Write(YC_FILE_SPEC_VERSION);
            headerStream.Write(IdBytesFromCipherSuite(cipherSuite));
            headerStream.Write(salt);
            headerStream.Write(iv);
            headerStream.Write((Int32)twoFactorKeyDerivationProvider.NumberOfOterations);
            headerStream.Write(inputValidationHash);

            symmetricCipherProvider.Encrypt(inputFileStream, outputFileStream, encryptionKey, iv);

            var streamHMAC = macProvider.CalculateMAC(outputFileStream, hmacKey);

            headerStream.Write(streamHMAC);
        }

        public void DecryptFile(Stream inputFileStream, Stream outputFileStream, string passphrase)
        {
            if (inputFileStream.Position != 0)
            {
                if (inputFileStream.CanSeek)
                    inputFileStream.Seek(0, SeekOrigin.Begin);
                else
                    throw new ArgumentException("Unable to seek inputFileStream to the beggining.");
            }

            using (var headerStream = new BinaryReader(inputFileStream))
            {
                var fileSpecMagicBytes = headerStream.ReadBytes(3);
                if (!fileSpecMagicBytes.SequenceEqual(YC_FILE_SPEC_BYTES))
                    throw new Exception("Wrong file header");

                var fileSpecVersion = headerStream.ReadByte();
                if (fileSpecVersion != YC_FILE_SPEC_VERSION)
                    throw new Exception("Invalid YubiCrypt file version.");

                var cipherSuiteBytes = headerStream.ReadBytes(4);

                var cipherSuite = CipherSuiteFromIdBytes(cipherSuiteBytes);

                var cipherSuiteKDF = KeyDerivationFunctionProviders.Where(p => p.IDByte == cipherSuite.KeyDerivationFunctionIDByte).SingleOrDefault();
                if (cipherSuiteKDF == null)
                    throw new Exception("Unsupported Key Derivation Function with ID byte " + cipherSuite.KeyDerivationFunctionIDByte);

                var cipherSuiteSymmetricCipher = SymmetricCipherProviders.Where(p => p.IDByte == cipherSuite.SymmetricCipherIDByte).SingleOrDefault();
                if (cipherSuiteSymmetricCipher == null)
                    throw new Exception("Unsupported Key Derivation Function with ID byte " + cipherSuite.SymmetricCipherIDByte);

                var cipherSuiteMAC = MACProviders.Where(p => p.IDByte == cipherSuite.MACIDByte).SingleOrDefault();
                if (cipherSuiteMAC == null)
                    throw new Exception("Unsupported Message Authentication Code with ID byte " + cipherSuite.MACIDByte);

                var passphraseBytes = Encoding.UTF8.GetBytes(passphrase);
                var salt = headerStream.ReadBytes(32);
                var iv = headerStream.ReadBytes(16);
                var iterations = headerStream.ReadInt32();
                var inputValidation = headerStream.ReadBytes(32);


                var tokenSerialNumberBytes = cipherSuiteKDF.GetExternalTokenSerial();
                var inputValidationInput = Combine(passphraseBytes, tokenSerialNumberBytes);
                var inputValidationHash = CalculateInputValidation(inputValidationInput);

                if (!inputValidationHash.SequenceEqual(inputValidation))
                    throw new Exception("Invalid credentials. Check if you are entering the correct passphrase and using the correct Yubikey token.");



                byte[] encryptionKey = new byte[cipherSuiteSymmetricCipher.KeySize / 8];
                byte[] hmacKey = new byte[cipherSuiteSymmetricCipher.KeySize / 8];
                cipherSuiteKDF.NumberOfOterations = iterations;
                cipherSuiteKDF.DeriveHMACAndEncryptionKey(passphraseBytes, salt, ref hmacKey, ref encryptionKey);

                //Check file HMAC
                if (!cipherSuiteMAC.ValidateMAC(inputFileStream, hmacKey))
                    throw new Exception("Invalid HMAC.");

                //Header is 92 bytes long
                inputFileStream.Seek(92, SeekOrigin.Begin);
                cipherSuiteSymmetricCipher.Decrypt(inputFileStream, outputFileStream, encryptionKey, iv);
            }
        }

        public byte[] DecryptTokenSecret(string encryptedSecret, string encryptionSalt, string passphrase)
        {
            byte[] encryptedSecretBytes = StringToByteArray(encryptedSecret);
            byte[] encryptionSaltBytes = StringToByteArray(encryptionSalt);
            byte[] passphraseBytes = new UTF8Encoding(false).GetBytes(passphrase);


            var pbkdf2 = new Org.BouncyCastle.Crypto.Generators.Pkcs5S2ParametersGenerator(new Org.BouncyCastle.Crypto.Digests.Sha1Digest());
            pbkdf2.Init(passphraseBytes, encryptionSaltBytes, 64000);

            ICipherParameters cipherParameters = pbkdf2.GenerateDerivedParameters(256, 128);

            PaddedBufferedBlockCipher blockCipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()), new Pkcs7Padding());
            blockCipher.Init(false, cipherParameters);
            byte[] secretkey = new byte[20];
            int bCount = blockCipher.ProcessBytes(encryptedSecretBytes, secretkey, 0);
            blockCipher.DoFinal(secretkey, bCount);

            return secretkey;
            
        }

        private byte[] CalculateInputValidation(byte[] inputValidation)
        {
            var hash = new Sha256Digest();
            hash.BlockUpdate(inputValidation, 0, inputValidation.Length);
            byte[] result = new byte[hash.GetDigestSize()];
            hash.DoFinal(result, 0);
            return result;
        }

        private byte[] IdBytesFromCipherSuite(CipherSuite cipherSuite)
        {
            return new byte[] { cipherSuite.KeyDerivationFunctionIDByte, cipherSuite.SymmetricCipherIDByte, cipherSuite.MACIDByte, 0x00 };
        }

        private CipherSuite CipherSuiteFromIdBytes(byte[] headerIdBytes)
        {
            if (headerIdBytes.Length != 4)
                throw new ArgumentException("Cipher suite header field must be exactly 4 bytes.", "headerBytes");

            var cipherSuite = new CipherSuite()
            {
                KeyDerivationFunctionIDByte = headerIdBytes[0],
                SymmetricCipherIDByte = headerIdBytes[1],
                MACIDByte = headerIdBytes[2]
                //4th byte is not used ATM
            };

            return cipherSuite;

        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        //https://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
        private byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}
