using System.Collections;

namespace NaturalMerge
{
    class LongReader : IEnumerable<long>, IDisposable
    {
        private BinaryReader Reader { get; set; }
        private long FileStreamLength { get; set; }

        public LongReader(string path)
        {
            Reader = new BinaryReader(File.Open(path, FileMode.OpenOrCreate));
            FileStreamLength = Reader.BaseStream.Length;
        }

        private bool IsEOF()
        {
            return Reader.BaseStream.Position >= FileStreamLength;
        }

        private IEnumerable<long> GetData()
        {
            while (!IsEOF())
            {
                yield return Reader.Read7BitEncodedInt64();
            }
        }

        public long? GetNext()
        {
            if (IsEOF()) return null;
            return Reader.Read7BitEncodedInt64();
        }

        public IEnumerator<long> GetEnumerator()
        {
            return GetData().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetData().GetEnumerator();
        }

        public void Dispose()
        {   
            ((IDisposable)Reader).Dispose();
        }
    }
}