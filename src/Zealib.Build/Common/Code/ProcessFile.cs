#define IN_CODE_DOM
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Zealib.IO;

public class ProcessFile : Task
{
    [Required]
    public string InputFile { get; set; }

    [Required]
    public string OutputFile { get; set; }

    [Required]
    public ITaskItem[] Pipelines { get; set; }

    public bool IsEncode { get; set; }

    public override bool Execute()
    {
        var list = new List<Func<Stream, bool, Stream>>();
        foreach (var pipeline in Pipelines)
        {
            var currentPipeline = pipeline;
            switch (pipeline.ItemSpec.ToUpper())
            {
                case "BASE64":
                    list.Add((Stream stream, bool encode) => CreateBase64Stream(currentPipeline, stream, encode));
                    break;
                case "GZIP":
                    list.Add((Stream stream, bool encode) => CreateGZipStream(currentPipeline, stream, encode));
                    break;
                case "XOR":
                    list.Add((Stream stream, bool encode) => CreateXorStream(currentPipeline, stream, encode));
                    break;
                default:
                    Log.LogError("Not supported pipeline \"{0}\".", currentPipeline.ItemSpec);
                    break;
            }
        }
        list.Reverse();

        using (var inStream = File.OpenRead(InputFile))
        using (var outStream = File.Create(OutputFile))
        {
            Stream readStream = inStream;
            Stream writeStream = outStream;

            var allWriteStream = new Stack<Stream>();
            if (IsEncode)
            {
                foreach (var createStream in list)
                {
                    writeStream = createStream(writeStream, true);
                    if (writeStream == null) return false;
                    allWriteStream.Push(writeStream);
                }
            }
            else
            {
                foreach (var createStream in list)
                {
                    readStream = createStream(readStream, false);
                    if (readStream == null) return false;
                }
            }

            int reads = 0;
            byte[] buffer = new byte[4096];
            while ((reads = readStream.Read(buffer, 0, 4096)) > 0)
                writeStream.Write(buffer, 0, reads);

            Stream flushStream;
            while (allWriteStream.Count > 0)
            {
                var stream = allWriteStream.Pop();
                stream.Flush();
                stream.Close();
            }
            readStream.Close();
        }

        return true;
    }

    private Stream CreateBase64Stream(
        ITaskItem item,
        Stream stream, bool encode)
    {
        string lineSizeStr = item.GetMetadata("LineSize");
        int lineSize = 76;
        if (!string.IsNullOrEmpty(lineSizeStr))
        {
            int actualLineSize;
            if (!int.TryParse(lineSizeStr, out actualLineSize))
            {
                Log.LogError("Invalid metadata value Base64.LineSize.");
                return null;
            }

            if (actualLineSize > 10)
                lineSize = actualLineSize;
        }
        return new Base64Stream(stream, encode ? Base64Stream.Mode.Encode : Base64Stream.Mode.Decode)
        {
            OutputLineLength = lineSize
        };
    }

    private Stream CreateGZipStream(
        ITaskItem item,
        Stream stream,
        bool encode)
    {
        return new GZipStream(stream, encode ? CompressionMode.Compress : CompressionMode.Decompress, true);
    }

    private Stream CreateXorStream(
        ITaskItem item,
        Stream stream,
        bool encode)
    {
        string factorsStr = item.GetMetadata("Factors");
        if (string.IsNullOrEmpty(factorsStr))
        {
            Log.LogError("Metadata value Xor.Factors is required.");
            return null;
        }
        byte[] factors = ParseBytes(factorsStr);
        if (factors.Length < 1)
        {
            Log.LogError("Metadata value Xor.Factors size must greater than 1.");
            return null;
        }

        return new XorStream(stream, factors);
    }

    private static byte[] ParseBytes(string value)
    {
        var list = new System.Collections.Generic.List<byte>();
        foreach (string item in value.Split(','))
        {
            var factor = item;
            if (factor.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                factor = factor.Substring(2);
                list.Add(byte.Parse(factor, System.Globalization.NumberStyles.HexNumber));
            }
            else
            {
                list.Add(byte.Parse(factor));
            }
        }
        return list.ToArray();
    }
}
