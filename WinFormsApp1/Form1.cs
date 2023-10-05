global using System.Drawing;
using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public static (char, UInt128)[] ASCIIsarr;
        public Form1()
        {
            InitializeComponent();
            LoadASCIIs();
        }
        public static void LoadASCIIs()
        {
            var files = Directory.EnumerateFiles(".\\ASCIIimages");
            int count = files.Count();
            ASCIIsarr = new (char,UInt128)[files.Count()];
            int index = 0;
            foreach (var file in files)
            {
                Bitmap img = (Bitmap)Image.FromFile(file);
                string[] splitted = file.Split('\\');
                var c = (char)int.Parse(splitted[splitted.Length - 1].Split('.')[0]);
                UInt128 ui = 0;
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        ui <<= 1;
                        ui |= img.GetPixel(j, i).GetBrightness() > 0.5?1u:0;
                    }
                }
                ASCIIsarr[index] = (c, ui);
                index++;
                img.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void button1_Click(object sender, EventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            textBox1.Text = string.Empty;
            sw.Start();
            Bitmap img = (Bitmap)Image.FromFile(textBox2.Text);
            textBox1.Text += img.PixelFormat + Environment.NewLine;
            textBox1.Text += sw.Elapsed.ToString() + Environment.NewLine;//1.9
            long divheight = (img.Height - 1) / 16 + 1;
            long divwidth = (img.Width - 1) / 8 + 1;
            bool[][] bools = new bool[divheight*16][];
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
                    bools[i] = new bool[divwidth*8];
                    for (int j = 0; j < imgw; j++)
                    {
                        bools[i][j] = ((int)rgbValues[i * 3 * imgw + j * 3]
                            + (int)rgbValues[i * 3 * imgw + j * 3 + 1]
                            + (int)rgbValues[i * 3 * imgw + j * 3 + 2]) > 384;
                    }
                });
            }
            else if(img.PixelFormat == PixelFormat.Format32bppArgb)
            {
                Parallel.For(0, imgh, i => {
                    bools[i] = new bool[divwidth*8];
                    for (int j = 0; j < imgw; j++)
                    {
                        bools[i][j] = ((int)rgbValues[i * 4 * imgw + j * 4+1]
                            + (int)rgbValues[i * 4 * imgw + j * 4 + 2]
                            + (int)rgbValues[i * 4 * imgw + j * 4 + 3]) > 384;
                    }
                });
            }
            else
            {
                textBox1.Text = "サポート対象外のpixelformat";
                return;
            }
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            img.UnlockBits(bmpData);
            textBox1.Text += sw.Elapsed.ToString() + Environment.NewLine;//1.8
            //縦は16px横は8pxごとに区切る
            long height = img.Height;
            long width = img.Width;
            img.Dispose();
            UInt128[][] converted = new UInt128[divheight][];
            Parallel.For(0, divheight, i =>
            {
                converted[i] = new UInt128[divwidth];
                for (int j = 0; j < divwidth; j++)
                {
                    UInt128 ui = 0;
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
                    ui |= ul1;
                    ui <<= 64;
                    ui |= ul2;
                    converted[i][j] = ui;
                }
            });
            textBox1.Text += sw.Elapsed.ToString() + Environment.NewLine;//1.7
            StringBuilder sb = new StringBuilder();
            string[] results = new string[divheight];
            Parallel.For(0, divheight, i => { results[i] = ASCIItask(divwidth, converted[i]).Result; });
            for (int i = 0; i < divheight; i++) sb.Append(results[i]);
            textBox1.Text += sw.Elapsed.ToString() + Environment.NewLine;//1.6
            textBox1.Text += sb.ToString();
            GC.Collect();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Task<string> ASCIItask(in long divwidth, in UInt128[] image)
        {

            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < divwidth; j++)
            {
                UInt128 hanspace = UInt128.MaxValue;
                UInt128 cost = UInt128.PopCount(hanspace^image[j]);
                char chr = ' ';
                for(int k = 0; k < ASCIIsarr.Length; k++)
                {
                    if(cost > UInt128.PopCount(ASCIIsarr[k].Item2 ^ image[j]))
                    {
                        cost = UInt128.PopCount(ASCIIsarr[k].Item2 ^ image[j]);
                        chr = ASCIIsarr[k].Item1;
                    }
                }
                sb.Append(chr);
            }
            sb.AppendLine();
            return Task.FromResult<string>(sb.ToString());
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
