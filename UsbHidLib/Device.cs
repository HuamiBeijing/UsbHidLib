using System;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace UsbHidLib
{
   public class Device : IDisposable
   {

      /* device handle */
      readonly SafeFileHandle _shandle;

      /* stream */
      private FileStream _fileStream;

      /* dispose */
      public void Dispose()
      {
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

      /* open hid device */
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

      public ushort GetPreparsedPacketSize()
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

      /* read record */
      public void Read(byte[] data)
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
   }
}
