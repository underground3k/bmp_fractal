using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace bmp_fractal
{
    class Program
    {
        static int resolution = 2000;

        static void Main(string[] args)
        {

            for (int i = 0; i < 5; i++)
            {
                string fileName = "sample_" + i;
                GenerateBmp(resolution, 0+i, fileName);
            }



        }

        public static void GenerateBmp(int resolution, int depth, string FileName)
        {
            byte[] data = Array.Empty<byte>();

            if (depth == 0) //if depth = 0, depth mode = max depth
            {
                double initialSide = resolution * 0.4;

                depth = Math.Max(1, (int)(Math.Log(initialSide) / Math.Log(4)));//max depth
            }

            byte[] resBytes = BitConverter.GetBytes(resolution);
            int rowStride = ((resolution * 3 + 3) / 4) * 4; //calculated with padding
            int byteDataSize = rowStride * resolution;
            int fileSize = 54 + byteDataSize;
            byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
            byte[] dataSizeBytes = BitConverter.GetBytes(byteDataSize);

            var header = new byte[54] { //header
                    0x42, 0x4d,
                    fileSizeBytes[0], fileSizeBytes[1], fileSizeBytes[2], fileSizeBytes[3], //file size
                    0x0, 0x0, 0x0, 0x0,
                    0x36, 0x0, 0x0, 0x0,
                    //header info
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

            data = new byte[byteDataSize];
            Array.Fill<byte>(data, (byte)255); //fills picture with white color

            //y middle point
            double cy = resolution / 2.0;

            DrawRecursive(0, cy, resolution - 1, cy, depth, data);

            string fullFileName = FileName + ".bmp";
            using (FileStream file = new FileStream(fullFileName, FileMode.Create, FileAccess.Write))
            {
                file.Write(header);
                file.Write(data);
                file.Close();
            }
        }

        //rekurentine lygtis:
        //T(n) = 8 * T(n-1) + c, T(0) = k
        //n- rekursijos gylis
        //c- fiksuotos operacijos viename nelapiniame mazge (25 sudetys, 2 dalybos, 1 salyga)
        //DrawLine operacijos baziniame lygyje
        static void DrawRecursive(double x0, double y0, double x8, double y8, int depth, byte[] data)
        {
            if (depth == 0) //base case, draw a straight line
            {
                DrawLine(
                    (int)Math.Round(x0), (int)Math.Round(y0),
                    (int)Math.Round(x8), (int)Math.Round(y8),
                    0, 0, 0, data);
                return;
            }

            //direction vectors
            double dx = (x8 - x0) / 4.0;
            double dy = (y8 - y0) / 4.0;

            //vector rotation by 90 degrees
            double px = dy;
            double py = -dx;

            double x1 = x0 + dx, y1 = y0 + dy; //right
            double x2 = x1 + px, y2 = y1 + py; //up
            double x3 = x2 + dx, y3 = y2 + dy; //right
            double x4 = x3 - px, y4 = y3 - py; //down
            double x5 = x4 - px, y5 = y4 - py; //down
            double x6 = x5 + dx, y6 = y5 + dy; //right
            double x7 = x6 + px, y7 = y6 + py; //up

            DrawRecursive(x0, y0, x1, y1, depth - 1, data);
            DrawRecursive(x1, y1, x2, y2, depth - 1, data);
            DrawRecursive(x2, y2, x3, y3, depth - 1, data);
            DrawRecursive(x3, y3, x4, y4, depth - 1, data);
            DrawRecursive(x4, y4, x5, y5, depth - 1, data);
            DrawRecursive(x5, y5, x6, y6, depth - 1, data);
            DrawRecursive(x6, y6, x7, y7, depth - 1, data);
            DrawRecursive(x7, y7, x8, y8, depth - 1, data);
        }

        static void DrawLine(int x0, int y0, int x1, int y1, byte r, byte g, byte b, byte[] data)
        {
            if (x0 == x1) //vertical
                for (int y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
                    SetPixel(x0, y, r, g, b, data);
            else //horizontal
                for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
                    SetPixel(x, y0, r, g, b, data);
        }

        static void SetPixel(int x, int y, byte r, byte g, byte b, byte[] data)
        {
            if (x < 0 || x >= resolution || y < 0 || y >= resolution) return; //out of bounds
            int flippedY = resolution - 1 - y; // flip Y
            int idx = (flippedY * resolution + x) * 3; //index of pixel
            data[idx] = b;
            data[idx + 1] = g;
            data[idx + 2] = r;
        }
    }
}