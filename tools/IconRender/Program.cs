using System;
using System.IO;
using SkiaSharp;

// Resizes the (square) master icon to <size>, optionally with rounded corners, for icon variants.
// Usage: dotnet run --project tools/IconRender -- <in.png> <out.png> <size> [cornerRadiusPx]
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("usage: <in.png> <out.png> <size> [cornerRadiusPx]");
            return 2;
        }

        var inPath = args[0];
        var outPath = args[1];
        var size = int.Parse(args[2]);
        var radius = args.Length > 3 ? float.Parse(args[3]) : 0f;

        using var src = SKBitmap.Decode(inPath);
        if (src is null)
        {
            Console.Error.WriteLine($"Cannot read {inPath}");
            return 1;
        }

        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (radius > 0)
        {
            using var clip = new SKPath();
            clip.AddRoundRect(new SKRoundRect(new SKRect(0, 0, size, size), radius, radius));
            canvas.ClipPath(clip, SKClipOperation.Intersect, antialias: true);
        }

        using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
        {
            canvas.DrawBitmap(src, new SKRect(0, 0, size, size), paint);
        }
        canvas.Flush();

        var dir = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(Path.GetFullPath(outPath));
        data.SaveTo(stream);

        Console.WriteLine($"Wrote {Path.GetFullPath(outPath)} ({size}x{size}, radius {radius}).");
        return 0;
    }
}
