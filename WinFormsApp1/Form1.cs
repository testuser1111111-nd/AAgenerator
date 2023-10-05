global using System.Drawing;
using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using System.Text;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public static List<Character> characters;
        public static (char, UInt128)[] ASCIIsarr;
        public Form1()
        {
            InitializeComponent();
            LoadChrs();
            LoadASCIIs();
        }
        public static void LoadChrs()
        {
            characters = new List<Character>();
            var files = Directory.EnumerateFiles(".\\images");
            foreach (var file in files)
            {
                Bitmap img = (Bitmap)Image.FromFile(file);
                string[] splitted = file.Split('\\');
                var c = (char)int.Parse(splitted[splitted.Length - 1].Split('.')[0]);
                if (c <= 127 | (0xff61 <= c & c <= 0xff9f))
                {
                    bool[,] bools = new bool[16, 8];
                    for (int i = 0; i < 16; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            bools[i, j] = img.GetPixel(j, i).GetBrightness() > 0.5;
                        }
                    }
                    characters.Add(new Character(c, true, bools));
                }
                else
                {
                    bool[,] bools = new bool[16, 16];
                    for (int i = 0; i < 16; i++)
                    {
                        for (int j = 0; j < 16; j++)
                        {
                            bools[i, j] = img.GetPixel(j, i).GetBrightness() > 0.5;
                        }
                    }
                    characters.Add(new Character(c, false, bools));
                }
                img.Dispose();
            }
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
                img.Dispose();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            textBox1.Text = string.Empty;
            sw.Start();
            Bitmap img = (Bitmap)Image.FromFile(textBox2.Text);
            textBox1.Text += sw.Elapsed.ToString();
            textBox1.Text += "\r\n";

            bool[,] bools = new bool[img.Height, img.Width];
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    bools[i, j] = img.GetPixel(j, i).GetBrightness() > 0.5;
                }
            }
            textBox1.Text += sw.Elapsed.ToString();
            textBox1.Text += "\r\n";
            //縦は16px横は8pxごとに区切る
            long divheight = (img.Height - 1) / 16 + 1;
            long divwidth = (img.Width - 1) / 8 + 1;
            bool[][][,] boolss = new bool[divheight][][,];
            for (int i = 0; i < divheight; i++)
            {
                boolss[i] = new bool[divwidth][,];
                for (int j = 0; j < divwidth; j++)
                {
                    boolss[i][j] = new bool[16, 8];
                    for (int k = 0; k < 16 & k + i * 16 < img.Height; k++)
                    {
                        for (int l = 0; l < 8 & l + j * 8 < img.Width; l++)
                        {
                            boolss[i][j][k, l] = bools[i * 16 + k, j * 8 + l];
                        }
                    }
                }
            }
            img.Dispose();
            textBox1.Text += sw.Elapsed.ToString();
            textBox1.Text += "\r\n";
            StringBuilder sb = new StringBuilder();
            string[] results = new string[divheight];
            bool chkd = checkBox1.Checked;
            bool fast = checkBox2.Checked;
            if (chkd)
            {
                UInt128[][] converted = new UInt128[divheight][];
                for(int i = 0; i < divheight; i++)
                {
                    converted[i] = new UInt128[divwidth];
                    for(int  j = 0; j < divwidth; j++)
                    {
                        UInt128 ui = 0;
                        for(int k = 0; k < 16; k++)
                        {
                            for(int l = 0; l < 8; l++)
                            {
                                ui <<= 1;
                                ui |= (boolss[i][j][k, l]?1u:0);
                            }
                        }
                        converted[i][j] = ui;
                    }
                }
                Parallel.For(0, divheight, i => { results[i] = ASCIItask(divwidth, converted[i]).Result; });
                for (int i = 0; i < divheight; i++) sb.Append(results[i]);
                //textBox1.Text += sb.ToString();
            }
            else
            {
                Parallel.For(0, divheight, i => { results[i] =task(divwidth, boolss, i,fast).Result; });
                for (int i = 0; i < divheight; i++) sb.Append(results[i]);
                //textBox1.Text = sb.ToString();
            }
            textBox1.Text += sw.Elapsed.ToString();
            textBox1.Text += "\n";
            GC.Collect();
        }
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
        public Task<string> task(in long divwidth, in bool[][][,] boolss, in long i,in bool fast)
        {
            StringBuilder sb = new StringBuilder();
            //コスト
            double[] dp = new double[divwidth + 1];
            dp[0] = 0;
            for (int j = 1; j <= divwidth; j++)
            {
                dp[j] = double.MaxValue;
            }
            int[] prev = new int[divwidth + 1];
            char[] prevchr = new char[divwidth + 1];
            for (int j = 0; j < divwidth; j++)
            {
                bool[,] hanspace = new bool[16, 8];
                for (int k = 0; k < 16; k++)
                {
                    for (int l = 0; l < 8; l++)
                    {
                        hanspace[k, l] = true;
                    }
                }
                double nextcost = dp[j] + EvalHan(fast, hanspace, boolss[i][j]);
                char nextchr = ' ';
                foreach (var character in characters)
                {
                    if (!character.ishan) continue;
                    if (nextcost > dp[j] + EvalHan(fast, character.Bools, boolss[i][j]))
                    {
                        nextchr = character.c;
                        nextcost = dp[j] + EvalHan(fast, character.Bools, boolss[i][j]);
                    }
                }
                if (nextcost < dp[j + 1])
                {
                    dp[j + 1] = nextcost;
                    prev[j + 1] = j;
                    prevchr[j + 1] = nextchr;
                }
                if (j + 2 <= divwidth)
                {
                    bool zenexists = false;
                    bool[,] zenspace = new bool[16, 16];
                    bool[,] combined = new bool[16, 16];
                    for (int k = 0; k < 16; k++)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            combined[k, l] = boolss[i][j][k, l];
                            combined[k, l + 8] = boolss[i][j + 1][k, l];
                        }
                        for (int l = 0; l < 16; l++)
                        {
                            zenspace[k, l] = true;
                        }
                    }
                    double zennextcost = dp[j] + EvalZen(fast, zenspace, combined);
                    char zennextchr = '　';
                    foreach (var zencharacter in characters)
                    {
                        if (zencharacter.ishan) continue;
                        zenexists = true;
                        if (zennextcost > dp[j] + EvalZen(fast, zencharacter.Bools, combined))
                        {
                            zennextchr = zencharacter.c;
                            zennextcost = dp[j] + EvalZen(fast, zencharacter.Bools, combined);
                        }
                    }
                    if (zennextcost < dp[j + 2] & zenexists)
                    {
                        dp[j + 2] = zennextcost;
                        prev[j + 2] = j;
                        prevchr[j + 2] = zennextchr;
                    }
                }

            }
            Stack<char> stack = new Stack<char>();
            long now = divwidth;
            while (now != 0)
            {
                stack.Push(prevchr[now]);
                now = prev[now];
            }
            while (stack.Count > 0)
            {
                sb.Append(stack.Pop());
            }
            sb.AppendLine();
            return Task.FromResult<string>(sb.ToString());
        }



        private double EvalZen(bool fastmode, bool[,] x, bool[,] y)
        {
            if (fastmode)
            {
                long cost = 0;
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        cost += (x[i, j] != y[i, j]) ? 1 : 0;
                    }
                }
                return (double)cost;
            }
            else
            {
                double cost = 0;
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        double tempcost = 3;
                        double tempcost2 = 3;
                        for (int k = -3; k <= 3; k++)
                        {
                            for (int l = -3; l <= 3; l++)
                            {
                                if (0 <= i + k & i + k < 16 & 0 <= j + l & j + l < 16)
                                {
                                    if (x[i, j] == y[i + k, j + l])
                                    {
                                        tempcost = Math.Min(tempcost, Math.Sqrt(k * k + l * l));
                                    }

                                    if (y[i, j] == x[i + k, j + l])
                                    {
                                        tempcost2 = Math.Min(tempcost2, Math.Sqrt(k * k + l * l));
                                    }

                                }
                            }
                        }
                        cost += tempcost + tempcost2;
                    }
                }
                return cost;
            }
        }

        private double EvalHan(bool fastmode, bool[,] x, bool[,] y)
        {
            if( fastmode )
            {
                long cost = 0;
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        cost += (x[i, j] != y[i, j]) ? 1 : 0;
                    }
                }
                return cost;
            }
            else
            {
                double cost = 0;
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        double tempcost = 3;
                        double tempcost2 = 3;
                        for (int k = -3; k <= 3; k++)
                        {
                            for (int l = -3; l <= 3; l++)
                            {
                                if (0 <= i + k & i + k < 16 & 0 <= j + l & j + l < 8)
                                {
                                    if (x[i, j] == y[i + k, j + l])
                                    {
                                        tempcost = Math.Min(tempcost, Math.Sqrt(k * k + l * l));
                                    }

                                    if (y[i, j] == x[i + k, j + l])
                                    {
                                        tempcost2 = Math.Min(tempcost2, Math.Sqrt(k * k + l * l));
                                    }

                                }
                            }
                        }
                        cost += tempcost + tempcost2;
                    }
                }
                return cost;
            }
        }



        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
    public class Character
    {
        public char c;
        public bool ishan;
        public bool[,] Bools;
        public Character(char c, bool ishan, bool[,] bools) 
        {
            this.c = c;
            this.ishan = ishan;
            this.Bools = bools;
        }
    }


}
