using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace UsbHidLib
{
   /// <summary>
   /// USB HID device simple access class.
   /// </summary>
   public sealed class Device : IDisposable
   {

      /// <summary>
      /// Device handle for HID.
      /// </summary>
      readonly SafeFileHandle _shandle;

      /// <summary>
      /// File stream opened using <see cref="_shandle"/>.
      /// </summary>
      private FileStream _fileStream;

      private CancellationTokenSource _cancellationTokenSource;

      private Task  _readerTask;

      private Action<byte[]> _onRecievedAction;

      /// <summary>
      /// Dispose.
      /// </summary>
      public void Dispose()
      {
         if (_readerTask != null)
         {
            _cancellationTokenSource.Cancel();
            _readerTask.Wait();
         }
         /* deal with file stream */
         if (_fileStream != null)
         {
            /* close stream */
            _fileStream.Close();
            /* get rid of object */
            _fileStream = null;
         }

         /* close handle */
         _shandle.Dispose();
      }

      /// <summary>
      /// Creates new Device.
      /// </summary>
      /// <param name="path">HID device path.</param>
      public Device(string path)
      {
         /* opens hid device file */
         var handle = Native.CreateFile(path,
             Native.GENERIC_READ | Native.GENERIC_WRITE,
             Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE,
             IntPtr.Zero, Native.OPEN_EXISTING, Native.FILE_FLAG_OVERLAPPED,
             IntPtr.Zero);

         /* whops */
         if (handle == Native.INVALID_HANDLE_VALUE)
         {
            throw new Exception("Could not open HID device");
         }

         /* build up safe file handle */
         _shandle = new SafeFileHandle(handle, false);

         /* prepare stream - async */
         _fileStream = new FileStream(_shandle, FileAccess.ReadWrite, 64, true);
      }

      /// <summary>
      /// Write record.
      /// </summary>
      /// <param name="data">Data to write.</param>
      public void Write(byte[] data)
      {
         /* write some bytes */
         _fileStream.Write(data, 0, data.Length);
         /* flush! */
         _fileStream.Flush();
      }

      internal ushort GetPreparsedPacketSize()
      {
         IntPtr preparsed = IntPtr.Zero;
         if (!Native.HidD_GetPreparsedData(_shandle, ref preparsed))
         {
            return 0;
         }

         var caps = new HIDP_CAPS();
         Native.HidP_GetCaps(preparsed, ref caps);
         var res = caps.InputReportByteLength;
         Native.HidD_FreePreparsedData(ref preparsed);
         return res;
      }

      /// <summary>
      /// Read record.
      /// </summary>
      /// <param name="data">Data to read.</param>
      internal void Read(byte[] data)
      {
         /* get number of bytes */
         int n = 0, bytes = data.Length;

         /* read buffer */
         while (n != bytes)
         {
            /* read data */
            int rc = _fileStream.Read(data, n, bytes - n);
            /* update pointers */
            n += rc;
         }
      }

      /// <summary>
      /// Starts async reading.
      /// </summary>
      /// <param name="receiver">Readed data consumer.</param>
      public void StartReading(Action<byte[]> receiver)
      {
         if (_readerTask != null)
         {
            return; // Already started.
         }
         _onRecievedAction = receiver;

         _cancellationTokenSource = new CancellationTokenSource();
         _readerTask = new Task(reader, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
         _readerTask.Start();
      }

      /// <summary>
      /// Reader task.
      /// </summary>
      private void reader()
      {
         while (true)
         {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
               return;
            }
            var size = GetPreparsedPacketSize();
            if (size == 0)
            {
               continue;
            }
            var data = new byte[size];
            Read(data);
            _onRecievedAction(data);
         }
      }
   }
}
