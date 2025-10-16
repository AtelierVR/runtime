using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Nox.CCK.Utils {
	public static class Crypto {
		public static string PrivateKeyPemPath
			=> Path.Combine(Constants.AppPath, "private_key.pem");

		public static string PublicKeyPemPath
			=> Path.Combine(Constants.AppPath, "public_key.pem");

		public static RSA GetKeys() {
			if (File.Exists(PrivateKeyPemPath))
				return LoadPrivateKeyFromPem(PrivateKeyPemPath);

			var rsa = GenerateKeys();
			SaveKeysToPem(rsa);
			return rsa;
		}

		public static void DeleteKeys() {
			if (File.Exists(PrivateKeyPemPath))
				File.Delete(PrivateKeyPemPath);
			if (File.Exists(PublicKeyPemPath))
				File.Delete(PublicKeyPemPath);
		}

		public static RSA GenerateKeys()
			=> RSA.Create(4096);

		/// <summary>
		/// Exporte la clé privée au format PEM
		/// </summary>
		public static string ExportPrivateKeyToPem(RSA rsa) {
			var parameters = rsa.ExportParameters(true);
			var rsaPrivateKey = new RsaPrivateCrtKeyParameters(
				new Org.BouncyCastle.Math.BigInteger(1, parameters.Modulus),
				new Org.BouncyCastle.Math.BigInteger(1, parameters.Exponent),
				new Org.BouncyCastle.Math.BigInteger(1, parameters.D),
				new Org.BouncyCastle.Math.BigInteger(1, parameters.P),
				new Org.BouncyCastle.Math.BigInteger(1, parameters.Q),
				new Org.BouncyCastle.Math.BigInteger(1, parameters.DP),
				new Org.BouncyCastle.Math.BigInteger(1, parameters.DQ),
				new Org.BouncyCastle.Math.BigInteger(1, parameters.InverseQ)
			);

			using var writer    = new StringWriter();
			var       pemWriter = new PemWriter(writer);
			pemWriter.WriteObject(rsaPrivateKey);
			return writer.ToString();
		}

		/// <summary>
		/// Exporte la clé publique au format PEM
		/// </summary>
		public static string ExportPublicKeyToPem(RSA rsa) {
			var parameters = rsa.ExportParameters(false);
			var rsaPublicKey = new RsaKeyParameters(
				false,
				new Org.BouncyCastle.Math.BigInteger(1, parameters.Modulus),
				new Org.BouncyCastle.Math.BigInteger(1, parameters.Exponent)
			);

			using var writer    = new StringWriter();
			var       pemWriter = new PemWriter(writer);
			pemWriter.WriteObject(rsaPublicKey);
			return writer.ToString();
		}

		/// <summary>
		/// Sauvegarde les clés au format PEM
		/// </summary>
		public static void SaveKeysToPem(RSA rsa) {
			var privateKeyPem = ExportPrivateKeyToPem(rsa);
			var publicKeyPem  = ExportPublicKeyToPem(rsa);

			File.WriteAllText(PrivateKeyPemPath, privateKeyPem);
			File.WriteAllText(PublicKeyPemPath, publicKeyPem);
		}

		/// <summary>
		/// Charge une clé privée depuis un fichier PEM
		/// </summary>
		public static RSA LoadPrivateKeyFromPem(string pemPath) {
			if (!File.Exists(pemPath))
				throw new FileNotFoundException($"Le fichier PEM {pemPath} n'existe pas");

			var       pemContent = File.ReadAllText(pemPath);
			using var reader     = new StringReader(pemContent);
			var       pemReader  = new PemReader(reader);
			var       keyPair    = (AsymmetricCipherKeyPair)pemReader.ReadObject();
			var       privateKey = (RsaPrivateCrtKeyParameters)keyPair.Private;

			var rsa = RSA.Create();
			var parameters = new RSAParameters {
				Modulus  = privateKey.Modulus.ToByteArrayUnsigned(),
				Exponent = privateKey.PublicExponent.ToByteArrayUnsigned(),
				D        = privateKey.Exponent.ToByteArrayUnsigned(),
				P        = privateKey.P.ToByteArrayUnsigned(),
				Q        = privateKey.Q.ToByteArrayUnsigned(),
				DP       = privateKey.DP.ToByteArrayUnsigned(),
				DQ       = privateKey.DQ.ToByteArrayUnsigned(),
				InverseQ = privateKey.QInv.ToByteArrayUnsigned()
			};
			rsa.ImportParameters(parameters);
			return rsa;
		}

		/// <summary>
		/// Charge une clé publique depuis un fichier PEM
		/// </summary>
		public static RSA LoadPublicKeyFromPem(string pemPath) {
			if (!File.Exists(pemPath))
				throw new FileNotFoundException($"Le fichier PEM {pemPath} n'existe pas");

			var       pemContent = File.ReadAllText(pemPath);
			using var reader     = new StringReader(pemContent);
			var       pemReader  = new PemReader(reader);
			var       publicKey  = (RsaKeyParameters)pemReader.ReadObject();

			var rsa = RSA.Create();
			var parameters = new RSAParameters {
				Modulus  = publicKey.Modulus.ToByteArrayUnsigned(),
				Exponent = publicKey.Exponent.ToByteArrayUnsigned()
			};
			rsa.ImportParameters(parameters);
			return rsa;
		}

		public static byte[] Encrypt(string data, RSA rsa) {
			var dataBytes = Encoding.UTF8.GetBytes(data);
			return rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
		}

		public static string Decrypt(byte[] data, RSA rsa) {
			var decryptedBytes = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
			return Encoding.UTF8.GetString(decryptedBytes);
		}

		public static byte[] Sign(string data, RSA rsa)
			=> Sign(Encoding.UTF8.GetBytes(data), rsa);

		public static byte[] Sign(byte[] data, RSA rsa)
			=> rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

		public static bool Verify(string data, byte[] signature, RSA rsa)
			=> Verify(Encoding.UTF8.GetBytes(data), signature, rsa);

		public static bool Verify(byte[] data, byte[] signature, RSA rsa)
			=> rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

		/// <summary>
		/// Compresse une clé publique RSA en format compact pour transmission
		/// </summary>
		public static string CompressPublicKey(RSA rsa) {
			var publicKeyPem = ExportPublicKeyToPem(rsa);
			var lines = publicKeyPem.Split('\n')
				.Select(line => line.Trim())
				.Where(line => !line.StartsWith("-----") && !string.IsNullOrWhiteSpace(line));
			return string.Join("", lines);
		}

		public static byte[] ExportPublicKeyToDer(RSA rsa) {
			var parameters = rsa.ExportParameters(false);
			var rsaPublicKey = new RsaKeyParameters(
				false,
				new Org.BouncyCastle.Math.BigInteger(1, parameters.Modulus),
				new Org.BouncyCastle.Math.BigInteger(1, parameters.Exponent)
			);

			var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(rsaPublicKey);
			return publicKeyInfo.GetDerEncoded();
		}

		/// <summary>
		/// Importe une clé publique depuis un format DER
		/// </summary>
		public static RSA ImportPublicKeyFromDer(byte[] derData) {
			var publicKeyInfo = SubjectPublicKeyInfo.GetInstance(derData);
			var rsaPublicKey  = (RsaKeyParameters)PublicKeyFactory.CreateKey(publicKeyInfo);

			var rsa = RSA.Create();
			var parameters = new RSAParameters {
				Modulus  = rsaPublicKey.Modulus.ToByteArrayUnsigned(),
				Exponent = rsaPublicKey.Exponent.ToByteArrayUnsigned()
			};
			rsa.ImportParameters(parameters);
			return rsa;
		}
	}
}