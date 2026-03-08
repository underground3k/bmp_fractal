using System;
using System.IO;
using System.Diagnostics;

namespace bmp_fractal
{
    class Program
    {
        static void Main(string[] args)
        {
            // ── Test 1: vary depth, fixed resolution ─────────────────────────
            int testResolution = 20000;
            int rowStride = ((testResolution * 3 + 3) / 4) * 4;
            byte[] data = new byte[rowStride * testResolution]; // allocate once

            // JIT warmup — run everything once before measuring
            Array.Fill<byte>(data, 255);
            DrawRecursive(0, testResolution / 2.0, testResolution - 1, testResolution / 2.0, 1, data, testResolution);

            Console.WriteLine("=== Test 1: depth (resolution = 20000) ===");
            for (int depth = 1; depth <= 6; depth++)
            {
                Array.Fill<byte>(data, 255); // reset canvas — outside measurement

                var sw = Stopwatch.StartNew();
                DrawRecursive(0, testResolution / 2.0, testResolution - 1, testResolution / 2.0, depth, data, testResolution);
                sw.Stop();

                Console.WriteLine($"depth={depth}  time={sw.Elapsed.TotalMilliseconds:F1}ms");
                SaveBmp(data, testResolution, "sample_depth_" + depth);
            }

            // ── Test 2: vary resolution, auto max depth ───────────────────────
            Console.WriteLine();
            Console.WriteLine("=== Test 2: resolution (auto max depth) ===");
            int[] resolutions = { 100, 400, 1600, 6400, 19000 };

            foreach (int res in resolutions)
            {
                int rs = ((res * 3 + 3) / 4) * 4;
                byte[] d = new byte[rs * res]; // separate buffer per resolution
                Array.Fill<byte>(d, 255);

                int maxDepth = Math.Max(1, (int)(Math.Log(res * 0.4) / Math.Log(4)));

                var sw = Stopwatch.StartNew();
                DrawRecursive(0, res / 2.0, res - 1, res / 2.0, maxDepth, d, res);
                sw.Stop();

                Console.WriteLine($"res={res}x{res}  depth={maxDepth}  time={sw.Elapsed.TotalMilliseconds:F3}ms");
                SaveBmp(d, res, "sample_res_" + res);
            }
        }

        // ── Drawing ───────────────────────────────────────────────────────────

        static void DrawRecursive(double x0, double y0, double x8, double y8, int depth, byte[] data, int resolution)
        {
            if (depth == 0)
            {
                DrawLine(
                    (int)Math.Round(x0), (int)Math.Round(y0),
                    (int)Math.Round(x8), (int)Math.Round(y8),
                    0, 0, 0, data, resolution);
                return;
            }

            double dx = (x8 - x0) / 4.0;
            double dy = (y8 - y0) / 4.0;
            double px = dy;
            double py = -dx;

            double x1 = x0 + dx, y1 = y0 + dy; // right
            double x2 = x1 + px, y2 = y1 + py; // up
            double x3 = x2 + dx, y3 = y2 + dy; // right
            double x4 = x3 - px, y4 = y3 - py; // down
            double x5 = x4 - px, y5 = y4 - py; // down
            double x6 = x5 + dx, y6 = y5 + dy; // right
            double x7 = x6 + px, y7 = y6 + py; // up

            DrawRecursive(x0, y0, x1, y1, depth - 1, data, resolution);
            DrawRecursive(x1, y1, x2, y2, depth - 1, data, resolution);
            DrawRecursive(x2, y2, x3, y3, depth - 1, data, resolution);
            DrawRecursive(x3, y3, x4, y4, depth - 1, data, resolution);
            DrawRecursive(x4, y4, x5, y5, depth - 1, data, resolution);
            DrawRecursive(x5, y5, x6, y6, depth - 1, data, resolution);
            DrawRecursive(x6, y6, x7, y7, depth - 1, data, resolution);
            DrawRecursive(x7, y7, x8, y8, depth - 1, data, resolution);
        }

        static void DrawLine(int x0, int y0, int x1, int y1, byte r, byte g, byte b, byte[] data, int resolution)
        {
            if (x0 == x1)
                for (int y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
                    SetPixel(x0, y, r, g, b, data, resolution);
            else
                for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
                    SetPixel(x, y0, r, g, b, data, resolution);
        }

        static void SetPixel(int x, int y, byte r, byte g, byte b, byte[] data, int resolution)
        {
            if (x < 0 || x >= resolution || y < 0 || y >= resolution) return;
            int flippedY = resolution - 1 - y;
            int idx = (flippedY * resolution + x) * 3;
            data[idx] = b;
            data[idx + 1] = g;
            data[idx + 2] = r;
        }

        // ── File saving ───────────────────────────────────────────────────────

        static void SaveBmp(byte[] data, int resolution, string fileName)
        {
            byte[] resBytes = BitConverter.GetBytes(resolution);
            int rowStride = ((resolution * 3 + 3) / 4) * 4;
            int byteDataSize = rowStride * resolution;
            int fileSize = 54 + byteDataSize;
            byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
            byte[] dataSizeBytes = BitConverter.GetBytes(byteDataSize);

            var header = new byte[54] {
                0x42, 0x4d,
                fileSizeBytes[0], fileSizeBytes[1], fileSizeBytes[2], fileSizeBytes[3],
                0x0, 0x0, 0x0, 0x0,
                0x36, 0x0, 0x0, 0x0,
                0x28, 0x0, 0x0, 0x0,
                resBytes[0], resBytes[1], resBytes[2], resBytes[3],
                resBytes[0], resBytes[1], resBytes[2], resBytes[3],
                0x1, 0x0,
                0x18, 0x0,
                0x0, 0x0, 0x0, 0x0,
                dataSizeBytes[0], dataSizeBytes[1], dataSizeBytes[2], dataSizeBytes[3],
                0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0
            };

            using (FileStream file = new FileStream(fileName + ".bmp", FileMode.Create, FileAccess.Write))
            {
                file.Write(header);
                file.Write(data);
            }
        }
    }
}