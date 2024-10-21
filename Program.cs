namespace NaturalMerge
{
    internal class Program
    {
        const int hundredMiB = 12_000_000;

        static void Main(string[] args)
        {
            const string dataAPath = "./data/Input.bin";
            const string dataBPath = "./data/Temp1.bin";
            const string dataCPath = "./data/Temp2.bin";
            const string dataOutPath = "./data/Output.txt";


            GenFile(dataAPath, hundredMiB);
            //GenFileChunkSorted(dataAPath, 10 * hundredMiB, (int)1.5 * hundredMiB);
            Console.WriteLine("File was generated. Sorting...");

            int steps = 0;
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            do
            {
                steps++;
                SplitFiles(dataAPath, dataBPath, dataCPath);
            }
            while (!MergeFiles(dataAPath, dataBPath, dataCPath));

            sw.Stop();
            Console.WriteLine($"Time elapsed - {sw.Elapsed}\nTook steps - {steps}");

            ConvertBinToTxt(dataAPath, dataOutPath);

            File.Delete(dataAPath);
            File.Delete(dataBPath);
            File.Delete(dataCPath);
        }


        static void GenFile(string path, int size)
        {
            Random rand = new();
            using BinaryWriter writer = new(File.Open(path, FileMode.Create));
            for (int i = 0; i < size; i++)
            {
                writer.Write7BitEncodedInt64(rand.NextInt64());
            }
        }

        static void GenFileChunkSorted(string path, int size, int chunkSize)
        {
            Random rand = new();
            using BinaryWriter writer = new(File.Open(path, FileMode.Create));
            List<long> chunk = [];
            int written = 0;

            while (written < size)
            {
                for (int i = 0; i < chunkSize && written < size; i++)
                {
                    chunk.Add(rand.NextInt64());
                    written++;
                }
                chunk.Sort();
                foreach (var number in chunk)
                {
                    writer.Write7BitEncodedInt64(number);
                }
                chunk.Clear();
            }            
        }


        static void ConvertBinToTxt(string pathBin, string pathTxt)
        {
            using var binReader = new BinaryReader(File.Open(pathBin, FileMode.OpenOrCreate));
            using var txtWriter = new StreamWriter(pathTxt);
            var streamLength = binReader.BaseStream.Length;
            while (binReader.BaseStream.Position < streamLength)
            {
                txtWriter.WriteLine(binReader.Read7BitEncodedInt64());
            }
        }


        static void SplitFiles(string pathA, string pathB, string pathC)
        {
            int writer = 0;

            using ChunkReader readerA = new(pathA, hundredMiB);
            using BinaryWriter writerB = new(File.Open(pathB, FileMode.Create));
            using BinaryWriter writerC = new(File.Open(pathC, FileMode.Create));
            BinaryWriter[] writers = [writerB, writerC];

            long? numberOld = readerA.GetNext();
            if (numberOld is null) return;
            writers[writer].Write7BitEncodedInt64((long)numberOld);

            foreach (var number in readerA)
            {
                long numberNew = number;
                if (numberNew < numberOld)
                {
                    writers[writer].Write7BitEncodedInt64(-1);
                    writer = (writer + 1) % 2;
                }

                writers[writer].Write7BitEncodedInt64(numberNew);
                numberOld = numberNew;
            }
        }


        static bool MergeFiles(string pathA, string pathB, string pathC)
        {
            using BinaryWriter writerA = new(File.Open(pathA, FileMode.Create));
            using ChunkReader readerB = new(pathB, hundredMiB);
            using ChunkReader readerC = new(pathC, hundredMiB);

            long? dataB;
            long? dataC;

            dataB = readerB.GetNext();
            dataC = readerC.GetNext();

            bool sorted = dataB is null || dataC is null;

            while (dataB is not null || dataC is not null)
            {
                if (dataC is null || dataB != -1 && dataC == -1)
                {
                    writerA.Write7BitEncodedInt64((long)dataB);
                    dataB = readerB.GetNext();
                }
                else if (dataB is null || dataB == -1 && dataC != -1)
                {
                    writerA.Write7BitEncodedInt64((long)dataC);
                    dataC = readerC.GetNext();
                }
                else
                {
                    if (dataB < dataC)
                    {
                        writerA.Write7BitEncodedInt64((long)dataB);
                        dataB = readerB.GetNext();
                    }
                    else if (dataB > dataC)
                    {
                        writerA.Write7BitEncodedInt64((long)dataC);
                        dataC = readerC.GetNext();
                    }

                }

                if (dataB == -1 && dataC == -1)
                {
                    dataB = readerB.GetNext();
                    dataC = readerC.GetNext();
                }
            }
            return sorted;
        }
    }
}