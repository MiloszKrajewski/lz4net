using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace LZ4.Tests.Continuous
{
	[XmlRoot("Results")]
	public class Results
	{
		[XmlElement("LastImprovement")]
		public DateTime LastImprovement;

		[XmlElement("Result")]
		public List<ResultsItem> Items = new List<ResultsItem>();

		public bool Update(string codec, double compressionSpeed, double decompressionSpeed, long inputBytes, long outputBytes)
		{
			var item = Items.FirstOrDefault(r => r.Codec == codec);
			var result = false;

			if (item == null)
			{
				item = new ResultsItem();
				item.Codec = codec;
				item.CompressionSpeed = compressionSpeed;
				item.DecompressionSpeed = decompressionSpeed;
				item.Age = 0;
				item.AverageCompressionSpeed = compressionSpeed;
				item.AverageDecompressionSpeed = decompressionSpeed;
				item.Cycles = 1;
				item.InputBytes = inputBytes;
				item.OutputBytes = outputBytes;
				item.Ratio = item.OutputBytes * 100 / item.InputBytes;
				Items.Add(item);
				result = true;
			}
			else
			{
				item.Age++;
				if (compressionSpeed > item.CompressionSpeed)
				{
					item.CompressionSpeed = compressionSpeed;
					item.Age = 0;
					result = true;
				}
				if (decompressionSpeed > item.DecompressionSpeed)
				{
					item.DecompressionSpeed = decompressionSpeed;
					item.Age = 0;
					result = true;
				}

				var cycles = item.Cycles;
				item.AverageCompressionSpeed = (item.AverageCompressionSpeed * cycles + compressionSpeed) / (cycles + 1);
				item.AverageDecompressionSpeed = (item.AverageDecompressionSpeed * cycles + decompressionSpeed) / (cycles + 1);
				item.Cycles = cycles + 1;
				item.InputBytes += inputBytes;
				item.OutputBytes += outputBytes;
				item.Ratio = item.OutputBytes * 100 / item.InputBytes;
			}

			if (result) LastImprovement = DateTime.Now;
			return result;
		}

		public void SaveAsCSV(string fileName)
		{
			using (var writer = new StreamWriter(fileName))
			{
				writer.WriteLine("Codec,CompressionSpeed,DecompressSpeed,AvgCompressionSpeed,AvgDecompressionSpeed,Ratio");
				foreach (var result in Items.OrderBy(r => r.Codec.ToLower()))
				{
					writer.WriteLine("{0},{1},{2},{3},{4},{5}",
						result.Codec,
						result.CompressionSpeed,
						result.DecompressionSpeed,
						result.AverageCompressionSpeed,
						result.AverageDecompressionSpeed,
						result.Ratio);
				}
			}
		}
	}

	[XmlRoot("Result")]
	public class ResultsItem
	{
		public string Codec;
		public double CompressionSpeed;
		public double DecompressionSpeed;
		public int Age;
		public double AverageCompressionSpeed;
		public double AverageDecompressionSpeed;
		public int Cycles;
		public double InputBytes;
		public double OutputBytes;
		public double Ratio;
	}
}
