using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;


namespace testhash
{
    class Program
    {
        static void Main(string[] args)
        {
            // Block #286819
            // https://blockchain.info/block/0000000000000000e067a478024addfecdc93628978aa52d91fabd4292982a50

            Int32 version0 = 2;
            string hashPrevBlock0 = "000000000000000117c80378b8da0e33559b5997f2ad55e2f7d18ec1975b9717";
            string hashMerkleRoot0 = "871714dcbae6c8193a2bb9b2a69fe1c0440399f38d94b3a0f1b447275a29978a";
            DateTime timestamp0 = new DateTime(2014, 2, 20, 4, 57, 25, DateTimeKind.Utc); // 2014-02-20 04:57:25 (0x358b0553)
            int difficulty0= 419520339; // 0x535f0119

            // version    | 4  | version of the bitcoin protocol used to create the block
            // prevHash   | 32 | hash of the previous block
            // merkleRoot | 32 | root of a sha256 hash tree where the leaves are transactions
            // time       | 4  | time of block creation in seconds since 1970 - 01 - 01T00: 00 UTC
            // bits       | 4  | difficulty of block hash in compressed form
            // nonce      | 4  | field used in mining


            // convert values
            byte[] version = BitConverter.GetBytes(version0);
            byte[] hashPrevBlock = StringToByteArray(hashPrevBlock0);
            byte[] hashMerkleRoot = StringToByteArray(hashMerkleRoot0);
            byte[] timestamp = BitConverter.GetBytes((Int32)(timestamp0 - new DateTime(1970, 1, 1)).TotalSeconds);
            byte[] difficulty = BitConverter.GetBytes(difficulty0);

            // concatenate in one array converted to little Endian
            var block_bytes0 = Combine(version,
                                       hashPrevBlock.Reverse().ToArray(),
                                       hashMerkleRoot.Reverse().ToArray(),
                                       timestamp,
                                       difficulty);

            // calculate target (verification) number
            byte[] exponent = new byte[4];
            exponent[0] = difficulty.Reverse().ToArray()[0];
            byte[] mantissa = new byte[4];
            mantissa[0] = difficulty.Reverse().ToArray()[1];
            mantissa[1] = difficulty.Reverse().ToArray()[2];
            mantissa[2] = difficulty.Reverse().ToArray()[3];
            byte[] target = new byte[32];
            int offset = BitConverter.ToInt32(exponent, 0) - 3;
            target[offset + 1] = mantissa[2];
            target[offset + 2] = mantissa[1];
            target[offset + 3] = mantissa[0];
            target = target.Reverse().ToArray();

            var target_s = BytesToString(target);

            int nonce = 856000000; //   856192328;

            for (;;)
            {
                // Block_header = Version(4) + hashPrevBlock(32) + hashMerkleRoot(32) + Time(4) + Bits(4) + Nonce(4)
                var block_bytes = Combine(block_bytes0, BitConverter.GetBytes(nonce));

                var hashed = GetSha256Hash(GetSha256Hash(block_bytes));
                var hashed_s = BytesToString(hashed);
                // check if hash less than the target
                if (hashed_s.EndsWith("00000000000000000"))
                {
                    Console.WriteLine("Hash found:");
                    Console.WriteLine($"{nonce}    {BytesToString(hashed.Reverse().ToArray())}");
                    break;
                }

                if (nonce % 1000 == 0)
                {
                    string s = $"{BytesToString(hashed.Reverse().ToArray())}";
                    Console.WriteLine($"{nonce}    {s}");
                }

                nonce += 1; 
            }

            Console.ReadKey();
        }

        static byte[] GetSha256Hash(byte[] input)
        {
            var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(input);
            return bytes;
        }

        static string BytesToString(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        static byte[] Combine(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }
    }
}
