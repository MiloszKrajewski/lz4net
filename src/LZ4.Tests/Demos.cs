using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace LZ4.Tests
{
    [TestFixture]
    public class Demos
    {
        const string LoremIpsum =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sit amet mauris diam. " +
            "Mauris mollis tristique sollicitudin. Nunc nec lectus nec ipsum pharetra venenatis. " +
            "Fusce et consequat massa, eu vestibulum erat. Proin in lectus a lacus fermentum viverra. " +
            "Aliquam vel tellus aliquam, eleifend justo ultrices, elementum elit. " +
            "Donec sed ullamcorper ex, ac sagittis ligula. Pellentesque vel risus lacus. " +
            "Proin aliquet lectus et tellus tristique, eget tristique magna placerat. " +
            "Maecenas ut ipsum efficitur, lobortis mauris at, bibendum libero. " +
            "Curabitur ultricies rutrum velit, eget blandit lorem facilisis sit amet. " +
            "Nunc dignissim nunc iaculis diam congue tincidunt. Suspendisse et massa urna. " +
            "Aliquam sagittis ornare nisl, quis feugiat justo eleifend iaculis. " +
            "Ut pulvinar id purus non convallis.";

        void CompressToStream()
        {
            using (var fileStream = new FileStream("lorem.lz4", FileMode.Create))
            using (var lz4Stream = new LZ4Stream(fileStream, LZ4StreamMode.Compress))
            using (var writer = new StreamWriter(lz4Stream))
            {
                for (var i = 0; i < 100; i++)
                    writer.WriteLine(LoremIpsum);
            }
        }

        void DecompressFromStream()
        {
            using (var fileStream = new FileStream("lorem.lz4", FileMode.Open))
            using (var lz4Stream = new LZ4Stream(fileStream, LZ4StreamMode.Decompress))
            using (var reader = new StreamReader(lz4Stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                }
            }
        }

        public string CompressToBuffer()
        {
            var text = Enumerable.Repeat(LoremIpsum, 5).Aggregate((a, b) => a + "\n" + b);

            var compressed = Convert.ToBase64String(
                LZ4Codec.Wrap(Encoding.UTF8.GetBytes(text)));

            return compressed;
        }

        public void DecompressBuffer()
        {
            var compressed = CompressToBuffer();
            var lorems =
                Encoding.UTF8.GetString(
                        LZ4Codec.Unwrap(Convert.FromBase64String(compressed)))
                    .Split('\n');

            foreach (var lorem in lorems)
                Console.WriteLine(lorem);
        }

        public void LowLevelCompress()
        {
            var inputBuffer = 
                Encoding.UTF8.GetBytes(
                    Enumerable.Repeat(LoremIpsum, 5).Aggregate((a, b) => a + "\n" + b));

            var inputLength = inputBuffer.Length;
            var maximumLength = LZ4Codec.MaximumOutputLength(inputLength);
            var outputBuffer = new byte[maximumLength];

            var outputLength = LZ4Codec.Encode(
                inputBuffer, 0, inputLength, 
                outputBuffer, 0, maximumLength);

        }
    }
}
