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
            byte[] data = new byte[rowStride * testResolution];

            // JIT warmup
            Array.Fill<byte>(data, 255);
            long wa = 0, wm = 0, wc = 0;
            DrawRecursive(0, testResolution / 2.0, testResolution - 1, testResolution / 2.0,
                          1, data, testResolution, ref wa, ref wm, ref wc);

            Console.WriteLine("=== Test 1: depth (resolution = 20000) ===");
            Console.WriteLine($"{"Depth",-7} {"Add/Sub",-13} {"Mul/Div",-13} {"Cmp",-13} {"Total",-13} {"Time (ms)"}");
            Console.WriteLine(new string('-', 72));

            for (int depth = 1; depth <= 6; depth++)
            {
                Array.Fill<byte>(data, 255);
                long opAdd = 0, opMul = 0, opCmp = 0;

                var sw = Stopwatch.StartNew();
                DrawRecursive(0, testResolution / 2.0, testResolution - 1, testResolution / 2.0,
                              depth, data, testResolution, ref opAdd, ref opMul, ref opCmp);
                sw.Stop();

                long total = opAdd + opMul + opCmp;
                Console.WriteLine($"{depth,-7} {opAdd,-13:N0} {opMul,-13:N0} {opCmp,-13:N0} {total,-13:N0} {sw.Elapsed.TotalMilliseconds:F1}");
                SaveBmp(data, testResolution, "sample_depth_" + depth);
            }

            // ── Test 2: vary resolution, auto max depth ───────────────────────
            Console.WriteLine();
            Console.WriteLine("=== Test 2: resolution (auto max depth) ===");
            Console.WriteLine($"{"Res",-12} {"Depth",-7} {"Add/Sub",-13} {"Mul/Div",-13} {"Cmp",-13} {"Total",-13} {"Time (ms)"}");
            Console.WriteLine(new string('-', 88));

            int[] resolutions = { 100, 400, 1600, 6400, 19000 };

            foreach (int res in resolutions)
            {
                int rs = ((res * 3 + 3) / 4) * 4;
                byte[] d = new byte[rs * res];
                Array.Fill<byte>(d, 255);

                int maxDepth = Math.Max(1, (int)(Math.Log(res * 0.4) / Math.Log(4)));
                long opAdd = 0, opMul = 0, opCmp = 0;

                var sw = Stopwatch.StartNew();
                DrawRecursive(0, res / 2.0, res - 1, res / 2.0,
                              maxDepth, d, res, ref opAdd, ref opMul, ref opCmp);
                sw.Stop();

                long total = opAdd + opMul + opCmp;
                Console.WriteLine($"{res + "x" + res,-12} {maxDepth,-7} {opAdd,-13:N0} {opMul,-13:N0} {opCmp,-13:N0} {total,-13:N0} {sw.Elapsed.TotalMilliseconds:F3}");
                SaveBmp(d, res, "sample_res_" + res);
            }
        }

        // ── Drawing ───────────────────────────────────────────────────────────

        static void DrawRecursive(double x0, double y0, double x8, double y8,
                                   int depth, byte[] data, int resolution,
                                   ref long opAdd, ref long opMul, ref long opCmp)
        {
            opCmp++;                                    // depth == 0
            if (depth == 0)
            {
                DrawLine((int)Math.Round(x0), (int)Math.Round(y0),
                         (int)Math.Round(x8), (int)Math.Round(y8),
                         0, 0, 0, data, resolution, ref opAdd, ref opMul, ref opCmp);
                return;
            }

            double dx = (x8 - x0) / 4.0; opAdd++; opMul++; // subtraction + division
            double dy = (y8 - y0) / 4.0; opAdd++; opMul++; // subtraction + division
            double px = dy;
            double py = -dx; opAdd++;           // negation

            double x1 = x0 + dx; opAdd++; double y1 = y0 + dy; opAdd++; // right
            double x2 = x1 + px; opAdd++; double y2 = y1 + py; opAdd++; // up
            double x3 = x2 + dx; opAdd++; double y3 = y2 + dy; opAdd++; // right
            double x4 = x3 - px; opAdd++; double y4 = y3 - py; opAdd++; // down
            double x5 = x4 - px; opAdd++; double y5 = y4 - py; opAdd++; // down
            double x6 = x5 + dx; opAdd++; double y6 = y5 + dy; opAdd++; // right
            double x7 = x6 + px; opAdd++; double y7 = y6 + py; opAdd++; // up

            DrawRecursive(x0, y0, x1, y1, depth - 1, data, resolution, ref opAdd, ref opMul, ref opCmp); opAdd++;
            DrawRecursive(x1, y1, x2, y2, depth - 1, data, resolution, ref opAdd, ref opMul, ref opCmp); opAdd++;
            DrawRecursive(x2, y2, x3, y3, depth - 1, data, resolution, ref opAdd, ref opMul, ref opCmp); opAdd++;
            DrawRecursive(x3, y3, x4, y4, depth - 1, data, resolution, ref opAdd, ref opMul, ref opCmp); opAdd++;
            DrawRecursive(x4, y4, x5, y5, depth - 1, data, resolution, ref opAdd, ref opMul, ref opCmp); opAdd++;
            DrawRecursive(x5, y5, x6, y6, depth - 1, data, resolution, ref opAdd, ref opMul, ref opCmp); opAdd++;
            DrawRecursive(x6, y6, x7, y7, depth - 1, data, resolution, ref opAdd, ref opMul, ref opCmp); opAdd++;
            DrawRecursive(x7, y7, x8, y8, depth - 1, data, resolution, ref opAdd, ref opMul, ref opCmp); opAdd++;
        }

        static void DrawLine(int x0, int y0, int x1, int y1,
                              byte r, byte g, byte b, byte[] data, int resolution,
                              ref long opAdd, ref long opMul, ref long opCmp)
        {
            opCmp++;                        // x0 == x1
            if (x0 == x1)
            {
                int yMin = Math.Min(y0, y1);
                int yMax = Math.Max(y0, y1);
                for (int y = yMin; y <= yMax; y++)
                {
                    opCmp++; opAdd++;       // loop condition + y++
                    SetPixel(x0, y, r, g, b, data, resolution, ref opAdd, ref opMul, ref opCmp);
                }
                opCmp++;                    // final loop exit check
            }
            else
            {
                int xMin = Math.Min(x0, x1);
                int xMax = Math.Max(x0, x1);
                for (int x = xMin; x <= xMax; x++)
                {
                    opCmp++; opAdd++;       // loop condition + x++
                    SetPixel(x, y0, r, g, b, data, resolution, ref opAdd, ref opMul, ref opCmp);
                }
                opCmp++;                    // final loop exit check
            }
        }

        static void SetPixel(int x, int y, byte r, byte g, byte b, byte[] data, int resolution,
                              ref long opAdd, ref long opMul, ref long opCmp)
        {
            opCmp += 4;                     // x<0, x>=res, y<0, y>=res
            if (x < 0 || x >= resolution || y < 0 || y >= resolution) return;

            int flippedY = resolution - 1 - y; opAdd += 2; // resolution-1, then -y
            int idx = (flippedY * resolution + x) * 3; opMul += 2; opAdd++; // *res, +x, *3

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