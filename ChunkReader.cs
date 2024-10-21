using System.Collections;

namespace NaturalMerge
{
    class ChunkReader : IEnumerable<long>, IDisposable
    {
        private long ChunkSize { get; set; }
        private long[] Data { get; set; }
        private BinaryReader Reader { get; set; }
        private int Capacity { get; set; }
        private int Position { get; set; }
        private long FileStreamLength { get; set; }

        public ChunkReader(string path, long chunkSize)
        {
            ChunkSize = chunkSize;
            Data = new long[chunkSize];
            Reader = new BinaryReader(File.Open(path, FileMode.OpenOrCreate));
            FileStreamLength = Reader.BaseStream.Length;
            Position = 0;
            RefillData();
        }

        private void RefillData()
        {
            Capacity = 0;
            Position = 0;
            for (int i = 0; i < ChunkSize && !IsEOF(); i++)
            {
                Data[i] = Reader.Read7BitEncodedInt64();
                Capacity++;
            }
        }


        private bool IsEOF()
        {
            return Reader.BaseStream.Position >= FileStreamLength;
        }

        private bool IsEOChunk()
        {
            return Position >= Capacity;
        }

        private IEnumerable<long> GetData()
        {
            while (!IsEOChunk() || !IsEOF())
            {
                if (IsEOChunk()) RefillData();
                yield return Data[Position++];
            }
        }

        public long? GetNext()
        {
            if (IsEOChunk() && IsEOF()) return null;
            if (IsEOChunk()) RefillData();

            return Data[Position++];
        }

        public IEnumerator<long> GetEnumerator()
        {
            return GetData().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        public void Dispose()
        {   
            ((IDisposable)Reader).Dispose();
        }
    }
}