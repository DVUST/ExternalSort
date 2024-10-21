using System.IO;

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

            Console.WriteLine("Generating file...");
            GenFile(dataAPath, 10*hundredMiB);
            Console.WriteLine("Pre-sorting...");
            PrepSort(dataAPath, (int)1.5*hundredMiB);

            Console.WriteLine("Sorting...");
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

            Console.WriteLine("Converting to txt...");
            ConvertBinToTxt(dataAPath, dataOutPath);

            Console.WriteLine("Deleting temporary files...");
            File.Delete(dataAPath);
            File.Delete(dataBPath);
            File.Delete(dataCPath);

            Console.WriteLine("Completed!");
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


        static void ConvertBinToTxt(string pathBin, string pathTxt)
        {
            using var binReader = new LongReader(pathBin);
            using var txtWriter = new StreamWriter(pathTxt);
            foreach (var number in binReader)
            {
                txtWriter.WriteLine(number);
            }
        }


        static void SplitFiles(string pathA, string pathB, string pathC)
        {
            int writerIdx = 0;

            using LongReader readerA = new(pathA);
            using BinaryWriter writerB = new(File.Open(pathB, FileMode.Create));
            using BinaryWriter writerC = new(File.Open(pathC, FileMode.Create));
            BinaryWriter[] writers = [writerB, writerC];

            long? numberOld = readerA.GetNext();
            if (numberOld is null) return;
            writers[writerIdx].Write7BitEncodedInt64((long)numberOld);

            foreach (var number in readerA)
            {
                long numberNew = number;
                if (numberNew < numberOld)
                {
                    writers[writerIdx].Write7BitEncodedInt64(-1);
                    writerIdx = (writerIdx + 1) % 2;
                }

                writers[writerIdx].Write7BitEncodedInt64(numberNew);
                numberOld = numberNew;
            }
        }


        static bool MergeFiles(string pathA, string pathB, string pathC)
        {
            using BinaryWriter writerA = new(File.Open(pathA, FileMode.Create));
            using LongReader readerB = new(pathB);
            using LongReader readerC = new(pathC);

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


        static void PrepSort(string path, int chunkSize = hundredMiB)
        {
            string tempPath = "./data/temp.bin";
            int capacity = 0;
            List<long> chunk = [];
            using (LongReader reader = new(path))
            using (BinaryWriter writer = new(File.Open(tempPath, FileMode.Create)))
            {
                foreach (var number in reader)
                {
                    chunk.Add(number);
                    capacity++;
                    if (capacity >= chunkSize)
                    {
                        SortAndWrite(writer, chunk);
                        capacity = 0;
                        chunk.Clear();
                    }
                }
                SortAndWrite(writer, chunk);
            }
            File.Move(tempPath, path, true);
        }


        static void SortAndWrite(BinaryWriter writer, List<long> data)
        {
            data.Sort();
            foreach (var number in data)
            {
                writer.Write7BitEncodedInt64(number);
            }
        }
    }
}