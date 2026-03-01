using System;
using System.IO;

namespace bmp_fractal
{
    class Program
    {
        static int resolution = 1000;

        static void Main(string[] args)
        {
            byte[] resBytes = BitConverter.GetBytes(resolution); //convert from int to byte array

            int rowSize = ((resolution * 3 + 3) / 4) * 4;
            int dataSize = resolution * rowSize;
            int fileSize = 54 + dataSize;

            byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
            byte[] dataSizeBytes = BitConverter.GetBytes(dataSize);

            var header = new byte[54] {//Antraštė
                    0x42, 0x4d,
                    fileSizeBytes[0], fileSizeBytes[1], fileSizeBytes[2], fileSizeBytes[3], //file size
                    0x0, 0x0, 0x0, 0x0,
                    0x36, 0x0, 0x0, 0x0,
                    //Antraštės informacija
                    0x28, 0x0, 0x0, 0x0,
                    resBytes[0], resBytes[1], resBytes[2], resBytes[3], //width
                    resBytes[0], resBytes[1], resBytes[2], resBytes[3], //height
                    0x1, 0x0,
                    0x18, 0x0,
                    0x0, 0x0, 0x0, 0x0,
                    dataSizeBytes[0], dataSizeBytes[1], dataSizeBytes[2], dataSizeBytes[3], //data size
                    0x0, 0x0, 0x0, 0x0,
                    0x0, 0x0, 0x0, 0x0,
                    0x0, 0x0, 0x0, 0x0,
                    0x0, 0x0, 0x0, 0x0
            };


            var data = new byte[dataSize]; //array for pixels
            Array.Fill<byte>(data, 0xFF); //fill with white


            using (FileStream file = new FileStream("sample.bmp", FileMode.Create, FileAccess.Write))
            {

                file.Write(header);
                file.Write(data);

                file.Close();
            }
        }
    }
}
