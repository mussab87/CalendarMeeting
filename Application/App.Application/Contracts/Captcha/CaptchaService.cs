using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace App.Application.Contracts.Infrastructure { }
public static class CaptchaService
{
    public static byte[] GenerateCaptcha(out string captchaText)
    {
        int width = 250, height = 60;
        Random random = new Random();
        captchaText = random.Next(10000, 99999).ToString(); // Generate a 5-digit random number

        using Bitmap bitmap = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(bitmap);
        g.Clear(Color.White);

        // Draw background noise
        DrawNoise(g, width, height, random);

        // Draw CAPTCHA text with random font styles and positions
        DrawCaptchaText(g, captchaText, random);

        // Draw additional security lines
        DrawSecurityLines(g, width, height, random);

        using MemoryStream ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private static void DrawNoise(Graphics g, int width, int height, Random random)
    {
        using SolidBrush brush = new SolidBrush(Color.LightGray);
        for (int i = 0; i < 100; i++)
        {
            int x = random.Next(width);
            int y = random.Next(height);
            g.FillEllipse(brush, x, y, 2, 2);
        }
    }

    private static void DrawCaptchaText(Graphics g, string captchaText, Random random)
    {
        using Font font = new Font("Arial", 28, FontStyle.Bold | FontStyle.Italic);
        using SolidBrush brush = new SolidBrush(Color.Black);

        for (int i = 0; i < captchaText.Length; i++)
        {
            float x = 20 + (i * 30) + random.Next(-5, 5); // Randomize x position
            float y = random.Next(10, 20); // Randomize y position

            g.DrawString(captchaText[i].ToString(), font, brush, x, y);
        }
    }

    private static void DrawSecurityLines(Graphics g, int width, int height, Random random)
    {
        using Pen pen = new Pen(Color.Gray, 2);
        for (int i = 0; i < 6; i++)
        {
            int x1 = random.Next(width);
            int y1 = random.Next(height);
            int x2 = random.Next(width);
            int y2 = random.Next(height);
            g.DrawLine(pen, x1, y1, x2, y2);
        }
    }
}
