using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Media;

namespace ConsoleApp1
{
    public class Program
    {
#pragma warning disable CS8618 
        public static (char, ulong, ulong)[] ASCIIsarr;
        public static ulong[] ASCIIsarr1;
        public static ulong[] ASCIIsarr2;
#pragma warning restore CS8618 
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void Main()
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = false });
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("this program only works on windows. sorry!");
                Console.Out.Flush();
                throw new PlatformNotSupportedException();
            }
            LoadASCIIs();
            Console.Clear();
            //Console.WriteLine("Press any key to start.");
            Console.Out.Flush();
            Console.ReadKey();
            Console.Clear();
            Stopwatch sw = new();
            sw.Start();
            SoundPlayer soundPlayer = new("sound.wav");
            soundPlayer.Load();
            soundPlayer.Play();
            long nowframe = 0;
            int skipped = 0;
            int max = Directory.GetFiles("./images").Length;
            int rendered = 0;
            bool flag = false;
            while ((sw.ElapsedMilliseconds * 60) / 1000 < max-5)
            {
                if (nowframe +1 < (sw.ElapsedMilliseconds * 60) / 1000 + 1)
                {
                    skipped++;
                }
                rendered++;
                nowframe = (sw.ElapsedMilliseconds * 60) / 1000 + 1;
                Console.CursorTop = 0;
                ShowAA(string.Format("./images/{0}.jpg", (sw.ElapsedMilliseconds * 60) / 1000 + 1),512);// flag?512:384);
                //Console.ForegroundColor = (ConsoleColor)(flag ? 15 :7);
                //flag = !flag;
                //Console.WriteLine(string.Format("{1}frame", (sw.ElapsedMilliseconds * 60) / 1000 / 60, (sw.ElapsedMilliseconds * 60) / 1000));
                //Console.WriteLine(sw.Elapsed.ToString());
                //Console.WriteLine("skipped frames:{0}",skipped);
                //Console.WriteLine("converted frames:{0}", rendered);
                //Console.WriteLine("Average Frame per second:{0}",rendered/sw.Elapsed.TotalSeconds);
                Console.Out.Flush();
            }
            //Console.WriteLine(sw.Elapsed.TotalMicroseconds / rendered);
            //Console.WriteLine("Press any key to exit");
            Console.Out.Flush();
            Console.ReadKey();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void ShowAA(string path,int bound)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException();
            }
            Bitmap img = (Bitmap)Image.FromFile(path);
            int divheight = (img.Height - 1) / 16 + 1;
            int divwidth = (img.Width - 1) / 8 + 1;
            ulong[][] bytesarr = new ulong[divheight*16][];
            //*
            int imgh = img.Height;
            int imgw = img.Width;
            BitmapData bmpData = img.LockBits(new Rectangle(0, 0, imgw, imgh), ImageLockMode.ReadOnly, img.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            if (img.PixelFormat == PixelFormat.Format24bppRgb)
            {
                Parallel.For(0, imgh, i => {
                    int wid = i * 3 * imgw;
                    bytesarr[i] = new ulong[divwidth];
                    for (int j = 0; j < imgw; j++)
                    {
                        unsafe
                        {
                            byte* adr = (byte*)ptr;
                            bytesarr[i][j>>3] |= ((int)adr[wid + j*3]+ (int)adr[wid + j * 3 + 1]+ (int)adr[wid + j * 3 + 2]) >= bound ? (byte)(1 << ((j & 0x7)^0x7)) : (byte)0;
                        }
                    }
                });
            }
            else if (img.PixelFormat == PixelFormat.Format32bppArgb)
            {
                Parallel.For(0, imgh, i => {
                    int wid = i * 4 * imgw;
                    bytesarr[i] = new ulong[divwidth];
                    for (int j = 0; j < imgw; j++)
                    {
                        unsafe
                        {
                            byte* adr = (byte*)ptr;
                            bytesarr[i][j >> 3] |= ((int)adr[wid + j * 4 +1] + (int)adr[wid + j * 4 + 2] + (int)adr[wid + j * 4 + 3]) >= bound ? (byte)(1 << ((j & 0x7) ^ 0x7)) : (byte)0;
                        }
                    }
                });
            }
            else
            {
                Console.WriteLine("Image Format Not Supported");
                return ;
            }
            Parallel.For(imgh, divheight * 16, i => { bytesarr[i] = new ulong[divwidth]; });
            img.UnlockBits(bmpData);
            //縦は16px横は8pxごとに区切る
            long height = img.Height;
            long width = img.Width;
            img.Dispose();
            //*/
            Parallel.For(imgh, divheight * 16, i => { bytesarr[i] = new ulong[divwidth]; });
            ulong[][] converted1 = new ulong[divheight][];
            ulong[][] converted2 = new ulong[divheight][];
            Parallel.For(0, divheight, i =>
            {
                //ここはVectorで回すと逆に遅くなる
                converted1[i] = new ulong[divwidth];
                converted2[i] = new ulong[divwidth];
                for (int j = 0; j < divwidth; j++)
                {
                    ulong ul1 = 0;
                    ulong ul2 = 0;
                    for (int k = 0; k < 8; k++)
                    {
                        ul1 <<= 8;
                        ul2 <<= 8;
                        ul1 |= bytesarr[(i << 4) | k][j];
                        ul2 |= bytesarr[(i << 4) | k | 8][j];
                    }
                    converted1[i][j] = ul1;
                    converted2[i][j] = ul2;
                }
            });
            StringBuilder sb = new();
            var results = new char[divheight][];
            Parallel.For(0, divheight, i => {
                //ここはVectorで回すと逆に遅くなる
                results[i] = new char[divwidth];
                for (int j = 0; j < divwidth; j++)
                {
                    ulong i1 = converted1[i][j];
                    ulong i2 = converted2[i][j];
                    /*画像側はbitが立ってると明るい
                    スペースは全ピクセルが暗い = 0
                    bitが立ってると明るくなる（黒背景に白文字なので）
                    */
                    ulong cost = Popcnt.X64.PopCount(i1) + Popcnt.X64.PopCount(i2);
                    char chr = ' ';
                    for (int k = 33; k < 127; k++)
                    {
                        ulong tempcost = Popcnt.X64.PopCount(ASCIIsarr1[k] ^ i1) + Popcnt.X64.PopCount(ASCIIsarr2[k] ^ i2);
                        if (cost > tempcost)
                        {
                            cost = tempcost;
                            chr = (char)k;
                        }
                    }
                    results[i][j] = chr;
                }
            });
            for (int i = 0; i < divheight; i++)
            {
                sb.Append(results[i]);
                sb.AppendLine();
            }
            Console.WriteLine(sb.ToString());
            return ;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void LoadASCIIs()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException();
            }
            var files = Directory.EnumerateFiles(".\\ASCIIimages");
            ASCIIsarr = new (char, UInt64, UInt64)[files.Count()];
            ASCIIsarr1 = new ulong[127];
            ASCIIsarr2 = new ulong[127];
            int index = 0;
            foreach (var file in files)
            {
                Bitmap img = (Bitmap)Image.FromFile(file);
                string[] splitted = file.Split('\\');
                var c = (char)int.Parse(splitted[^1].Split('.')[0]);
                UInt64 ul1 = 0;
                UInt64 ul2 = 0;
                //文字は画像が暗い方のbitを立たせる
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        ul1 <<= 1;
                        ul1 |= img.GetPixel(j, i).GetBrightness() > 0.5 ? 0 : 1u;
                    }
                }
                for (int i = 8; i < 16; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        ul2 <<= 1;
                        ul2 |= img.GetPixel(j, i).GetBrightness() > 0.5 ? 0 : 1u;
                    }
                }
                ASCIIsarr[index] = (c, ul1,ul2);
                ASCIIsarr1[c] = ul1;
                ASCIIsarr2[c] = ul2;
                index++;
                img.Dispose();
            }
        }
    }

}
