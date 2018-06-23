// Author: Ivan Grgurina

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Analiza_velikih_skupova_podataka_1b
{
    public static class Hamming
    {
        public const int MASK_STEP = 1;
        public const byte MASK_BASE = 0x01;
        public const int BITS_IN_BYTE = 8;

        public static int Distance(byte[] left, byte[] right)
        {
            int distance = 0;
            for (int i = 0; i < left.Length; i++)
            {
                var diff = left[i] ^ right[i];
                for (int j = 0; j < BITS_IN_BYTE; j++)
                {
                    if ((diff & MASK_BASE) == MASK_BASE)
                        distance++;
                    diff >>= MASK_STEP;
                }
            }
            return distance;
        }
    }

    public static class Sim
    {
        public const byte MASK_BASE = 0x80;
        public const int MASK_STEP = 1;
        public const int HASH_SIZE = 128; // in bits
        public const int HASH_BYTES = 16; // in bytes = HASH_SIZE (128) / BITS_IN_BYTE (8) = 128 / 8 = 16
        public const int BITS_IN_BYTE = 8;

        public static byte[] Hash(string input)
        {
            // split input line from console
            var jedinke = input.Split(' ');

            // heksadecimalni zapis 128-bitnog
            var sh = new int[HASH_SIZE];

            // going string by string
            foreach (var jedinka in jedinke)
            {
                sh = Process(sh, jedinka);
            }

            return Finalize(sh);
        }

        private static int[] Process(int[] sh, string jedinka)
        {
            // calculate md5
            var hash = MD6(jedinka);

            // going byte by byte
            for (int i = 0; i < hash.Length; i++)
            {
                var mask = MASK_BASE;

                // going bit by bit (with moving mask)
                for (int j = 0; j < BITS_IN_BYTE; j++)
                {
                    var index = i * BITS_IN_BYTE + j;
                    // ako je​ i - ti bit u hash jednak​ 1:
                    var test = (hash[i] & mask) != 0; // == 1 ? using mask?


                    if (test)
                        sh[index] += 1;
                    else
                        sh[index] -= 1;

                    mask >>= MASK_STEP; // move the mask
                }
            }

            return sh;
        }

        private static byte[] Finalize(int[] sh)
        {
            var val = new byte[HASH_BYTES];
            // ako je​ i-ti element u sh >=​ 0:
            for (int bitIndex = 0; bitIndex < sh.Length; bitIndex += BITS_IN_BYTE)
            {
                var i = bitIndex / BITS_IN_BYTE;
                var mask = MASK_BASE;
                for (int j = 0; j < BITS_IN_BYTE; j++)
                {
                    if (sh[bitIndex + j] >= 0)
                        val[i] |= mask;
                    mask >>= MASK_STEP;
                }
            }

            return val;
        }

        private static byte[] MD6(string s) => MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(s));

        public const string HEX_FORMAT = "X2";
        private static string HEX(byte[] sh)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < sh.Length; i++)
            {
                sb.Append(sh[i].ToString(HEX_FORMAT));
            }
            return sb.ToString();
        }
    }

    public static class LSH
    {
        public const int BAND_SIZE = 8;
        public const int BITS_IN_BYTE = 8;

        public static List<HashSet<int>> Candidates(List<byte[]> lstHash, int b = BAND_SIZE)
        {
            // init
            var candidates = new List<HashSet<int>>(lstHash.Count);
            for (int i = 0; i < lstHash.Count; i++)
            {
                candidates.Add(new HashSet<int>());
            }

            // algorithm
            for (int band = 0; band < b; band++)
            {
                var buckets = new Dictionary<int, HashSet<int>>();

                for (int currentId = 0; currentId < lstHash.Count; currentId++)
                {
                    var hash = lstHash[currentId];
                    int val = hash2int(hash, band);
                    var txtInBucket = new HashSet<int>();

                    if (buckets.ContainsKey(val))
                    {
                        txtInBucket = buckets[val];

                        foreach (var id in txtInBucket)
                        {
                            candidates[currentId].Add(id);
                            candidates[id].Add(currentId);
                        }
                    }
                    else
                    {
                        txtInBucket = new HashSet<int>();
                    }
                    txtInBucket.Add(currentId);
                    buckets[val] = txtInBucket;
                }
            }

            return candidates;
        }

        // magic
        private static int hash2int(byte[] hash, int band)
        {
            var rv = 0;
            rv |= hash[band * 2];
            rv <<= BITS_IN_BYTE;
            rv |= hash[band * 2 + 1];
            return rv;
        }
    }


    public class SimHashBuckets
    {
        public static void Main(string[] args)
        {
            // 1.
            int N = int.Parse(Console.ReadLine());
            var lstHash = new List<byte[]>(N); // new byte[N][];
            for (int i = 0; i < N; i++)
            {
                lstHash.Add(Sim.Hash(Console.ReadLine().Trim()));
            }

            // 2.
            var candidates = LSH.Candidates(lstHash);

            // 3.
            int Q = int.Parse(Console.ReadLine());
            // upit_0
            for (int j = 0; j < Q; j++)
            {
                var upit = Console.ReadLine().Split(' ');
                Console.WriteLine(Query(candidates, lstHash, I: int.Parse(upit[0]), K: int.Parse(upit[1])));
            }
        }

        public static int Query(List<HashSet<int>> candidates, List<byte[]> lstHash, int I, int K)
        {
            int count = 0;
            var hash = lstHash[I];

            //for (int j = 0; j < lstHash.Count; j++)
            foreach (var j in candidates[I])
            {
                var distance = Hamming.Distance(hash, lstHash[j]);
                if (distance <= K)
                    count++;
            }

            return count;
        }
    }
}
