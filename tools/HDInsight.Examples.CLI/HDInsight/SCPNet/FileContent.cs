using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HDInsight.Examples.CLI
{
    public class FileContent : HttpContent
    {
        private const int DefaultBufferSize = 1024 * 64;
        private readonly string _fileName;
        private readonly FileInfo _fileInfo;

        public FileContent(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }
            _fileInfo = new FileInfo(fileName);
            if (!_fileInfo.Exists)
            {
                throw new FileNotFoundException(string.Empty, fileName);
            }
            _fileName = fileName;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _fileInfo.Length;
            return true;
        }

        private FileStream _FileStream;
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            _FileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                                         DefaultBufferSize,
                                         FileOptions.Asynchronous | FileOptions.SequentialScan);

            _FileStream.CopyTo(stream);
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        protected override void Dispose(bool disposing)
        {
            if (_FileStream != null)
            {
                _FileStream.Dispose();
                _FileStream = null;
            }
            base.Dispose(disposing);
        }
    }
}
