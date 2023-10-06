using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleApp1
{
    public class Program
    {
        public static (char, UInt64,UInt64)[] ASCIIsarr;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void Main(string[] args)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException();
            }
            LoadASCIIs();
            Console.ReadKey();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            for (int i = 1; i <= 13149; i++)
            {
                ShowAA(string.Format("./ba/{0:0000}.png",i));
                Console.WriteLine(string.Format("{0}sec({1}flame)",i/60,i));
                Console.WriteLine(stopwatch.Elapsed.ToString());
                //Thread.Sleep(1);//たぶんここのオーバーヘッドがデカい 1ms待機のはずが10msくらい待機してる
                Task.Delay(1).Wait();//表示が何故かおかしくなる
            }
            Console.ReadKey();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void ShowAA(string path)
        {
            Bitmap img = (Bitmap)Image.FromFile(path);
            long divheight = (img.Height - 1) / 16 + 1;
            long divwidth = (img.Width - 1) / 8 + 1;
            bool[][] bools = new bool[divheight * 16][];
            int imgh = img.Height;
            int imgw = img.Width;
            Rectangle rect = new Rectangle(0, 0, imgw, imgh);
            BitmapData bmpData = img.LockBits(rect, ImageLockMode.ReadOnly, img.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * imgh;
            byte[] rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            if (img.PixelFormat == PixelFormat.Format24bppRgb)
            {
                Parallel.For(0, imgh, i => {
                    int wid = i * 3 * imgw;
                    bools[i] = new bool[divwidth * 8];
                    for (int j = 0; j < imgw; j++)
                    {
                        bools[i][j] = ((int)rgbValues[wid + j * 3]
                            + (int)rgbValues[wid + j * 3 + 1]
                            + (int)rgbValues[wid + j * 3 + 2]) >= 384;
                    }
                });
            }
            else if (img.PixelFormat == PixelFormat.Format32bppArgb)
            {
                Parallel.For(0, imgh, i => {
                    bools[i] = new bool[divwidth * 8];
                    for (int j = 0; j < imgw; j++)
                    {
                        bools[i][j] = ((int)rgbValues[i * 4 * imgw + j * 4 + 1]
                            + (int)rgbValues[i * 4 * imgw + j * 4 + 2]
                            + (int)rgbValues[i * 4 * imgw + j * 4 + 3]) >= 384;
                    }
                });
            }
            else
            {
                Console.WriteLine("Image Format Not Supported");
                return;
            }
            Parallel.For(imgh, divheight * 16, i => { bools[i] = new bool[divwidth * 8]; });
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            img.UnlockBits(bmpData);
            //縦は16px横は8pxごとに区切る
            long height = img.Height;
            long width = img.Width;
            img.Dispose();
            (UInt64,UInt64)[][] converted = new (UInt64,UInt64)[divheight][];
            Parallel.For(0, divheight, i =>
            {
                converted[i] = new (UInt64, UInt64)[divwidth];
                for (int j = 0; j < divwidth; j++)
                {
                    ulong ul1 = 0;
                    ulong ul2 = 0;
                    for (int k = 0; k < 8; k++)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            ul1 <<= 1;
                            ul1 |= (byte)(bools[i * 16 + k][j * 8 + l] ? 1 : 0);
                        }
                    }
                    for (int k = 8; k < 16; k++)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            ul2 <<= 1;
                            ul2 |= (byte)(bools[i * 16 + k][j * 8 + l] ? 1 : 0);
                        }
                    }
                    converted[i][j] = (ul1,ul2);
                }
            });
            StringBuilder sb = new StringBuilder();
            string[] results = new string[divheight];
            Parallel.For(0, divheight, i => { results[i] = ASCIItask(divwidth, converted[i]).Result; });
            for (int i = 0; i < divheight; i++) sb.Append(results[i]);
            Console.WriteLine(sb.ToString());
            return;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void LoadASCIIs()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException();
            }
            var files = Directory.EnumerateFiles(".\\ASCIIimages");
            int count = files.Count();
            ASCIIsarr = new (char, UInt64,UInt64)[files.Count()];
            int index = 0;
            foreach (var file in files)
            {
                Bitmap img = (Bitmap)Image.FromFile(file);
                string[] splitted = file.Split('\\');
                var c = (char)int.Parse(splitted[splitted.Length - 1].Split('.')[0]);
                UInt64 ul1 = 0;
                UInt64 ul2 = 0;
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        ul1 <<= 1;
                        ul1 |= img.GetPixel(j, i).GetBrightness() > 0.5 ? 1u : 0;
                    }
                }
                for (int i = 8; i < 16; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        ul2 <<= 1;
                        ul2 |= img.GetPixel(j, i).GetBrightness() > 0.5 ? 1u : 0;
                    }
                }
                ASCIIsarr[index] = (c, ul1,ul2);
                index++;
                img.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Task<string> ASCIItask(in long divwidth, in (UInt64,UInt64)[] image)
        {

            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < divwidth; j++)
            {
                (UInt64,UInt64) hanspace = (UInt64.MaxValue,UInt64.MaxValue);
                long cost = BitOperations.PopCount(hanspace.Item1 ^ image[j].Item1)
                    + BitOperations.PopCount(hanspace.Item2 ^ image[j].Item2);
                char chr = ' ';
                for (int k = 0; k < ASCIIsarr.Length; k++)
                {
                    if (cost > BitOperations.PopCount(ASCIIsarr[k].Item2 ^ image[j].Item1)
                        + BitOperations.PopCount(ASCIIsarr[k].Item3 ^ image[j].Item2))
                    {
                        cost = BitOperations.PopCount(ASCIIsarr[k].Item2 ^ image[j].Item1)
                        + BitOperations.PopCount(ASCIIsarr[k].Item3 ^ image[j].Item2);
                        chr = ASCIIsarr[k].Item1;
                    }
                }
                sb.Append(chr);
            }
            sb.AppendLine();
            return Task.FromResult<string>(sb.ToString());
        }
    }

}
